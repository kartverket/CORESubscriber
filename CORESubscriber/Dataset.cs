using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CORESubscriber.Xml;

// ReSharper disable PossibleNullReferenceException

namespace CORESubscriber
{
    internal class Dataset
    {
        internal static string EmptyValue = "0";

        internal static readonly List<object> DefaultElements = new List<object>
        {
            new XAttribute(XmlAttributes.Namespace, ""),

            new XAttribute(XmlAttributes.SubscriberLastIndex, EmptyValue),

            new XElement(XmlElements.AbortedChangelog,
                new XAttribute(XmlAttributes.EndIndex, ""),
                new XAttribute(XmlAttributes.Transaction, ""),
                new XAttribute(XmlAttributes.ChangelogPath, ""),
                new XAttribute(XmlAttributes.ChangelogId, EmptyValue)),

            new XElement(XmlElements.WfsClient, ""),

            new XElement(XmlElements.Subscribed, bool.FalseString),

            new XElement(XmlElements.Precision,
                new XAttribute(XmlAttributes.EpsgCode, ""),
                new XAttribute(XmlAttributes.Decimals, ""),
                new XAttribute(XmlAttributes.Tolerance, ""))
        };

        internal static string Id { get; set; }

        internal static string OrderedChangelogId { get; set; }

        internal static long ProviderLastIndex { get; set; }

        internal static long SubscriberLastIndex { get; set; }

        public static string Version { get; set; }

        internal static void UpdateSettings()
        {
            OrderedChangelogId = EmptyValue;

            SetSubscriberLastIndexToProviderLastIndex();

            SetAbortedchangelogToChangelogId();

            Provider.Save();
        }

        private static void SetSubscriberLastIndexToProviderLastIndex()
        {
            if (ProviderLastIndex < 1) return;

            GetDatasetConfig().Attribute(XmlAttributes.SubscriberLastIndex).Value = ProviderLastIndex.ToString();

            Provider.Save();
        }

        internal static bool ReadVariables(XObject subscribed)
        {
            Id = GetDatasetIdFromElement(subscribed);

            SubscriberLastIndex = GetSubscriberLastIndex(subscribed);

            OrderedChangelogId = GetAbortedChangelogId();

            Version = GetVersion(subscribed);

            return OrderedChangelogId == EmptyValue;
        }

        public static string GetDatasetIdFromElement(XObject subscribed)
        {
            return subscribed.Parent?.Attribute(XmlAttributes.DatasetId)?.Value;
        }

        private static string GetVersion(XObject subscribed)
        {
            return subscribed.Parent?.Attribute(XmlAttributes.Version)?.Value;
        }

        private static long GetSubscriberLastIndex(XObject subscribed)
        {
            return Convert.ToInt64(subscribed.Parent?.Attribute(XmlAttributes.SubscriberLastIndex)?.Value);
        }

        public static string GetAbortedChangelogId()
        {
            return GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog).Attribute(XmlAttributes.ChangelogId)
                .Value;
        }

        public static void SetChangelogPath(string dataFolder)
        {
            GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .SetAttributeValue(XmlAttributes.ChangelogPath, dataFolder);

            Provider.Save();
        }

        public static string GetChangelogPath()
        {
            return GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .Attribute(XmlAttributes.ChangelogPath).Value;
        }

        public static void SetEndindex(string endIndex)
        {
            GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .SetAttributeValue(XmlAttributes.EndIndex, endIndex);

            Provider.Save();
        }

        public static void SetTransaction(string transaction)
        {
            GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .SetAttributeValue(XmlAttributes.Transaction, transaction);

            Provider.Save();
        }

        public static int GetTransaction()
        {
            var transaction = GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .Attribute(XmlAttributes.Transaction).Value;
            if (string.IsNullOrEmpty(transaction)) return 0;

            return Convert.ToInt32(GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .Attribute(XmlAttributes.Transaction).Value);
        }

        private static void SetAbortedchangelogToChangelogId()
        {
            GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .SetAttributeValue(XmlAttributes.ChangelogId, EmptyValue);

            Provider.Save();
        }

        private static XElement GetDatasetConfigFirstDescendant(XName descendant)
        {
            return GetDatasetConfig().Descendants(descendant).First();
        }

        private static XElement GetDatasetConfig()
        {
            return Provider.ConfigFileXml.Descendants(XmlElements.Dataset)
                .First(d => d.Attribute(XmlAttributes.DatasetId)?.Value == Id);
        }

        public static void ResetAbortedChangelog()
        {
            SetChangelogPath("");

            SetEndindex("");

            SetTransaction("");
        }

        public static void SetOrderedChangelogId(string orderedChangelogId)
        {
            OrderedChangelogId = orderedChangelogId;

            GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog)
                .SetAttributeValue(XmlAttributes.ChangelogId, OrderedChangelogId);

            Provider.Save();
        }

        public static string GetWfsClient()
        {
            var wfsClient = GetDatasetConfigFirstDescendant(XmlElements.WfsClient).Value;

            if (wfsClient == "") throw new Exception($"No wfsClient given for dataset {Id}");

            return wfsClient;
        }

        public static bool ChangelogIdIsDefault()
        {
            return OrderedChangelogId == EmptyValue;
        }

        public static void SetAbortedChangelog(string orderedChangelogId)
        {
            GetDatasetConfigFirstDescendant(XmlElements.AbortedChangelog).Attribute(XmlAttributes.ChangelogId)
                    .Value =
                orderedChangelogId;

            Provider.Save();
        }

        public static void SetProviderLastIndex(long providerLastIndex)
        {
            ProviderLastIndex = providerLastIndex;
        }
    }
}