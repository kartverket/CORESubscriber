﻿using System;
using System.Linq;
using System.Xml.Linq;
using CORESubscriber.Xml;

// ReSharper disable PossibleNullReferenceException

namespace CORESubscriber.SoapAction
{
    internal class OrderChangelog2
    {
        public static void Run()
        {
            if (Dataset.OrderedChangelogId != -1) return;

            var responseContent =
                SoapRequest.Send(SoapActions.OrderChangelog2, SetOrderVariables(SoapRequest.GetSoapContentByAction(SoapActions.OrderChangelog2)));

            Dataset.OrderedChangelogId =
                Convert.ToInt64(responseContent
                    .Descendants(Provider.GeosynchronizationNamespace + XmlAttributes.ChangelogId.LocalName).First().Value);

            Provider.ConfigFileXml.Descendants(XmlElements.Dataset)
                    .First(d => d.Attribute(XmlAttributes.DatasetId)?.Value == Dataset.Id)
                    .Descendants(XmlElements.AbortedChangelog).First().Attribute(XmlAttributes.ChangelogId)
                    .Value =
                Dataset.OrderedChangelogId.ToString();

            Provider.Save();
        }

        private static XDocument SetOrderVariables(XDocument orderChangelog)
        {
            orderChangelog.Descendants(Provider.GeosynchronizationNamespace + XmlElements.Order.LocalName).First()
                    .Attribute(XmlAttributes.StartIndex).Value =
                (Dataset.SubscriberLastIndex + 1).ToString();

            orderChangelog.Descendants(Provider.GeosynchronizationNamespace + XmlAttributes.DatasetId.LocalName).First()
                .Value = Dataset.Id;

            orderChangelog.Descendants(Provider.GeosynchronizationNamespace + XmlElements.Order.LocalName).First()
                    .Attribute(XmlAttributes.Count).Value =
                Config.OrderedChangeCount;

            orderChangelog.Descendants(Provider.GeosynchronizationNamespace + XmlAttributes.DatasetVersion.LocalName).First()
                .Value = Dataset.Version;

            return orderChangelog;
        }
    }
}