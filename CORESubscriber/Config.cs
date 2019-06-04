using System.Collections.Generic;
using System.IO;
using CORESubscriber.Xml;

namespace CORESubscriber
{
    public class Config
    {
        internal const string XmlMediaType = "text/xml";

        internal static Dictionary<string, List<string>> Actions = new Dictionary<string, List<string>>
        {
            {
                "add", new List<string>
                {
                    XmlAttributes.Uri.LocalName,
                    XmlAttributes.User.LocalName,
                    XmlAttributes.Password.LocalName,
                    "outputFile"
                }
            },
            {
                "sync", new List<string>
                {
                    "configFile",
                    "tempFolder"
                }
            }
        };

        internal static string DownloadFolder = Path.GetTempPath();

        internal static string ErrorLog = "errorLog.txt";

        internal static int StatusQueryDelay = 300;

        public static string OrderedChangeCount = "10000";
    }
}