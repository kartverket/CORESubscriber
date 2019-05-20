using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CORESubscriber.SoapAction;
using CORESubscriber.Xml;

namespace CORESubscriber
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                    Run(args);

                else
                    WriteHelp();
            }
            catch (Exception e)
            {
                HandleExceptionText(e);
            }
        }

        private static void HandleExceptionText(Exception e)
        {
            Console.WriteLine(e.Message);

            using (var file = File.Exists(Config.ErrorLog)
                ? File.Open(Config.ErrorLog, FileMode.Append)
                : File.Open(Config.ErrorLog, FileMode.CreateNew))
            using (var stream = new StreamWriter(file))
                stream.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") + ":\r\n\t" + e.Message + ":\r\n" +
                                 e.StackTrace);
        }

        private static void Run(IReadOnlyList<string> args)
        {
            switch (args[0].ToLower())
            {
                case "sync":

                    SetSyncVariables(args);

                    SynchronizeSubscribedDatasets();

                    break;

                case "add":

                    SetAddVariables(args);

                    GetCapabilities.Run();

                    break;

                default:

                    WriteHelp();

                    break;
            }
        }

        private static void SetSyncVariables(IReadOnlyList<string> args)
        {
            Provider.ConfigFile = args[1];

            if (args.Count > 2) Config.DownloadFolder = args[2];

            Provider.ReadSettings();
        }

        private static void SetAddVariables(IReadOnlyList<string> args)
        {
            Provider.ApiUrl = args[1];

            Provider.User = args[2];

            Provider.Password = args[3];

            if (args.Count > 4) Provider.ConfigFile = args[4];
        }

        private static void SynchronizeSubscribedDatasets()
        {
            var timer = StartTimer();

            var datasetsUpdated = new List<string>();

            var datasetsFailed = new List<string>();

            foreach (var subscribedElement in GetSubscribedElements())
            {
                try
                {
                    datasetsUpdated.Add(SynchronizeDataset(subscribedElement));
                }
                catch (Exception e)
                {
                    datasetsFailed.Add(Dataset.GetDatasetIdFromElement(subscribedElement));

                    HandleExceptionText(e);
                }
            }
            
            timer.Stop();

            WriteStatus(datasetsUpdated.Any(d => !string.IsNullOrWhiteSpace(d)), datasetsFailed, timer);
        }

        private static void WriteStatus(bool updatesFound, IReadOnlyCollection<string> datasetsFailed, Stopwatch timer)
        {
            if (updatesFound)
            {
                Console.WriteLine($"INFO: Time used: {timer.Elapsed}");

                return;
            }

            if (datasetsFailed.Count > 0)
            {
                Console.WriteLine($"ERROR: Datasets failed \r\n\t{string.Join(", ",datasetsFailed.Select(d => d).ToList())}");

                return;
            }

            Console.WriteLine("INFO: All datasets are up to date");
        }

        private static string SynchronizeDataset(XObject subscribed)
        {
            if (Dataset.ReadVariables(subscribed))
                if (!GetLastIndex.Run())
                    return null;

            WriteDatasetInformation();

            GetChangelog();

            Changelog.Run();

            Dataset.ProviderLastIndex = Dataset.GetProviderLastIndex();

            Dataset.SaveFinalSettings();

            return Dataset.GetDatasetIdFromElement(subscribed);
        }

        private static void GetChangelog()
        {
            var abortedChangelogId = Dataset.GetAbortedChangelogId();

            var changelogPath = Dataset.GetChangelogPath();

            if (abortedChangelogId == Dataset.EmptyValue)
            {
                GetChangelogFromProvider();

                return;
            }

            if (changelogPath == string.Empty)
            {
                Dataset.OrderedChangelogId = abortedChangelogId;

                WaitForChangelogAndDownload();

                return;
            }

            Changelog.DataFolder = changelogPath;
        }

        private static void WaitForChangelogAndDownload()
        {
            GetChangelogStatus.Run();

            Changelog.Get(SoapAction.GetChangelog.Run()).Wait();
        }

        private static void WriteDatasetInformation()
        {
            Console.WriteLine(
                $"Dataset: {Dataset.Id}\r\n\tProvider LastIndex: {Dataset.ProviderLastIndex}\r\n\tSubscriber Lastindex: {Dataset.SubscriberLastIndex}");
        }

        private static void GetChangelogFromProvider()
        {
            if (Provider.GeosynchronizationNamespace == XmlNamespaces.Geosynchronization11) OrderChangelog.Run();

            else OrderChangelog2.Run();

            WaitForChangelogAndDownload();
        }

        private static Stopwatch StartTimer()
        {
            var timer = new Stopwatch();
            timer.Start();
            return timer;
        }

        private static IEnumerable<XElement> GetSubscribedElements()
        {
            return Provider.ConfigFileXml.Descendants(XmlElements.Dataset).Descendants(XmlElements.Subscribed)
                .Where(s => string.Equals(s.Value.ToString(), bool.TrueString,
                    StringComparison.CurrentCultureIgnoreCase));
        }

        private static void WriteHelp()
        {
            Console.WriteLine("Syntax: action [parameters]");

            Console.WriteLine();

            foreach (var action in Config.Actions)
            {
                Console.WriteLine(action.Key + ":");

                foreach (var parameter in action.Value) Console.WriteLine("\t" + parameter);
            }
        }
    }
}