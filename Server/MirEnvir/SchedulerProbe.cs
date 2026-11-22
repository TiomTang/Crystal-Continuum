using System.Diagnostics;
using System.Globalization;

namespace Server.MirEnvir
{
    /// <summary>
    /// Read-only scheduling observation probe. Modified 2026-11-22.
    /// Records per-cycle wall work, per-cycle process CPU, due counts from the main object walk
    /// and each monster partition, then emits one summary line per window to the Debug log.
    /// Disable with MIR2_SCHED_PROBE=0. Every entry point swallows its own failure so the probe
    /// can never interrupt the work loop.
    /// </summary>
    public static class SchedulerProbe
    {
        private const int BucketCount = 400;          // 1ms buckets, 0..399
        private const int MaxPartitions = 64;
        private const int PhaseCount = 5;
        private const long WindowMs = 60 * 1000;

        private static readonly bool Enabled = ReadEnabled();
        private static readonly object Sync = new object();
        private static readonly int[] WorkBuckets = new int[BucketCount];
        private static readonly int[] CpuBuckets = new int[BucketCount];
        private static readonly int[] PartitionObjects = new int[MaxPartitions];
        private static readonly int[] PartitionDue = new int[MaxPartitions];
        private static readonly long[] PartitionDueSum = new long[MaxPartitions];
        private static readonly long[] PartitionLastTs = new long[MaxPartitions];
        private static readonly long[] PartitionGapMax = new long[MaxPartitions];
        private static int _burstLines;
        private static readonly long[] PartitionEarliest = new long[MaxPartitions];
        private static readonly int[] PartitionDueMax = new int[MaxPartitions];
        private static readonly long[] PartitionLateMax = new long[MaxPartitions];
        private static readonly int[] PartitionPasses = new int[MaxPartitions];
        private static readonly int[] PartitionDuePasses = new int[MaxPartitions];
        private static readonly Process Self = TrySelf();
        private static readonly string[] PhaseNames = { "conn", "monster", "walk", "maps", "envir" };
        private static readonly long[] PhaseTicks = new long[PhaseCount];
        private static readonly long[] CyclePhaseTicks = new long[PhaseCount];
        // A cycle over this wall cost is what players feel as a hitch. Logging the phase split
        // for the worst cycles is the only way to attribute an intermittent freeze. CPU is only
        // a secondary trigger: Process.TotalProcessorTime moves in 15.625ms scheduler quanta, so
        // single-cycle CPU figures are coarse and jump in steps of 15.6/31.2/62.5ms.
        private const double StallMs = 50.0;
        private const double StallCpuMs = 200.0;
        private const int StallLinesPerWindow = 8;
        private static int _stallCount;
        private static int _stallLines;

        private static long _windowStart = Stopwatch.GetTimestamp();
        private static long _cycles, _waited, _waitMs;
        private static long _walkVisited, _walkDue;
        private static long _workOver, _cpuOver;
        private static long _workSumMicros, _cpuSumMicros, _cpuSamples;
        private static long _cycleStartTs;
        private static long _workEndTs;
        private static long _phaseTs;
        private static long _lastCpuMicros;
        private static int _partitionSize;

        static SchedulerProbe()
        {
            for (int i = 0; i < MaxPartitions; i++) PartitionEarliest[i] = long.MaxValue;
        }

        public static bool IsEnabled => Enabled;

        private static bool ReadEnabled()
        {
            try
            {
                return !string.Equals(Environment.GetEnvironmentVariable("MIR2_SCHED_PROBE"), "0",
                    StringComparison.Ordinal);
            }
            catch { return true; }
        }

        private static Process TrySelf()
        {
            try { return Process.GetCurrentProcess(); }
            catch { return null; }
        }

        /// <summary>Marks the beginning of one work-loop cycle. One clock read.</summary>
        public static void MarkCycleStart()
        {
            if (!Enabled) return;
            try
            {
                _cycleStartTs = Stopwatch.GetTimestamp();
                _workEndTs = 0;
                Array.Clear(CyclePhaseTicks, 0, CyclePhaseTicks.Length);
            }
            catch { }
        }

        /// <summary>Starts timing the first named phase of a cycle.</summary>
        public static void MarkPhaseStart()
        {
            if (!Enabled) return;
            try { _phaseTs = Stopwatch.GetTimestamp(); }
            catch { }
        }

        /// <summary>Closes one phase and starts timing the next. Wall time on the work-loop thread.</summary>
        public static void EndPhase(int phase)
        {
            if (!Enabled) return;
            try
            {
                long now = Stopwatch.GetTimestamp();
                if (phase >= 0 && phase < PhaseCount && _phaseTs > 0)
                {
                    PhaseTicks[phase] += now - _phaseTs;
                    CyclePhaseTicks[phase] += now - _phaseTs;
                }
                _phaseTs = now;
            }
            catch { }
        }

        /// <summary>Marks the end of a cycle's work, before the loop waits for the next tick.</summary>
        public static void MarkWorkEnd()
        {
            if (!Enabled) return;
            try { _workEndTs = Stopwatch.GetTimestamp(); }
            catch { }
        }

        /// <summary>Records one finished cycle: wall cost of its work, CPU delta, walk due counts.</summary>
        public static void NoteCycle(Envir envir, long waitMs, int walkVisited, int walkDue)
        {
            if (!Enabled) return;
            try
            {
                long endTs = _workEndTs > _cycleStartTs ? _workEndTs : Stopwatch.GetTimestamp();
                double workMs = (endTs - _cycleStartTs) * 1000.0 / Stopwatch.Frequency;
                long cpuNow = CpuMicros();
                double cpuMs = _lastCpuMicros <= 0 || cpuNow <= 0 ? -1 : (cpuNow - _lastCpuMicros) / 1000.0;
                _lastCpuMicros = cpuNow;

                NoteCycleSample(waitMs, workMs, cpuMs, walkVisited, walkDue);
                if (workMs >= StallMs || cpuMs >= StallCpuMs) NoteStall(envir, workMs, cpuMs);
            }
            catch { }
        }

        /// <summary>Accumulates one sample. Exposed so regression checks can inject known costs.</summary>
        public static void NoteCycleSample(long waitMs, double workMs, double cpuMs, int walkVisited, int walkDue)
        {
            if (!Enabled) return;
            try
            {
                _cycles++;
                if (waitMs > 0) { _waited++; _waitMs += waitMs; }
                _walkVisited += walkVisited;
                _walkDue += walkDue;
                _workSumMicros += (long)(workMs * 1000.0);
                if (cpuMs >= 0) { _cpuSumMicros += (long)(cpuMs * 1000.0); _cpuSamples++; }
                Count(WorkBuckets, ref _workOver, workMs);
                if (cpuMs >= 0) Count(CpuBuckets, ref _cpuOver, cpuMs);
            }
            catch { }
        }

        /// <summary>One line per outlier cycle, with the phase split of that same cycle.</summary>
        private static void NoteStall(Envir envir, double workMs, double cpuMs)
        {
            if (!Enabled) return;
            try
            {
                _stallCount++;
                if (_stallLines >= StallLinesPerWindow) return;
                _stallLines++;

                int players = -1, connections = -1, objects = -1;
                if (envir != null)
                {
                    players = envir.Players.Count;
                    connections = envir.Connections.Count;
                    objects = envir.Objects.Count;
                }

                var phases = new System.Text.StringBuilder();
                double ticksToMs = 1000.0 / Stopwatch.Frequency;
                for (int i = 0; i < PhaseCount; i++)
                {
                    if (i > 0) phases.Append(' ');
                    phases.Append(PhaseNames[i]).Append('=').Append((CyclePhaseTicks[i] * ticksToMs).ToString("F1", CultureInfo.InvariantCulture));
                }

                Server.Logger.GetLogger(LogType.Debug).Info(string.Format(CultureInfo.InvariantCulture,
                    "scheduler stall work={0:F1}ms cpu={1:F1}ms phases[{2}] players={3} conns={4} objects={5}",
                    workMs, cpuMs, phases, players, connections, objects));
            }
            catch { }
        }

        /// <summary>Records one monster partition pass. Called from the partition's own worker.</summary>
        public static void NotePartition(int id, int objects, int due, long earliest, long latest, long lateMax, long now)
        {
            if (!Enabled) return;
            try
            {
                if (id < 0 || id >= MaxPartitions) return;
                PartitionObjects[id] = objects;
                PartitionDue[id] = due;
                PartitionDueSum[id] += due;
                PartitionEarliest[id] = earliest;
                if (due > PartitionDueMax[id]) PartitionDueMax[id] = due;
                if (lateMax > PartitionLateMax[id]) PartitionLateMax[id] = lateMax;
                PartitionPasses[id]++;
                if (due > 0) PartitionDuePasses[id]++;
                if (id + 1 > _partitionSize) _partitionSize = id + 1;

                long nowTs = Stopwatch.GetTimestamp();
                long gapMs = PartitionLastTs[id] == 0 ? -1 : (nowTs - PartitionLastTs[id]) * 1000 / Stopwatch.Frequency;
                PartitionLastTs[id] = nowTs;
                if (gapMs > PartitionGapMax[id]) PartitionGapMax[id] = gapMs;

                // A whole-partition pass means every deadline expired at once: either the
                // partition was not walked for ~its re-arm window, or something re-armed them
                // together. Log it when it happens instead of inferring from window maxima.
                if (due >= 5000 && _burstLines < 6)
                {
                    _burstLines++;
                    Server.Logger.GetLogger(LogType.Debug).Info(string.Format(CultureInfo.InvariantCulture,
                        "scheduler burst part={0} due={1} objs={2} gap={3}ms now={4} earliest={5} latest={6} span={7}ms",
                        id, due, objects, gapMs, now, earliest,
                        latest == long.MinValue ? -1 : latest,
                        latest == long.MinValue || earliest == long.MaxValue ? -1 : (latest - earliest)));
                }
            }
            catch { }
        }

        /// <summary>Emits the window summary once the window has elapsed. Main thread only.</summary>
        public static void ReportDue(Envir envir)
        {
            if (!Enabled) return;
            try
            {
                long now = Stopwatch.GetTimestamp();
                long elapsedMs = (now - _windowStart) * 1000 / Stopwatch.Frequency;
                if (elapsedMs < WindowMs) return;
                string line;
                lock (Sync) line = BuildReport(elapsedMs / 1000.0, envir);
                Server.Logger.GetLogger(LogType.Debug).Info(line);
            }
            catch { }
        }

        /// <summary>Formats the current window and resets it. Exposed for regression verification.</summary>
        public static string BuildReport(double windowSeconds, Envir envir)
        {
            int monsters = 0;
            for (int i = 0; i < _partitionSize; i++) monsters += PartitionObjects[i];

            int worldObjects = -1, maps = -1, players = -1, connections = -1;
            long now = 0;
            try
            {
                if (envir != null)
                {
                    worldObjects = envir.Objects.Count;
                    maps = envir.MapList.Count;
                    players = envir.Players.Count;
                    connections = envir.Connections.Count;
                    now = envir.Time;
                }
            }
            catch { }

            string line = string.Format(CultureInfo.InvariantCulture,
                "scheduler win={0:F1}s cycles={1} waited={2} wait_ms={3} work_ms[{4}] cpu_ms[{5}] " +
                "main_walk[visited_avg={6} due_avg={7}] phases[{8}] parts=[{9}] world[objects={10} monsters={11} nonmonster={12} maps={13} players={14} conns={15} mt={16} tl={17}] stalls={18}",
                windowSeconds, _cycles, _waited, _waitMs,
                Describe(WorkBuckets, _workOver, _workSumMicros, _cycles),
                Describe(CpuBuckets, _cpuOver, _cpuSumMicros, _cpuSamples),
                _cycles == 0 ? 0 : _walkVisited / _cycles,
                _cycles == 0 ? 0 : _walkDue / _cycles,
                DescribePhases(),
                DescribePartitions(now),
                worldObjects, monsters, monsters < 0 || worldObjects < 0 ? -1 : worldObjects - monsters,
                maps, players, connections,
                Settings.Multithreaded ? "True" : "False", Settings.ThreadLimit, _stallCount);

            ResetWindow();
            return line;
        }

        private static string DescribePhases()
        {
            var text = new System.Text.StringBuilder();
            double ticksToMs = 1000.0 / Stopwatch.Frequency;
            for (int i = 0; i < PhaseCount; i++)
            {
                if (i > 0) text.Append(' ');
                double perCycle = _cycles == 0 ? 0 : PhaseTicks[i] * ticksToMs / _cycles;
                text.Append(PhaseNames[i]).Append('=').Append(perCycle.ToString("F1", CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        private static string DescribePartitions(long now)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < _partitionSize; i++)
            {
                if (i > 0) text.Append(' ');
                text.Append('#').Append(i.ToString(CultureInfo.InvariantCulture))
                    .Append(":objs=").Append(PartitionObjects[i].ToString(CultureInfo.InvariantCulture))
                    .Append(",due=").Append(PartitionDue[i].ToString(CultureInfo.InvariantCulture))
                    .Append(",due_max=").Append(PartitionDueMax[i].ToString(CultureInfo.InvariantCulture))
                    .Append(",due_avg=").Append((PartitionPasses[i] == 0 ? 0 : PartitionDueSum[i] / PartitionPasses[i]).ToString(CultureInfo.InvariantCulture))
                    .Append(",gap_max=").Append(PartitionGapMax[i].ToString(CultureInfo.InvariantCulture))
                    .Append(",due_passes=").Append(PartitionDuePasses[i].ToString(CultureInfo.InvariantCulture))
                    .Append('/').Append(PartitionPasses[i].ToString(CultureInfo.InvariantCulture))
                    .Append(",late_max=").Append(PartitionLateMax[i].ToString(CultureInfo.InvariantCulture)).Append("ms")
                    .Append(",earliest=");
                long earliest = PartitionEarliest[i];
                if (earliest == long.MaxValue) text.Append("none");
                else text.Append((earliest - Envir.Main.Time).ToString(CultureInfo.InvariantCulture)).Append("ms");
            }
            return text.Length == 0 ? "none" : text.ToString();
        }

        private static void ResetWindow()
        {
            Array.Clear(WorkBuckets, 0, WorkBuckets.Length);
            Array.Clear(CpuBuckets, 0, CpuBuckets.Length);
            Array.Clear(PhaseTicks, 0, PhaseTicks.Length);
            Array.Clear(PartitionDueSum, 0, PartitionDueSum.Length);
            _stallCount = 0;
            _stallLines = 0;
            _burstLines = 0;
            Array.Clear(PartitionGapMax, 0, PartitionGapMax.Length);
            for (int i = 0; i < MaxPartitions; i++)
            {
                PartitionObjects[i] = 0;
                PartitionDue[i] = 0;
                PartitionEarliest[i] = long.MaxValue;
                PartitionDueMax[i] = 0;
                PartitionLateMax[i] = 0;
                PartitionPasses[i] = 0;
                PartitionDuePasses[i] = 0;
            }
            _partitionSize = 0;
            _cycles = _waited = _waitMs = 0;
            _walkVisited = _walkDue = 0;
            _workOver = _cpuOver = 0;
            _workSumMicros = _cpuSumMicros = _cpuSamples = 0;
            _windowStart = Stopwatch.GetTimestamp();
            _cycleStartTs = _windowStart;
            _workEndTs = 0;
            _lastCpuMicros = CpuMicros();
        }

        private static void Count(int[] buckets, ref long overflow, double value)
        {
            if (double.IsNaN(value) || value < 0) return;
            int index = (int)value;
            if (index >= buckets.Length) overflow++;
            else buckets[index]++;
        }

        private static string Describe(int[] buckets, long overflow, long sumMicros, long samples)
        {
            if (samples <= 0) return "samples=0";
            double avg = sumMicros / 1000.0 / samples;
            int max = -1;
            for (int i = buckets.Length - 1; i >= 0; i--)
            {
                if (buckets[i] == 0) continue;
                max = i;
                break;
            }
            string limit = buckets.Length.ToString(CultureInfo.InvariantCulture);
            string maxText = overflow > 0 ? ">=" + limit : max < 0 ? "-" : max.ToString(CultureInfo.InvariantCulture);
            string p50 = BucketText(Percentile(buckets, samples, 0.50), buckets.Length);
            string p95 = BucketText(Percentile(buckets, samples, 0.95), buckets.Length);
            return string.Format(CultureInfo.InvariantCulture,
                "p50={0} p95={1} max={2} avg={3:F1} over{4}={5}",
                p50, p95, maxText, avg, limit, overflow);
        }

        private static string BucketText(int bucket, int bucketCount)
        {
            return bucket >= bucketCount ? ">=" + bucketCount.ToString(CultureInfo.InvariantCulture)
                : bucket.ToString(CultureInfo.InvariantCulture);
        }

        private static int Percentile(int[] buckets, long total, double fraction)
        {
            long target = (long)Math.Ceiling(total * fraction);
            long seen = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                seen += buckets[i];
                if (seen >= target) return i;
            }
            return buckets.Length;
        }

        private static long CpuMicros()
        {
            try
            {
                if (Self == null) return -1;
                Self.Refresh();
                return (long)(Self.TotalProcessorTime.TotalMilliseconds * 1000.0);
            }
            catch { return -1; }
        }
    }
}
