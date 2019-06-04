using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using CORESubscriber.Xml;

namespace CORESubscriber.SoapAction
{
    public class SoapRequest
    {
        public static XDocument GetSoapContentByAction(string action)
        {
            var actionText = File.ReadAllText("Queries/" + action + ".xml");

            actionText = actionText.Replace(XmlNamespaces.Geosynchronization.NamespaceName,
                Provider.GeosynchronizationNamespace.NamespaceName);

            var soapContent = XDocument.Parse(actionText);

            return soapContent;
        }

        public static XDocument Send(string action, XDocument requestContent)
        {
            var request = CreateRequest(requestContent, action);

            var client = GetClient();

            var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;

            var responseMessage = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode) throw new WebException(
                $"Query failed. Message from Provider: \r\n{responseMessage}");
            
            return XDocument.Parse(responseMessage);
        }

        private static HttpClient GetClient()
        {
            var byteArray = Encoding.ASCII.GetBytes(Provider.User + ":" + Provider.Password);

            var client = new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Config.XmlMediaType));

            return client;
        }

        private static HttpRequestMessage CreateRequest(XDocument soapXml, string action)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(Provider.ApiUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(soapXml.ToString(), Encoding.UTF8, Config.XmlMediaType),
            };

            request.Headers.Clear();

            request.Content.Headers.ContentType = new MediaTypeHeaderValue(Config.XmlMediaType);

            request.Headers.Add("SOAPAction", Provider.GeosynchronizationNamespace.NamespaceName + "/#" + action);

            return request;
        }
    }
}