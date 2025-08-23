using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using CredentialChannelFactory.Configuration;
using CredentialChannelFactory.Endpoint;
using CredentialChannelFactory.Exceptions;
using Microsoft.Extensions.Logging;

namespace CredentialChannelFactory
{
    /// <summary>
    /// A generic factory class for creating WCF channels with credential-based authentication.
    /// Provides both synchronous and asynchronous channel creation methods with proper resource management.
    /// </summary>
    /// <typeparam name="T">The WCF service contract interface type that implements IChannel.</typeparam>
    public class Factory<T>
        : IDisposable, IAsyncDisposable
        where T : class, IChannel
    {
        #region Private Fields

        private const string HttpAddress = "http";
        private const string HttpsAddress = "https";

        private readonly ChannelFactory<T> channelFactory;
        private readonly WcfConfiguration configuration;
        private readonly ILogger<Factory<T>> logger;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private bool disposed;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the Factory class with the specified URL and credentials.
        /// </summary>
        /// <param name="url">The complete URL of the WCF service endpoint.</param>
        /// <param name="userName">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="configuration">Optional WCF configuration settings. If null, default settings are used.</param>
        /// <param name="logger">Optional logger instance. If null, a NullLogger is used.</param>
        /// <exception cref="ChannelCreationException">Thrown when the WCF factory cannot be created.</exception>
        public Factory(string url, string userName, string password, WcfConfiguration configuration = default,
            ILogger<Factory<T>> logger = default)
        {
            this.logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Factory<T>>.Instance;
            this.configuration = configuration ?? new WcfConfiguration();

            try
            {
                ConfigureCertificateValidation();

                var isHttps = url.StartsWith(HttpsAddress, StringComparison.OrdinalIgnoreCase);

                channelFactory = CreateChannelFactory(
                    url: url,
                    userName: userName,
                    password: password,
                    isHttps: isHttps);

                this.logger.LogInformation("WCF Factory created for URL: {Url}", url);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to create WCF Factory for URL: {Url}", url);
                throw new ChannelCreationException("Failed to create WCF Factory", ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the Factory class with the specified endpoint and credentials.
        /// </summary>
        /// <param name="endpoint">The WCF endpoint configuration containing host, port, and path information.</param>
        /// <param name="userName">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="configuration">Optional WCF configuration settings. If null, default settings are used.</param>
        /// <param name="logger">Optional logger instance. If null, a NullLogger is used.</param>
        /// <exception cref="ChannelCreationException">Thrown when the WCF factory cannot be created.</exception>
        public Factory(WcfEndpoint endpoint, string userName, string password,
            WcfConfiguration configuration = default, ILogger<Factory<T>> logger = default)
            : this(BuildUrl(endpoint), userName, password, configuration, logger)
        { }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether the factory has been disposed.
        /// </summary>
        public bool IsDisposed => disposed;

        /// <summary>
        /// Gets the lock object used for thread synchronization.
        /// </summary>
        public object LockObject { get; } = new object();

        /// <summary>
        /// Gets the URI of the WCF service endpoint.
        /// </summary>
        public Uri Uri { get; private set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Releases all resources used by the Factory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously releases all resources used by the Factory.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates a new WCF channel instance synchronously.
        /// This method is thread-safe and can be called multiple times to create multiple channels.
        /// </summary>
        /// <returns>A new instance of the WCF service channel.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the factory has been disposed.</exception>
        /// <exception cref="ChannelCreationException">Thrown when the channel cannot be created.</exception>
        public T Get()
        {
            ThrowIfDisposed();

            lock (LockObject)
            {
                ThrowIfDisposed();

                try
                {
                    var channel = channelFactory.CreateChannel();
                    logger.LogDebug("Channel created successfully");
                    return channel;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create channel");
                    throw new ChannelCreationException("Failed to create channel", ex);
                }
            }
        }

        /// <summary>
        /// Creates a new WCF channel instance asynchronously.
        /// This method is thread-safe and can be called multiple times to create multiple channels.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation that returns a new instance of the WCF service channel.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the factory has been disposed.</exception>
        /// <exception cref="ChannelCreationException">Thrown when the channel cannot be created.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
        public async Task<T> GetAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var semaphoreAcquired = false;
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                semaphoreAcquired = true;

                ThrowIfDisposed();

                var channel = await Task.Run(() =>
                {
                    try
                    {
                        return channelFactory.CreateChannel();
                    }
                    catch (OperationCanceledException)
                    {
                        // Re-throw cancellation exceptions without wrapping them
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create channel asynchronously");
                        throw new ChannelCreationException("Failed to create channel", ex);
                    }
                }, cancellationToken);

                logger.LogDebug("Channel created successfully (async)");
                return channel;
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions without logging them as errors
                logger.LogDebug("Channel creation was cancelled");
                throw;
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    semaphore.Release();
                }
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Releases the unmanaged resources used by the Factory and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        channelFactory?.Close();
                        logger.LogDebug("Channel factory closed");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Exception occurred while closing channel factory");
                        channelFactory?.Abort();
                    }
                    finally
                    {
                        semaphore?.Dispose();
                    }
                }

                disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (channelFactory != default)
            {
                try
                {
                    await Task.Run(() => channelFactory.Close());
                    logger.LogDebug("Channel factory closed (async)");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Exception occurred while closing channel factory asynchronously");
                    channelFactory.Abort();
                }
            }

            semaphore?.Dispose();
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Builds a complete URL from the specified WCF endpoint configuration.
        /// </summary>
        /// <param name="endpoint">The WCF endpoint configuration.</param>
        /// <returns>A complete URL string for the WCF service.</returns>
        private static string BuildUrl(WcfEndpoint endpoint)
        {
            var protocol = endpoint.IsHttps ? HttpsAddress : HttpAddress;
            return $"{protocol}://{endpoint.Host}:{endpoint.Port}/{endpoint.Path.TrimStart('/')}";
        }

        /// <summary>
        /// Configures SSL certificate validation based on the current configuration settings.
        /// </summary>
        private void ConfigureCertificateValidation()
        {
            if (configuration.IgnoreCertificateErrors)
            {
                ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
                logger.LogWarning("Certificate validation is disabled - this should only be used in development");
            }
            else if (configuration.TrustedCertificateThumbprints?.Length > 0)
            {
                ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            }
        }

        /// <summary>
        /// Configures a BasicHttpBinding for HTTP communication.
        /// </summary>
        /// <returns>A configured BasicHttpBinding instance.</returns>
        private BasicHttpBinding ConfigureHttpBinding()
        {
            var binding = new BasicHttpBinding
            {
                Security = { Mode = BasicHttpSecurityMode.TransportCredentialOnly },
                MaxBufferSize = configuration.MaxBufferSize,
                MaxReceivedMessageSize = configuration.MaxReceivedMessageSize,
                AllowCookies = configuration.AllowCookies,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
            };

            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            if (configuration.SendTimeout.HasValue)
            {
                binding.SendTimeout = configuration.SendTimeout.Value;
            }

            return binding;
        }

        /// <summary>
        /// Configures a BasicHttpsBinding for HTTPS communication.
        /// </summary>
        /// <returns>A configured BasicHttpsBinding instance.</returns>
        private BasicHttpsBinding ConfigureHttpsBinding()
        {
            var binding = new BasicHttpsBinding
            {
                Security = { Mode = BasicHttpsSecurityMode.Transport },
                MaxBufferSize = configuration.MaxBufferSize,
                MaxReceivedMessageSize = configuration.MaxReceivedMessageSize,
                AllowCookies = configuration.AllowCookies,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
            };

            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            if (configuration.SendTimeout.HasValue)
            {
                binding.SendTimeout = configuration.SendTimeout.Value;
            }

            return binding;
        }

        /// <summary>
        /// Creates the appropriate binding based on whether HTTPS is used.
        /// </summary>
        /// <param name="isHttps">True if HTTPS should be used; otherwise, false for HTTP.</param>
        /// <returns>A configured binding instance (either BasicHttpBinding or BasicHttpsBinding).</returns>
        private Binding CreateBinding(bool isHttps)
        {
            return isHttps
                ? (Binding)ConfigureHttpsBinding()
                : (Binding)ConfigureHttpBinding();
        }

        /// <summary>
        /// Creates and configures a ChannelFactory instance with the specified parameters.
        /// </summary>
        /// <param name="url">The service endpoint URL.</param>
        /// <param name="userName">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <param name="isHttps">True if HTTPS should be used; otherwise, false.</param>
        /// <returns>A configured ChannelFactory instance.</returns>
        private ChannelFactory<T> CreateChannelFactory(string url, string userName, string password, bool isHttps)
        {
            var binding = CreateBinding(isHttps);
            var address = new EndpointAddress(new Uri(url));

            Uri = address?.Uri;

            var factory = new ChannelFactory<T>(binding, address);

            factory.Credentials.UserName.UserName = userName;
            factory.Credentials.UserName.Password = password;

            return factory;
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the factory has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the factory has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Factory<T>));
            }
        }

        /// <summary>
        /// Validates server certificates based on trusted certificate thumbprints.
        /// </summary>
        /// <param name="sender">The object that initiated the certificate validation.</param>
        /// <param name="certificate">The certificate to validate.</param>
        /// <param name="chain">The certificate chain.</param>
        /// <param name="sslPolicyErrors">SSL policy errors encountered during validation.</param>
        /// <returns>True if the certificate is valid or trusted; otherwise, false.</returns>
        private bool ValidateServerCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If there are no SSL errors, the certificate is valid
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Check if the certificate is in the list of trusted thumbprints
            if (configuration.TrustedCertificateThumbprints?.Length > 0)
            {
                var thumbprint = certificate.GetCertHashString();
                foreach (var trustedThumbprint in configuration.TrustedCertificateThumbprints)
                {
                    if (string.Equals(thumbprint, trustedThumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("Certificate accepted based on trusted thumbprint: {Thumbprint}", thumbprint);
                        return true;
                    }
                }
            }

            logger.LogWarning("Certificate validation failed: {SslPolicyErrors}", sslPolicyErrors);
            return false;
        }

        #endregion Private Methods
    }
}