using System.Text;

namespace Server
{
    /// <summary>
    /// Code pages .NET Core no longer ships by default. Scripts and drop files written before the game
    /// moved to UTF-8 are GBK or GB18030, so the provider has to be registered before those names resolve;
    /// without it Encoding.GetEncoding(936) throws NotSupportedException at the point of use.
    /// </summary>
    public static class TextEncodings
    {
        static TextEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>Code page 936: what the older script and list files were saved as.</summary>
        public static Encoding Gbk
        {
            get { return Encoding.GetEncoding(936); }
        }

        /// <summary>Code page 54936: GBK's superset, used by the drop file importer.</summary>
        public static Encoding Gb18030
        {
            get { return Encoding.GetEncoding(54936); }
        }
    }
}
