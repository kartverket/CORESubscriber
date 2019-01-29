using System;
using System.Linq;
using System.Threading.Tasks;
using CORESubscriber.Xml;

namespace CORESubscriber.SoapAction
{
    public class GetChangelogStatus
    {
        public static void Run()
        {
            var getChangelogStatus = SoapRequest.GetSoapContentByAction(SoapActions.GetChangelogStatus);

            getChangelogStatus.Descendants(Provider.GeosynchronizationNamespace + XmlAttributes.ChangelogId.LocalName)
                    .First().Value =
                Dataset.OrderedChangelogId;

            var queryCounter = 1;

            while (true)
            {
                var returnValue = GetChangelogStatusResponseValue(getChangelogStatus);

                Console.WriteLine("Query " + queryCounter + ": changelog with ID " + Dataset.OrderedChangelogId +
                                  " is " + returnValue);

                switch (returnValue)
                {
                    case "queued":
                    case "working":
                        Task.Delay(Config.StatusQueryDelay * queryCounter++).Wait();
                        continue;
                    case "finished":
                        return;
                    default:
                        throw new Exception("Status for changelog with ID " + Dataset.OrderedChangelogId + ": " +
                                            returnValue);
                }
            }
        }

        private static string GetChangelogStatusResponseValue(System.Xml.Linq.XDocument getChangelogStatus)
        {
            return GetResponseContent(getChangelogStatus).Descendants(Provider.GeosynchronizationNamespace + "return").First()
                .Value;
        }

        private static System.Xml.Linq.XDocument GetResponseContent(System.Xml.Linq.XDocument getChangelogStatus)
        {
            return SoapRequest.Send(SoapActions.GetChangelogStatus, getChangelogStatus);
        }
    }
}