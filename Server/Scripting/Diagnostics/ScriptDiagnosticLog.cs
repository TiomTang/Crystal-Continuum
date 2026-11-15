using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.MirObjects
{
    /// <summary>Isolated script detail storage. Modified 2026-11-15.</summary>
    public static class ScriptDiagnosticLog
    {
        private const long MaximumFileBytes = 16 * 1024 * 1024;
        private const int MaximumRecordBytes = 256 * 1024;
        private static readonly object Sync = new object();
        private static readonly Encoding Utf8 = new UTF8Encoding(false);
        // Only files created by this backend are eligible for deletion.
        private static readonly Regex OwnedName = new Regex(
            @"^script-[0-9]{8}T[0-9]{9}Z-[a-f0-9]{32}(\.[0-9]+)?\.log$",
            RegexOptions.CultureInvariant);
        private static Hierarchy _repository;
        private static ILog _logger;
        private static RollingFileAppender _appender;
        private static System.Threading.Timer _timer;
        private static string _directory, _activeFile;
        private static int _retentionDays;
        private static long _maxDirectoryBytes, _directoryBytes, _suppressedWrites, _pendingSuppressed;
        private static long _errorVersion;
        private static DateTime _activeDay, _nextMaintenanceUtc, _nextWarningUtc;
        private static bool _initialized, _stopping;
        private static string _lastWriteError, _lastMaintenanceError, _lastSuppressedSummary;
        private static readonly System.Threading.ManualResetEventSlim PeriodicIdle = new System.Threading.ManualResetEventSlim(true);
        private static System.Threading.ManualResetEventSlim _shutdownComplete;
        private static int _periodicCallbacks, _shutdownThread;
        private static bool _shutdownDeferred;
        [ThreadStatic] private static int _periodicDepth;

        // Keep the public error view inclusive: a saved record does not prove retention recovered.
        private static string CurrentError => _lastWriteError == null ? _lastMaintenanceError :
            _lastMaintenanceError == null ? _lastWriteError : _lastWriteError + "; " + _lastMaintenanceError;
        public static string LastWriteError { get { lock (Sync) return CurrentError; } }
        public static bool IsHealthy { get { lock (Sync) return _initialized && !_stopping && _appender != null && CurrentError == null; } }
        public static long SuppressedWriteCount { get { lock (Sync) return _suppressedWrites; } }
        public static string ActiveFile { get { lock (Sync) return _activeFile; } }

        /// <summary>Call only from the actual world-load path, never an Envir static constructor.</summary>
        public static void Initialize(string directory = null, int retentionDays = 30,
            long maxDirectoryBytes = 536870912)
        {
            lock (Sync)
            {
                if (_initialized) return;
                _stopping = _shutdownDeferred = false;
                _shutdownThread = 0;
                _shutdownComplete = new System.Threading.ManualResetEventSlim(false);
                _lastWriteError = _lastMaintenanceError = null;
                _lastSuppressedSummary = null;
                _suppressedWrites = _pendingSuppressed = 0;
                _nextWarningUtc = DateTime.MinValue;
                _initialized = true;
                try
                {
                    if (retentionDays < 1 || maxDirectoryBytes < 1)
                        throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention and storage limit must be positive.");
                    _retentionDays = retentionDays;
                    _maxDirectoryBytes = maxDirectoryBytes;
                    _directory = Path.GetFullPath(directory ?? Path.Combine("Logs", "Script"));
                    Directory.CreateDirectory(_directory);
                    if ((File.GetAttributes(_directory) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Script log directory must not be a reparse point.");
                    _repository ??= (Hierarchy)LogManager.CreateRepository("ScriptDiagnostics-" + Guid.NewGuid().ToString("N"));
                    _repository.Root.Level = Level.All;
                    _repository.Configured = true;
                    _logger = LogManager.GetLogger(_repository.Name, "ScriptDetails");
                    OpenFile(DateTime.UtcNow);
                    Maintain(DateTime.UtcNow, 0);
                }
                catch (Exception ex)
                {
                    Fail("Script detail logging initialization failed", ex);
                    CloseAppender();
                }
                // Failure still gets a bounded periodic fallback and repeat-summary flush.
                _timer = new System.Threading.Timer(_ => PeriodicFlush(), null,
                    TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
        }

        public static void Write(ScriptDiagnostic diagnostic)
        {
            if (diagnostic == null) return;
            lock (Sync)
            {
                bool written = false;
                string failure = null;
                try
                {
                    if (!_initialized || _appender == null)
                        failure = _lastWriteError ?? "Script detail logging is not initialized.";
                    else
                    {
                        DateTime now = DateTime.UtcNow;
                        if (_activeDay != now.Date) OpenFile(now);
                        string detail = diagnostic.IsSummary ? diagnostic.ToSummaryString() : diagnostic.ToDetailString();
                        if (diagnostic.IsSummary)
                            detail += " firstUTC=" + diagnostic.FirstSeenUtc.ToString("O", CultureInfo.InvariantCulture) +
                                " lastUTC=" + diagnostic.LastSeenUtc.ToString("O", CultureInfo.InvariantCulture);
                        long bytes = Utf8.GetByteCount(detail) + Utf8.GetByteCount(Environment.NewLine);
                        if (bytes > MaximumRecordBytes)
                            failure = "Script detail exceeds the 256 KiB per-record limit; detail was not saved.";
                        else if (bytes > _maxDirectoryBytes)
                            failure = "Script detail exceeds the directory limit; history retained and detail was not saved.";
                        else
                        {
                            // Inspect current occupancy before every write; external files also consume space.
                            // This is a process-local check, not an atomic cross-process storage limit.
                            _directoryBytes = DirectoryFiles().Sum(file => file.Length);
                            if (now >= _nextMaintenanceUtc)
                                Maintain(now, bytes);
                            if (bytes > _maxDirectoryBytes - _directoryBytes)
                                failure = "Script detail directory limit reached; active file retained and detail was not saved.";
                            else if (_appender != null)
                            {
                                long version = _errorVersion;
                                // Reserve even a failed write: the stream may have accepted a partial record.
                                _directoryBytes += bytes;
                                _logger.Info(detail);
                                if (version == _errorVersion)
                                {
                                    _lastWriteError = null;
                                    written = true;
                                }
                                else failure = _lastWriteError;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Fail("Script detail write failed", ex);
                    failure = _lastWriteError;
                }
                if (!written)
                {
                    _suppressedWrites++;
                    _pendingSuppressed++;
                    _lastWriteError = failure ?? "Script detail was not saved.";
                    _lastSuppressedSummary = diagnostic.ToSummaryString();
                    WarnFallback(_lastSuppressedSummary);
                }
                // Loading writes details only; the load boundary publishes one classified summary.
                // A failed write is represented by the rate-limited fallback, not a success-looking line.
                else
                {
                    if (CurrentError != null) WarnFallback(null);
                    if (diagnostic.Runtime || diagnostic.IsSummary)
                        Publish(diagnostic.ToSummaryString(), Severity(diagnostic.Category));
                }
            }
        }

        public static void ReportLoadSummary(string operation)
        {
            Flush();
            var entries = ScriptDiagnostics.Snapshot();
            string counts = string.Join(", ", Enum.GetValues<ScriptDiagnosticCategory>().Select(category => category + "=" +
                entries.Count(entry => entry.Category == category)));
            string batch = " batch=" + ScriptDiagnostics.BatchId;
            string message = "[Script " + operation + "]" + batch + " " + counts +
                ", suppressed=" + ScriptDiagnostics.Suppressed + ", detailWritesSuppressed=" + SuppressedWriteCount;
            lock (Sync)
                message += CurrentError == null ? ", details=" + _directory : ", DETAILS DEGRADED: " + CurrentError;
            Publish(message, entries.Any(entry => Severity(entry.Category) == Level.Error) ? Level.Error :
                entries.Count > 0 ? Level.Warn : Level.Info);
        }

        public static void Flush()
        {
            // Never hold the backend lock while entering the collector: its sink calls Write.
            ScriptDiagnostics.Flush();
            lock (Sync)
            {
                if (!_initialized) return;
                try
                {
                    _appender?.Flush(5000);
                    if (_appender != null) Maintain(DateTime.UtcNow, 0);
                }
                catch (Exception ex) { Fail("Script detail flush/maintenance failed", ex); }
                if (_pendingSuppressed > 0) WarnFallback(null, true);
            }
        }

        public static void Shutdown()
        {
            System.Threading.Timer timer = null;
            System.Threading.ManualResetEventSlim completed;
            bool finish = false, wait = false;
            lock (Sync)
            {
                if (!_initialized) return;
                completed = _shutdownComplete;
                if (_stopping)
                {
                    // A collector sink or fallback may reenter shutdown on its owning thread.
                    if (_periodicDepth > 0 || _shutdownThread == Environment.CurrentManagedThreadId) return;
                    wait = true;
                }
                else
                {
                    _stopping = true;
                    timer = _timer;
                    _timer = null;
                    if (_periodicDepth > 0) _shutdownDeferred = true;
                    else
                    {
                        _shutdownThread = Environment.CurrentManagedThreadId;
                        finish = true;
                    }
                }
            }
            timer?.Dispose();
            if (wait) completed.Wait();
            else if (finish)
            {
                // No backend lock is held: in-flight callbacks still need it to persist summaries
                // already marked emitted by the collector. Queued callbacks exit at the stop gate.
                PeriodicIdle.Wait();
                CompleteShutdown();
            }
            // Reentrant periodic shutdown is completed by the last callback, never by waiting on itself.
        }

        private static void CompleteShutdown()
        {
            try { Flush(); }
            catch (Exception ex)
            {
                lock (Sync) Fail("Script diagnostic shutdown flush failed", ex);
            }
            finally
            {
                lock (Sync)
                {
                    try { CloseAppender(); }
                    finally
                    {
                        _initialized = false;
                        _shutdownThread = 0;
                        _shutdownComplete.Set();
                    }
                }
            }
        }

        private static void PeriodicFlush()
        {
            lock (Sync)
            {
                if (!_initialized || _stopping) return;
                _periodicCallbacks++;
                _periodicDepth++;
                PeriodicIdle.Reset();
            }
            try { Flush(); }
            catch (Exception ex)
            {
                lock (Sync) Fail("Script diagnostic periodic flush failed", ex);
            }
            finally
            {
                bool finish = false;
                lock (Sync)
                {
                    _periodicDepth--;
                    if (--_periodicCallbacks == 0)
                    {
                        PeriodicIdle.Set();
                        if (_shutdownDeferred)
                        {
                            _shutdownDeferred = false;
                            _shutdownThread = Environment.CurrentManagedThreadId;
                            finish = true;
                        }
                    }
                }
                if (finish) CompleteShutdown();
            }
        }

        private static void OpenFile(DateTime now)
        {
            CloseAppender();
            _activeDay = now.Date;
            _activeFile = Path.Combine(_directory, "script-" + now.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture) +
                "-" + Guid.NewGuid().ToString("N") + ".log");
            var layout = new PatternLayout("%message%newline");
            layout.ActivateOptions();
            _appender = new RollingFileAppender
            {
                Name = "ScriptDetails",
                File = _activeFile,
                AppendToFile = true,
                Encoding = Utf8,
                ImmediateFlush = true,
                LockingModel = new FileAppender.ExclusiveLock(),
                RollingStyle = RollingFileAppender.RollingMode.Size,
                MaximumFileSize = Math.Min(MaximumFileBytes, Math.Max(1, _maxDirectoryBytes / 4)).ToString(CultureInfo.InvariantCulture),
                MaxSizeRollBackups = -1,
                CountDirection = 1,
                StaticLogFileName = true,
                PreserveLogFileNameExtension = true,
                Layout = layout,
                ErrorHandler = new DiagnosticErrorHandler()
            };
            long version = _errorVersion;
            _appender.ActivateOptions();
            if (version != _errorVersion)
            {
                CloseAppender();
                return;
            }
            _repository.Root.AddAppender(_appender);
            _nextMaintenanceUtc = DateTime.MinValue;
        }

        private static void CloseAppender()
        {
            if (_appender == null) return;
            var appender = _appender;
            _appender = null;
            try
            {
                _repository?.Root.RemoveAppender(appender);
                appender.Close();
            }
            catch (Exception ex) { Fail("Script detail close failed", ex); }
        }

        private static List<FileInfo> DirectoryFiles()
        {
            try
            {
                var files = new DirectoryInfo(_directory).EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList();
                // Windows directory enumeration can cache stale lengths for files still open by a writer.
                foreach (var file in files) file.Refresh();
                return files.Where(file => (file.Attributes & FileAttributes.ReparsePoint) == 0).ToList();
            }
            catch (Exception ex)
            {
                FailMaintenance("Script detail directory scan failed", ex);
                throw;
            }
        }

        private static void Maintain(DateTime now, long incomingBytes)
        {
            // Cleanup remains periodic; admission separately checks fresh occupancy on every write.
            _nextMaintenanceUtc = now.AddMinutes(1);
            bool failed = false;
            try
            {
                var directoryFiles = DirectoryFiles();
                // Unrelated files consume the budget, but are never deletion candidates.
                long total = directoryFiles.Sum(file => file.Length);
                var files = directoryFiles.Where(file => OwnedName.IsMatch(file.Name))
                    .OrderBy(file => file.LastWriteTimeUtc).ThenBy(file => file.Name, StringComparer.Ordinal).ToList();
                foreach (var file in files)
                {
                    if (string.Equals(file.FullName, _activeFile, StringComparison.OrdinalIgnoreCase)) continue;
                    if (file.LastWriteTimeUtc >= now.AddDays(-_retentionDays) && total <= _maxDirectoryBytes - incomingBytes) continue;
                    try
                    {
                        long length = file.Length;
                        file.Delete();
                        total -= length;
                    }
                    catch (Exception ex)
                    {
                        failed = true;
                        FailMaintenance("Script detail retention could not delete " + file.Name, ex);
                    }
                }
                _directoryBytes = total;
                if (!failed) _lastMaintenanceError = null;
            }
            catch (Exception ex)
            {
                FailMaintenance("Script detail maintenance failed", ex);
                throw;
            }
        }

        private static Level Severity(ScriptDiagnosticCategory category)
        {
            return category == ScriptDiagnosticCategory.EngineFault || category == ScriptDiagnosticCategory.RuntimeFailure ? Level.Error : Level.Warn;
        }

        private static void Fail(string message, Exception exception)
        {
            _errorVersion++;
            _lastWriteError = message + (exception == null ? string.Empty : ": " + exception.GetType().Name + ": " + exception.Message);
            WarnFallback(null);
        }

        private static void FailMaintenance(string message, Exception exception)
        {
            _lastMaintenanceError = message + (exception == null ? string.Empty : ": " + exception.GetType().Name + ": " + exception.Message);
            WarnFallback(null);
        }

        private static void WarnFallback(string summary, bool force = false)
        {
            DateTime now = DateTime.UtcNow;
            if (!force && now < _nextWarningUtc) return;
            _nextWarningUtc = now.AddMinutes(1);
            summary ??= _lastSuppressedSummary;
            string error = CurrentError;
            string message = (error == null ? "[Script detail storage recovered]" : "[Script DETAILS DEGRADED] " + error) +
                " Suppressed detail writes since last notice=" + _pendingSuppressed + "; total=" + _suppressedWrites +
                (string.IsNullOrEmpty(summary) ? string.Empty : ". Latest: " + summary);
            _pendingSuppressed = 0;
            Publish(message, Level.Error);
        }

        private static void Publish(string message, Level level)
        {
            // MessageQueue.Enqueue always writes Info as well. Queue directly for Warn/Error so
            // there is exactly one existing Server-log record, at the correct severity.
            try
            {
                if (level == Level.Info) MessageQueue.Instance.Enqueue(message);
                else
                {
                    var queue = MessageQueue.Instance.MessageLog;
                    if (queue.Count < 100) queue.Enqueue("[" + DateTime.Now + "]: " + message + Environment.NewLine);
                    if (level == Level.Error) Server.Logger.GetLogger(LogType.Server).Error(message);
                    else Server.Logger.GetLogger(LogType.Server).Warn(message);
                }
            }
            catch { /* A diagnostic must not bring down world loading, even if the fallback also fails. */ }
        }

        private sealed class DiagnosticErrorHandler : IErrorHandler
        {
            public void Error(string message) { Error(message, null, ErrorCode.GenericFailure); }
            public void Error(string message, Exception exception) { Error(message, exception, ErrorCode.GenericFailure); }
            public void Error(string message, Exception exception, ErrorCode errorCode)
            {
                lock (Sync) Fail("log4net script detail " + errorCode + ": " + message, exception);
            }
        }
    }
}
