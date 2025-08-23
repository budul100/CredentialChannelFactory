using CredentialChannelFactory.Configuration;
using CredentialChannelFactory.Endpoint;

namespace CredentialChannelFactory.Tests.Helpers
{
    public static class TestHelper
    {
        #region Public Methods

        public static WcfConfiguration CreateTestConfiguration()
        {
            return new WcfConfiguration
            {
                MaxBufferSize = 1024 * 1024,
                MaxReceivedMessageSize = 1024 * 1024,
                SendTimeout = TimeSpan.FromSeconds(30),
                AllowCookies = true,
                IgnoreCertificateErrors = true // Only for testing!
            };
        }

        public static WcfEndpoint CreateTestEndpoint(bool isHttps = true)
        {
            return new WcfEndpoint(
                host: "test.example.com",
                port: isHttps ? 443 : 80,
                path: "TestService.svc",
                isHttps: isHttps);
        }

        #endregion Public Methods
    }
}