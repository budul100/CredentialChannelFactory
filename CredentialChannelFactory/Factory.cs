using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace CredentialChannelFactory
{
    public class Factory<T>
        : IDisposable
        where T : class, IChannel
    {
        #region Private Fields

        private const string HttpAddress = "http";
        private const string HttpsAddress = "https";

        private readonly ChannelFactory<T> channelFactory;

        private bool disposed = false;

        #endregion Private Fields

        #region Public Constructors

        public Factory(string url, string userName, string password, bool notIgnoreCertificateErrors = false)
        {
            if (!notIgnoreCertificateErrors)
            {
                IgnoreCertificateErrors();
            }

            var isHttps = url.StartsWith(HttpsAddress);

            channelFactory = GetChannelFactory(
                url: url,
                userName: userName,
                password: password,
                isHttps: isHttps);
        }

        public Factory(string host, int port, string path, string userName, string password, bool isHttps = true,
            bool notIgnoreCertificateErrors = false)
        {
            if (!notIgnoreCertificateErrors)
            {
                IgnoreCertificateErrors();
            }

            var url = GetUrl(
                host: host,
                port: port,
                path: path,
                ishttps: isHttps);

            channelFactory = GetChannelFactory(
                url: url,
                userName: userName,
                password: password,
                isHttps: isHttps);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
        }

        public T Get()
        {
            return channelFactory.CreateChannel() as T;
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    channelFactory.Close();
                }

                disposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static void IgnoreCertificateErrors()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => { return true; };
        }

        private ChannelFactory<T> GetChannelFactory(string url, string userName, string password, bool isHttps)
        {
            var binding = isHttps
                ? GetHttpsBinding()
                : GetHttpBinding();

            var address = new EndpointAddress(new Uri(url));

            var result = new ChannelFactory<T>(
                binding: binding,
                remoteAddress: address);

            result.Credentials.UserName.UserName = userName;
            result.Credentials.UserName.Password = password;

            return result;
        }

        private Binding GetHttpBinding()
        {
            var result = new BasicHttpBinding();

            result.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            result.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            result.MaxBufferSize = int.MaxValue;
            result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
            result.MaxReceivedMessageSize = int.MaxValue;
            result.AllowCookies = true;

            return result;
        }

        private Binding GetHttpsBinding()
        {
            var result = new BasicHttpsBinding();

            result.Security.Mode = BasicHttpsSecurityMode.Transport;
            result.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            result.MaxBufferSize = int.MaxValue;
            result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
            result.MaxReceivedMessageSize = int.MaxValue;
            result.AllowCookies = true;

            return result;
        }

        private string GetUrl(string host, int port, string path, bool ishttps)
        {
            var result = (ishttps ? HttpsAddress : HttpAddress) + $"://{host}:{port}/{path}";

            return result;
        }

        #endregion Private Methods
    }
}