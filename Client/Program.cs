using Client.Launching;
using Client.Resolution;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Client
{
    internal static class Program
    {
        private const int ExitInvalidArguments = 2;
        private const int ExitInstallationUnavailable = 3;
        private const int ExitDisplayUnavailable = 4;

        public static CMain Form;

        public static bool Restart;

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == StartupRecovery.ProbeArgument)
            {
                try { StartupRecovery.CheckDirectX(); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    Environment.ExitCode = 1;
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!SetWorkingDirectory()) return;

            LaunchOptions options;

            try { options = LaunchOptions.Parse(args); }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, GameLanguage.Get("Invalid launch arguments"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = ExitInvalidArguments;
                return;
            }

            try
            {
                // The build configuration must not change which file the client reads:
                // --test-config decides, so the launcher's selected target is the edited file.
                Settings.UseTestConfig = options.TestConfig;

                // The saved language decides the text of the messages below, so load it first.
                Settings.LoadLanguage();

                // A direct start takes the same lease as a launcher start: the client needs no
                // launcher to run, but an update still may not replace files under it.
                using InstallationLease installation = AcquireInstallationLease();

                if (installation == null) return;

                if (RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully == true) { }

                Packet.IsServer = false;

                Settings.Load();
                Settings.EnsureFirstLaunchConfig();
                Settings.ApplyLaunchOptions(options);

                if (!CheckResolutionSetting()) return;

                // Explicit arguments are written before the first connection, so a later start
                // without arguments reaches the same server with the same display settings.
                if (!Settings.SaveLaunchOverrides(options))
                {
                    string target = Path.GetFullPath(Settings.ConfigFile);
                    CMain.SaveError("[Startup] Launch arguments could not be written to " + target);
                    MessageBox.Show(GameLanguage.Get("The launch settings could not be saved:") + Environment.NewLine + target +
                                    Environment.NewLine + Environment.NewLine +
                                    GameLanguage.Get("The game will use them for this session only."),
                                    GameLanguage.Get("Unable to save settings"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (!StartupRecovery.EnsureDirectX()) return;

                Application.Run(Form = new CMain());

                Settings.Save();

                if (Restart)
                {
                    Application.Restart();
                }
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                StartupRecovery.ShowFatal(ex);
            }
        }

        /// <summary>
        /// The game reads and writes everything relative to its own folder, which may be a
        /// network share that is no longer reachable.
        /// </summary>
        private static bool SetWorkingDirectory()
        {
            try
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                       ex is ArgumentException || ex is NotSupportedException)
            {
                MessageBox.Show(GameLanguage.Get("The game cannot open its installation folder:") + Environment.NewLine +
                                AppContext.BaseDirectory + Environment.NewLine + Environment.NewLine + ex.Message,
                                GameLanguage.Get("Unable to start game"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = ExitInstallationUnavailable;
                return false;
            }
        }

        private static InstallationLease AcquireInstallationLease()
        {
            try
            {
                return InstallationLease.Acquire(AppContext.BaseDirectory);
            }
            catch (IOException ex)
            {
                StartupRecovery.Log(ex);
                MessageBox.Show(ex.Message, GameLanguage.Get("Unable to start game"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = ExitInstallationUnavailable;
                return null;
            }
        }

        /// <summary>
        /// Keep the saved resolution inside the choices the client offers and the monitor can show.
        /// A resolution from an older configuration or a different monitor is corrected once and
        /// stored, so the next start does not repeat the check.
        /// </summary>
        private static bool CheckResolutionSetting()
        {
            if (!DisplayResolutions.GetDisplayResolutions())
            {
                MessageBox.Show(GameLanguage.Get("Could not get display resolutions"), GameLanguage.Get("Get Display Resolution Issue"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = ExitDisplayUnavailable;
                return false;
            }

            ResolutionOptions.Option option = ResolutionOptions.Resolve(Settings.Resolution, Settings.ResolutionHeight, DisplayResolutions.MaxSupportedWidth());

            if (option.Width == Settings.Resolution && option.Height == Settings.ResolutionHeight) return true;

            CMain.SaveError(string.Format("[Startup] Resolution {0}x{1} is not available; using {2}x{3}",
                                          Settings.Resolution, Settings.ResolutionHeight, option.Width, option.Height));

            Settings.Resolution = option.Width;
            Settings.ResolutionHeight = option.Height;
            Settings.Save();

            return true;
        }

        public static class RuntimePolicyHelper
        {
            public static bool LegacyV2RuntimeEnabledSuccessfully { get; private set; }

            static RuntimePolicyHelper()
            {
                //ICLRRuntimeInfo clrRuntimeInfo =
                //    (ICLRRuntimeInfo)RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                //        Guid.Empty,
                //        typeof(ICLRRuntimeInfo).GUID);

                //try
                //{
                //    clrRuntimeInfo.BindAsLegacyV2Runtime();
                //    LegacyV2RuntimeEnabledSuccessfully = true;
                //}
                //catch (COMException)
                //{
                //    // This occurs with an HRESULT meaning 
                //    // "A different runtime was already bound to the legacy CLR version 2 activation policy."
                //    LegacyV2RuntimeEnabledSuccessfully = false;
                //}
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
            private interface ICLRRuntimeInfo
            {
                void xGetVersionString();
                void xGetRuntimeDirectory();
                void xIsLoaded();
                void xIsLoadable();
                void xLoadErrorString();
                void xLoadLibrary();
                void xGetProcAddress();
                void xGetInterface();
                void xSetDefaultStartupFlags();
                void xGetDefaultStartupFlags();

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void BindAsLegacyV2Runtime();
            }
        }
    }
}
