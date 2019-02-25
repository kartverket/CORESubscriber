using System;
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

        private static XDocument GetChangelogXml(FileInfo fileInfo)
        {
            return XDocument.Parse(fileInfo.OpenText().ReadToEnd());
        }

        private static string GetEndIndex(XContainer changeLogXml)
        {
            return changeLogXml.Descendants().Attributes(XmlAttributes.EndIndex).First().Value;
        }

        private static void DoTransactions(FileInfo fileInfo)
        {
            var changelogXml = GetChangelogXml(fileInfo);

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

            File.WriteAllText($"{Config.DownloadFolder}/lastTransaction.xml", xTransaction.ToString());

            return xTransaction;
        }

        private static void Send(XNode transactionDocument)
        {
            using (var client = new HttpClient())
            {
                var response = client.PostAsync(Dataset.GetWfsClient(), GetHttpContent(transactionDocument));

                if (!response.Result.IsSuccessStatusCode)
                    throw new TransactionAbortedException(
                        $"Transaction failed. Message from WFS-server: \r\n{GetResponseErrorMessage(response)}");

                WriteTransactionSummaryToConsole(response);
            }
        }

        private static StringContent GetHttpContent(XNode transactionDocument)
        {
            return new StringContent(transactionDocument.ToString(), Encoding.UTF8, Config.XmlMediaType);
        }

        private static string GetResponseErrorMessage(Task<HttpResponseMessage> response)
        {
            return response.Result.Content.ReadAsStringAsync().Result;
        }

        private static void WriteTransactionSummaryToConsole(Task<HttpResponseMessage> response)
        {
            Console.WriteLine(XDocument.Parse(response.Result.Content.ReadAsStringAsync().Result)
                .Descendants(XmlNamespaces.Wfs + "TransactionSummary").First().ToString());
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