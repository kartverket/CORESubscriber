﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using CORESubscriber.Xml;

namespace CORESubscriber
{
    internal class Provider
    {
        internal static string ConfigFile;

        internal static XDocument ConfigFileXml { get; set; }

        internal static string Password { get; set; }

        internal static string User { get; set; }

        internal static string ApiUrl { get; set; }

        internal static XNamespace GeosynchronizationNamespace = XmlNamespaces.Geosynchronization;

        internal static XNamespace ChangelogNamespace = XmlNamespaces.Changelog;

        internal static void Save(IEnumerable<XElement> datasetsList)
        {
            ConfigFileXml = File.Exists(ConfigFile)
                ? ReadConfigFile()
                : new XDocument(
                    new XComment(
                        "Settings for Provider. Don't edit attributes unless you know what you're doing! SubscriberLastIndex is -1 to indicate first synchronization. Under normal circumstances only the text-value of the elements wfsClient and subscribed should be manually edited."),
                    CreateDefaultProvider());

            AddDatasetsToDocument(datasetsList, ConfigFileXml);

            Save();
        }

        internal static void Save()
        {
            using (var fileStream = new FileStream(ConfigFile, FileMode.OpenOrCreate))
            {
                ConfigFileXml.Save(fileStream);
            }
        }

        internal static void ReadSettings()
        {
            ConfigFileXml = ReadConfigFile();

            var provider = ConfigFileXml.Descendants(XmlElements.Provider).First();

            Password = provider.Attribute(XmlAttributes.Password)?.Value;

            User = provider.Attribute(XmlAttributes.User)?.Value;

            ApiUrl = provider.Attribute(XmlAttributes.Uri)?.Value;

            GeosynchronizationNamespace = provider.Attribute(XmlAttributes.GeosynchronizationNamespace)?.Value;

            ChangelogNamespace = provider.Attribute(XmlAttributes.ChangelogNamespace)?.Value;
        }

        private static XDocument ReadConfigFile()
        {
            return XDocument.Parse(File.ReadAllText(ConfigFile));
        }

        private static void AddDatasetsToDocument(IEnumerable<XElement> datasetsList, XContainer datasetsDocument)
        {
            foreach (var xElement in datasetsList)
            {
                if (datasetsDocument.Descendants(XmlElements.Provider).Descendants().Any(d =>
                    Capabilities.Fields.All(f =>
                        d.Attribute(f)?.Value == xElement.Attribute(f)?.Value)
                ))
                    continue;

                // ReSharper disable once PossibleNullReferenceException
                xElement.Attribute(XmlAttributes.Namespace).Value =
                    GetNamespaceFromApplicationSchema(xElement.Attribute(XmlAttributes.ApplicationSchema)?.Value);

                datasetsDocument.Descendants(XmlElements.Provider)
                    .First()?.Add(xElement);
            }
        }

        private static string GetNamespaceFromApplicationSchema(string applicationSchema)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var result = client.GetAsync(applicationSchema).Result;

                    var xsd = XDocument.Parse(result.Content.ReadAsStringAsync().Result);

                    return xsd.Root?.Attribute(XmlAttributes.TargetNamespace)?.Value;
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        private static XElement CreateDefaultProvider()
        {
            var providerElement = new XElement(XmlElements.Provider);

            providerElement.Add(new List<object>
            {
                new XAttribute(XmlAttributes.Uri, ApiUrl),
                new XAttribute(XmlAttributes.User, User),
                new XAttribute(XmlAttributes.Password, Password),
                new XAttribute(XmlAttributes.GeosynchronizationNamespace, GeosynchronizationNamespace),
                new XAttribute(XmlAttributes.ChangelogNamespace, ChangelogNamespace)
            });

            return providerElement;
        }
    }
}