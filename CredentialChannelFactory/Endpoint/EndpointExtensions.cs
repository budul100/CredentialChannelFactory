using System.ServiceModel.Channels;
using Microsoft.Extensions.Logging;
using CredentialChannelFactory.Configuration;

namespace CredentialChannelFactory.Endpoint
{
    /// <summary>
    /// Provides extension methods for WCF endpoints to create factory instances with credentials.
    /// </summary>
    public static class EndpointExtensions
    {
        #region Public Methods

        /// <summary>
        /// Creates a new <see cref="Factory{T}"/> instance for the specified WCF endpoint and credentials.
        /// </summary>
        /// <typeparam name="T">The WCF service contract interface type that implements <see cref="IChannel"/>.</typeparam>
        /// <param name="endpoint">The WCF endpoint configuration containing host, port, and path information.</param>
        /// <param name="userName">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="config">Optional WCF configuration settings. If default, default settings are used.</param>
        /// <param name="logger">Optional logger instance for the factory. If default, a NullLogger is used.</param>
        /// <returns>A new <see cref="Factory{T}"/> instance configured with the specified parameters.</returns>
        public static Factory<T> CreateWcfFactory<T>(this WcfEndpoint endpoint, string userName, string password,
            WcfConfiguration config = default, ILogger<Factory<T>> logger = default)
            where T : class, IChannel
        {
            return new Factory<T>(
                endpoint: endpoint,
                userName: userName,
                password: password,
                configuration: config,
                logger: logger);
        }

        #endregion Public Methods
    }
}