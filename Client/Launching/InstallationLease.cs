using System;
using System.IO;

namespace Client.Launching
{
    /// <summary>
    /// Installation lease held for the whole run of the game. The launcher and its update helper
    /// take the same file exclusively while they replace files, so an update cannot start under a
    /// running game and several game instances may still share the installation.
    /// Nothing here needs the launcher to be present; a direct start takes the same lease.
    /// </summary>
    internal sealed class InstallationLease : IDisposable
    {
        internal const string FileName = ".mir2-install.lock";

        private const string TransactionDirectory = ".mir2-transaction";

        private const string PendingUpdateDirectory = ".launcher-update";

        private const string PendingUpdateFileName = "pending.json";

        private readonly FileStream _stream;

        private InstallationLease(FileStream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Take the shared read lease. Throws <see cref="IOException"/> with a message meant for
        /// the player when the installation is being replaced or an earlier update is unfinished.
        /// </summary>
        internal static InstallationLease Acquire(string directory)
        {
            string root = Path.GetFullPath(directory);

            EnsureNotReparsePoint(root);

            string path = Path.Combine(root, FileName);

            EnsureNotReparsePoint(path);

            FileStream stream;

            try
            {
                stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException(GameLanguage.Get("The game cannot create its installation lock in {0}. Check the folder permissions or start the game from a writable copy.", root), ex);
            }
            catch (IOException ex)
            {
                throw new IOException(GameLanguage.Get("Another program is replacing or repairing this installation. Wait for it to finish and start the game again."), ex);
            }

            if (Directory.Exists(Path.Combine(root, TransactionDirectory)) ||
                File.Exists(Path.Combine(root, PendingUpdateDirectory, PendingUpdateFileName)))
            {
                stream.Dispose();
                throw new IOException(GameLanguage.Get("An interrupted update is waiting in {0}. Start the launcher once so it can finish recovery before the game runs.", root));
            }

            return new InstallationLease(stream);
        }

        /// <summary>
        /// Reparse points would let the lock live outside the installation it is supposed to guard.
        /// </summary>
        private static void EnsureNotReparsePoint(string path)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException(GameLanguage.Get("The installation path {0} is a reparse point; refusing to lease it.", path));
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException(GameLanguage.Get("The game cannot read its installation folder {0}. Check the folder permissions or start the game from a readable copy.", path), ex);
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
