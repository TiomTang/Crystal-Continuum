using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client.Resolution
{
    internal static class DisplayResolutions
    {
        internal static List<eSupportedResolution> DisplaySupportedResolutions = new List<eSupportedResolution>();

        internal static bool GetDisplayResolutions()
        {
            bool parsedOK = false;

            // Repeated calls replace the previous result instead of appending to it.
            DisplaySupportedResolutions.Clear();

            var supportedResolutions = Enum.GetNames(typeof(eSupportedResolution));
            try
            {
                List<string> list = new();

                DEVMODE vDevMode = new DEVMODE();
                int i = 0;
                while (EnumDisplaySettings(null, i, ref vDevMode))
                {
                    string displayResolution = $"w{vDevMode.dmPelsWidth}h{vDevMode.dmPelsHeight}";

                    if(supportedResolutions.Contains(displayResolution))
                    {
                        if (!list.Contains(displayResolution))
                        {
                            list.Add(displayResolution);
                        }
                    }
                    i++;
                }

                if (list.Count > 0)
                {
                    foreach (string displayResolution in list)
                    {
                        eSupportedResolution resolution;
                        if (Enum.TryParse(displayResolution, true, out resolution))
                        {
                            DisplaySupportedResolutions.Add(resolution);
                        }
                    }
                }

                if (DisplaySupportedResolutions.Count > 0)
                {
                    parsedOK = true;
                }
                
            }
            catch
            {
                parsedOK = false;
            }

            return parsedOK;
        }

        /// <summary>
        /// Widest width the monitor reports among the choices the client offers, used to fall back
        /// from a saved resolution the monitor cannot show. Zero when nothing was enumerated.
        /// </summary>
        internal static int MaxSupportedWidth()
        {
            int widest = 0;

            foreach (eSupportedResolution resolution in DisplaySupportedResolutions)
                if ((int)resolution > widest)
                    widest = (int)resolution;

            return widest;
        }

        internal static bool IsSupported(int resolution)
        {
            return IsSupported(resolution.ToString());
        }

        internal static bool IsSupported(string resolution)
        {
            eSupportedResolution res;
            if (!Enum.TryParse(resolution, true, out res))
            {
                return false;
            }

            if(!Enum.IsDefined(typeof(eSupportedResolution), res))
            {
                return false;
            }
            return true;
        }

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplaySettings(
              string deviceName, int modeNum, ref DEVMODE devMode);
        const int ENUM_CURRENT_SETTINGS = -1;

        const int ENUM_REGISTRY_SETTINGS = -2;

        [StructLayout(LayoutKind.Sequential)]
        internal struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }
    }
}
