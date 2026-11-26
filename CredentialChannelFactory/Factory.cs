using System;
using System.IdentityModel.Selectors;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;
using System.Threading.Tasks;
using CredentialChannelFactory.Configuration;
using CredentialChannelFactory.Endpoint;
using CredentialChannelFactory.Exceptions;
using CredentialChannelFactory.Security;
using Microsoft.Extensions.Logging;

namespace CredentialChannelFactory;

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

    private const string HttpsAddress = "https";

    private readonly ChannelFactory<T> channelFactory;
    private readonly WcfConfiguration configuration;
    private readonly ILogger<Factory<T>> logger;

    // 0 = active, 1 = disposed (set atomically at the start of the disposal)
    private int disposeState;

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
            ArgumentException.ThrowIfNullOrWhiteSpace(url);

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
        : this(endpoint?.ToString(), userName, password, configuration, logger)
    { }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether the factory has been disposed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref disposeState) != 0;

    /// <summary>
    /// Gets an object that was formerly used for thread synchronization.
    /// </summary>
    [Obsolete("The factory no longer synchronizes on this object. It will be removed in the next major version.")]
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
        await DisposeAsyncCore().ConfigureAwait(false);
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

    /// <summary>
    /// Creates a new WCF channel instance. Channel creation does not perform any I/O,
    /// so the returned task is already completed. Errors are reported through the returned task.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token. If already cancelled, a cancelled task is returned.</param>
    /// <returns>A task that returns a new instance of the WCF service channel.</returns>
    /// <exception cref="ObjectDisposedException">Reported by the task when the factory has been disposed.</exception>
    /// <exception cref="ChannelCreationException">Reported by the task when the channel cannot be created.</exception>
    /// <exception cref="OperationCanceledException">Reported by the task when the token is cancelled.</exception>
    public Task<T> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Channel creation was cancelled");
            return Task.FromCanceled<T>(cancellationToken);
        }

        try
        {
            return Task.FromResult(Get());
        }
        catch (Exception ex)
        {
            return Task.FromException<T>(ex);
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
        // Claim the disposal atomically so that concurrent or repeated calls do nothing
        if (Interlocked.Exchange(ref disposeState, 1) != 0)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

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
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Claim the disposal atomically so that concurrent or repeated calls do nothing
        if (Interlocked.Exchange(ref disposeState, 1) != 0)
        {
            return;
        }

        if (channelFactory is null)
        {
            return;
        }

        try
        {
            await Task.Run(() => channelFactory.Close()).ConfigureAwait(false);
            logger.LogDebug("Channel factory closed (async)");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception occurred while closing channel factory asynchronously");
            channelFactory.Abort();
        }
    }

    #endregion Protected Methods

    #region Private Methods

    /// <summary>
    /// Configures the SSL certificate validation of the specified channel factory based on the current configuration settings.
    /// </summary>
    /// <param name="factory">The channel factory to configure.</param>
    /// <param name="isHttps">True if the service is accessed via HTTPS; certificate settings are ignored otherwise.</param>
    private void ConfigureCertificateValidation(ChannelFactory<T> factory, bool isHttps)
    {
        if (!isHttps)
        {
            return;
        }

        X509CertificateValidator validator;

        if (configuration.IgnoreCertificateErrors)
        {
            validator = new AcceptAllCertificateValidator();
            logger.LogWarning("Certificate validation is disabled - this should only be used in development");
        }
        else if (configuration.TrustedCertificateThumbprints?.Length > 0)
        {
            validator = new ThumbprintCertificateValidator(configuration.TrustedCertificateThumbprints);
        }
        else
        {
            return;
        }

        factory.Credentials.ServiceCertificate.SslCertificateAuthentication = new X509ServiceCertificateAuthentication
        {
            CertificateValidationMode = X509CertificateValidationMode.Custom,
            CustomCertificateValidator = validator
        };
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

        Uri = address.Uri;

        var factory = new ChannelFactory<T>(binding, address);

        try
        {
            factory.Credentials.UserName.UserName = userName;
            factory.Credentials.UserName.Password = password;

            ConfigureCertificateValidation(
                factory: factory,
                isHttps: isHttps);
        }
        catch
        {
            factory.Abort();
            throw;
        }

        return factory;
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the factory has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the factory has been disposed.</exception>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    #endregion Private Methods
}