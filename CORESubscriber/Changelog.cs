﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using CORESubscriber.Xml;

namespace CORESubscriber
{
    internal class Changelog
    {
        private static string DataFolder { get; set; }

        private static string WfsClient { get; set; }

        internal static async Task Get(string downloadUrl)
        {
            string zipFile;

            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes(Provider.User + ":" + Provider.Password);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var result = client.GetAsync(downloadUrl).Result;

                if (!result.IsSuccessStatusCode)
                    throw new FileNotFoundException("Statuscode when trying to download from " +
                                                    downloadUrl + " was " + result.StatusCode);

                var changelogFileName = downloadUrl.Split('/')[downloadUrl.Split('/').Length - 1];

                zipFile = Config.DownloadFolder + "/" + changelogFileName;

                DataFolder = Config.DownloadFolder + "/" + changelogFileName.Split(".")[0];

                using (var fs = new FileStream(zipFile, FileMode.Create))
                {
                    await result.Content.CopyToAsync(fs);
                }
            }

            if (Directory.Exists(DataFolder)) Directory.Delete(DataFolder, true);

            ZipFile.ExtractToDirectory(zipFile, DataFolder);
        }

        internal static void Execute()
        {
            WfsClient = Provider.ConfigFileXml.Descendants()
                .First(d => d.Attribute(XmlAttributes.DatasetId)?.Value == Dataset.Id)
                .Descendants(XmlElements.WfsClient).First()
                .Value;

            if (WfsClient == "") throw new Exception("No wfsClient given for dataset " + Dataset.Id);

            var directoryInfo = new DirectoryInfo(DataFolder);

            foreach (var directory in directoryInfo.GetDirectories()) ReadFiles(directory.GetFiles());

            ReadFiles(directoryInfo.GetFiles());
        }

        private static void ReadFiles(IEnumerable<FileInfo> files)
        {
            foreach (var fileInfo in files)
            {
                var changelogXml = XDocument.Parse(fileInfo.OpenText().ReadToEnd());

                foreach (var transaction in changelogXml.Descendants(Provider.ChangelogNamespace + "transactions").ToList())
                {
                    transaction.Name = XmlNamespaces.Wfs + "Transaction";

                    Send(new XDocument(transaction));
                }
            }
        }

        private static void Send(XNode transactionDocument)
        {
            using (var client = new HttpClient())
            {
                var httpContent = new StringContent(transactionDocument.ToString(), Encoding.UTF8, Config.XmlMediaType);

                var response = client.PostAsync(WfsClient, httpContent);

                if (!response.Result.IsSuccessStatusCode)
                {
                    var errorMessage = response.Result.Content.ReadAsStringAsync().Result;

                    throw new TransactionAbortedException("Transaction failed. Message from WFS-server: \r\n" +
                                                          errorMessage);
                }

                Console.WriteLine(XDocument.Parse(response.Result.Content.ReadAsStringAsync().Result)
                    .Descendants(XmlNamespaces.Wfs + "TransactionSummary").First().ToString());
            }
        }
    }
}