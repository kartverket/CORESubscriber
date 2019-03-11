using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
        public static string DataFolder { get; set; }

        private static int Transaction { get; set; }

        internal static async Task Get(string downloadUrl)
        {
            var zipFilePath = GetZipFilePath(downloadUrl);

            await DownloadZipFileDownload(downloadUrl, zipFilePath);

            ExtractFilesToDatafolder(zipFilePath);
        }

        public static void ExtractFilesToDatafolder(string zipFilePath)
        {
            if (Directory.Exists(DataFolder)) Directory.Delete(DataFolder, true);

            ZipFile.ExtractToDirectory(zipFilePath, DataFolder, Encoding.UTF8);
        }

        private static async Task DownloadZipFileDownload(string downloadUrl, string zipFilePath)
        {
            var result = GetResult(downloadUrl);

            if (!result.IsSuccessStatusCode)
                throw new FileNotFoundException(
                    $"Statuscode when trying to download from {downloadUrl} was {result.StatusCode}");

            await SaveZipFile(zipFilePath, result);

        }

        internal static void Run()
        {
            var directoryInfo = new DirectoryInfo(DataFolder);

            directoryInfo.GetDirectories().ToList().ForEach(ReadFiles);

            ReadFiles(directoryInfo);
        }

        private static void ReadFiles(DirectoryInfo directoryInfo)
        {
            directoryInfo.GetDirectories().ToList().ForEach(directory =>
                directory.GetFiles().ToList().ForEach(DoTransactions));
        }

        private static XDocument GetChangelogXml(string xmlFilePath)
        {
            return XDocument.Load(xmlFilePath);
        }

        private static string GetEndIndex(XContainer changeLogXml)
        {
            return changeLogXml.Descendants().Attributes(XmlAttributes.EndIndex).First().Value;
        }

        private static void DoTransactions(FileInfo fileInfo)
        {
            var xmlFilePath = fileInfo.FullName;
            var changelogXml = GetChangelogXml(xmlFilePath);

            Dataset.SetEndindex(GetEndIndex(changelogXml));

            changelogXml.Descendants(Provider.ChangelogNamespace + "transactions")
                .ToList().ForEach(PrepareAndSendTransaction);
        }

        private static void PrepareAndSendTransaction(XElement transaction)
        {
            var abortedTransaction = Dataset.GetTransaction();

            if (abortedTransaction > Transaction)
            {
                Console.WriteLine($"Transaction {Transaction} is lower than abortedTransaction ({abortedTransaction}). Skipping");

                Transaction++;

                return;
            }

            Send(SetTransactionValues(transaction));

            Transaction++;

            Dataset.SetTransaction(Transaction.ToString());
        }

        private static XDocument SetTransactionValues(XElement transaction)
        {
            transaction.Name = XmlNamespaces.Wfs + "Transaction";

            transaction.SetAttributeValue("version", "2.0.0");

            var xTransaction = new XDocument(transaction);
            string lastTranPath = Path.Combine($"{Config.DownloadFolder}", "lastTransaction.xml");
            System.IO.File.Delete(lastTranPath);
            xTransaction.Save(lastTranPath);

            return xTransaction;
        }

        private static void Send(XDocument transactionDocument)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                var request = new HttpRequestMessage(HttpMethod.Post, Dataset.GetWfsClient());
                Stream stream = new MemoryStream();
                transactionDocument.Save(stream);
                stream.Position = 0;
                request.Content = new StreamContent(stream);


                var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;

                var responseMessage = GetResponseMessage(response);

                if (!response.IsSuccessStatusCode)
                    throw new TransactionAbortedException(
                        $"Transaction failed. Message from WFS-server: \r\n{responseMessage}");

                WriteTransactionSummaryToConsole(responseMessage);
            }
        }

        private static string GetResponseMessage(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync().Result;
        }

        private static void WriteTransactionSummaryToConsole(string responseMessage)
        {
            Console.WriteLine(XDocument.Parse(responseMessage).Descendants(XmlNamespaces.Wfs + "TransactionSummary").First().ToString());
        }

        private static async Task SaveZipFile(string zipFilePath, HttpResponseMessage result)
        {
            using (var fs = new FileStream(zipFilePath, FileMode.Create)) await result.Content.CopyToAsync(fs);

            Dataset.SetChangelogPath(DataFolder);
        }

        private static string GetZipFilePath(string downloadUrl)
        {
            var changelogFileName = GetChangelogFileNameFromDownloadUrl(downloadUrl);

            SetDataFolder(changelogFileName);

            return Config.DownloadFolder + "/" + changelogFileName;
        }

        public static void SetDataFolder(string changelogFileName)
        {
            DataFolder = Config.DownloadFolder + "/" + changelogFileName.Split(".")[0];
        }

        private static string GetChangelogFileNameFromDownloadUrl(string downloadUrl)
        {
            return downloadUrl.Split('/')[downloadUrl.Split('/').Length - 1];
        }

        private static HttpResponseMessage GetResult(string downloadUrl)
        {
            using (var client = SetCredentials(new HttpClient())) return client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).Result;
        }

        private static HttpClient SetCredentials(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(Provider.User + ":" + Provider.Password)));

            return client;
        }
    }
}