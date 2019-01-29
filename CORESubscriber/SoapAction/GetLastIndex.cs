using System;
using System.Linq;
using CORESubscriber.Xml;

namespace CORESubscriber.SoapAction
{
    internal class GetLastIndex
    {
        public static bool Run()
        {
            var xDocument = SoapRequest.GetSoapContentByAction(SoapActions.GetLastIndex);

            xDocument.Descendants(Provider.GeosynchronizationNamespace + XmlAttributes.DatasetId.LocalName).First()
                .Value = Dataset.Id;

            Dataset.SetProviderLastIndex(GetLastIndexFromProvider(xDocument));

            return Dataset.ProviderLastIndex > Dataset.SubscriberLastIndex;
        }

        private static long GetLastIndexFromProvider(System.Xml.Linq.XDocument xDocument)
        {
            return Convert.ToInt64(SoapRequest.Send(SoapActions.GetLastIndex, xDocument)
                .Descendants(Provider.GeosynchronizationNamespace + XmlElements.Return.LocalName).First().Value);
        }
    }
}