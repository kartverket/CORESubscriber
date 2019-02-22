using System.Linq;
using CORESubscriber.Xml;

namespace CORESubscriber.SoapAction
{
    public class GetChangelog
    {
        public static string Run()
        {
            var getChangelog = SoapRequest.GetSoapContentByAction(SoapActions.GetChangelog);

            getChangelog.Descendants(Provider.GeosynchronizationNamespace + XmlAttributes.ChangelogId.LocalName).First()
                    .Value =
                Dataset.OrderedChangelogId;

            var responseContent = SoapRequest.Send(SoapActions.GetChangelog, getChangelog);

            var returnValue = responseContent
                .Descendants(Provider.GeosynchronizationNamespace + XmlElements.DownloadUri.LocalName).First().Value;

            return returnValue;
        }
    }
}