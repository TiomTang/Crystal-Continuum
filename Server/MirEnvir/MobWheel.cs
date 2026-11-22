using System;
using System.Collections.Generic;
using System.Threading;
using Server.MirObjects;

namespace Server.MirEnvir
{
    /// <summary>
    /// Coarse deadline wheel for one work partition.
    ///
    /// The partition used to walk every object it owned on every pass (75,765 monsters on the
    /// live server) comparing deadlines, which cost about half a core while the world sat idle
    /// and almost nothing was due. Objects are now parked in the 16ms bucket that matches their
    /// deadline, and a pass only visits the buckets whose time slice has arrived, so the cost
    /// follows the number of due objects instead of the population.
    ///
    /// Buckets are addressed by absolute time slice, so an empty bucket costs one list count
    /// check. An object scheduled further ahead than the wheel span lands in the furthest
    /// reachable bucket; when that bucket comes round its deadline is still in the future, so it is
    /// parked again instead of being processed early.
    ///
    /// While an object is being processed it is owned by the pass (WheelDetached), so a deadline
    /// pulled forward from another thread (a player hitting a monster sets OperateTime to zero) is
    /// stored immediately and picked up when the pass re-parks the object it just finished.
    /// </summary>
    public sealed class MobWheel
    {
        /// <summary>Bucket resolution: 16ms, the same order as one work-loop pass.</summary>
        public const int Shift = 4;

        /// <summary>Number of buckets, giving a 4096ms span (idle re-arm deadlines stay under 3.5s).</summary>
        public const int Slots = 256;

        private const int Mask = Slots - 1;

        private readonly LinkedList<MapObject>[] _slots = new LinkedList<MapObject>[Slots];

        /// <summary>Slice recorded per bucket, used to report the earliest pending deadline.</summary>
        private readonly long[] _slotSlice = new long[Slots];

        /// <summary>One lock per partition. Never held while object code (Process) runs.</summary>
        private readonly object _lock = new object();

        private readonly List<MapObject> _batch = new List<MapObject>(512);

        private long _lastSlice = long.MinValue;
        private int _count;

        public MobWheel()
        {
            for (var i = 0; i < Slots; i++)
            {
                _slots[i] = new LinkedList<MapObject>();
                _slotSlice[i] = long.MinValue;
            }
        }

        /// <summary>Objects currently parked in this wheel.</summary>
        public int Count
        {
            get { return Volatile.Read(ref _count); }
        }

        public static long SliceOf(long deadline)
        {
            return deadline >> Shift;
        }

        public static int SlotIndex(long slice)
        {
            return (int)(slice & Mask);
        }

        /// <summary>
        /// Keeps an insert out of slices the running pass has already drained, and inside the span
        /// the wheel can represent.
        /// </summary>
        public static long ClampSlice(long slice, long lastSlice)
        {
            if (lastSlice == long.MinValue) return slice;

            var min = lastSlice + 1;
            var max = lastSlice + Slots;
            if (slice < min) return min;
            if (slice > max) return max;
            return slice;
        }

        /// <summary>Parks an object by its current deadline and takes ownership of NodeThreaded.</summary>
        public void Add(MapObject obj)
        {
            lock (_lock)
            {
                var node = obj.NodeThreaded;
                if (node != null && node.List != null) return;

                if (node == null) Interlocked.Increment(ref _count);
                InsertLocked(obj, obj.OperateTime);
            }
        }

        public void Remove(MapObject obj)
        {
            lock (_lock)
            {
                var node = obj.NodeThreaded;
                if (node == null) return;

                // A pass in the middle of processing it keeps the node; it notices the dropped
                // membership when it re-parks.
                if (!obj.WheelDetached && node.List != null) node.List.Remove(node);

                obj.NodeThreaded = null;
                Interlocked.Decrement(ref _count);
            }
        }

        /// <summary>Re-parks an object whose deadline changed (may be called from any thread).</summary>
        public void Reslot(MapObject obj)
        {
            lock (_lock)
            {
                var node = obj.NodeThreaded;
                if (node == null) return;

                // The pass that owns the object re-parks it with the deadline stored above.
                if (obj.WheelDetached) return;

                if (node.List != null) node.List.Remove(node);
                InsertLocked(obj, obj.OperateTime);
            }
        }

        /// <summary>
        /// Detaches and returns every object whose deadline has arrived. The list is reused between
        /// passes: process each entry, then call <see cref="Reinsert"/> so it is parked again.
        /// </summary>
        public List<MapObject> TakeDue(long now, out long earliest, out long latest, out long lateMax)
        {
            var batch = _batch;
            batch.Clear();

            earliest = long.MaxValue;
            latest = long.MinValue;
            lateMax = 0;

            var nowSlice = SliceOf(now);

            while (true)
            {
                LinkedList<MapObject> due;

                lock (_lock)
                {
                    if (_lastSlice == long.MinValue)
                    {
                        // Nothing drained yet: start one full span back so every parked object is
                        // visited exactly once, whatever slice world loading landed it in.
                        _lastSlice = nowSlice - Slots;
                    }

                    if (_lastSlice >= nowSlice) break;

                    _lastSlice++;

                    var idx = SlotIndex(_lastSlice);
                    due = _slots[idx];
                    if (due.Count == 0) continue;

                    _slots[idx] = new LinkedList<MapObject>();
                    _slotSlice[idx] = long.MinValue;

                    for (var parked = due.First; parked != null; parked = parked.Next)
                        parked.Value.WheelDetached = true;
                }

                for (var node = due.First; node != null;)
                {
                    var obj = node.Value;
                    node = node.Next;

                    var owner = obj.NodeThreaded;

                    // Dropped from the wheel while in flight (despawned): nothing to park.
                    if (owner == null)
                    {
                        if (node.List == due) due.Remove(node);
                        obj.WheelDetached = false;
                        continue;
                    }

                    if (owner.List == due) due.Remove(owner);

                    var deadline = obj.OperateTime;
                    if (deadline < earliest) earliest = deadline;
                    if (deadline > latest) latest = deadline;

                    if (deadline > now)
                    {
                        // Its bucket came round before the deadline (parked beyond the span, or
                        // re-armed meanwhile): park it again instead of processing it early.
                        lock (_lock) InsertLocked(obj, deadline);
                        continue;
                    }

                    if (now - deadline > lateMax) lateMax = now - deadline;
                    batch.Add(obj);
                }
            }

            if (earliest == long.MaxValue) earliest = NextParkedDeadline();

            return batch;
        }

        /// <summary>Parks a processed object again by its (new) deadline. Safe to call twice.</summary>
        public void Reinsert(MapObject obj)
        {
            lock (_lock)
            {
                var node = obj.NodeThreaded;
                if (node == null)
                {
                    obj.WheelDetached = false;
                    return;
                }

                if (node.List != null) node.List.Remove(node);
                InsertLocked(obj, obj.OperateTime);
            }
        }

        /// <summary>Drops all membership (world teardown).</summary>
        public void Clear()
        {
            lock (_lock)
            {
                for (var i = 0; i < Slots; i++)
                {
                    _slots[i] = new LinkedList<MapObject>();
                    _slotSlice[i] = long.MinValue;
                }

                _batch.Clear();
                _lastSlice = long.MinValue;
                Interlocked.Exchange(ref _count, 0);
            }
        }

        private void InsertLocked(MapObject obj, long deadline)
        {
            var slice = ClampSlice(SliceOf(deadline), _lastSlice);
            var idx = SlotIndex(slice);
            var list = _slots[idx];

            if (list.Count == 0) _slotSlice[idx] = slice;

            obj.NodeThreaded = list.AddLast(obj);
            obj.WheelDetached = false;
        }

        private long NextParkedDeadline()
        {
            var slice = long.MaxValue;

            for (var i = 0; i < Slots; i++)
            {
                if (_slots[i].Count == 0) continue;
                if (_slotSlice[i] < slice) slice = _slotSlice[i];
            }

            return slice == long.MaxValue ? long.MaxValue : slice << Shift;
        }
    }
}
