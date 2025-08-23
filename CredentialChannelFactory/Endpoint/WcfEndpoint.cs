namespace CredentialChannelFactory.Endpoint
{
    /// <summary>
    /// Represents a WCF endpoint configuration with host, port, path, and protocol information.
    /// </summary>
    public class WcfEndpoint
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WcfEndpoint"/> class.
        /// </summary>
        public WcfEndpoint()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WcfEndpoint"/> class with the specified host, port, path, and protocol.
        /// </summary>
        /// <param name="host">The host name or IP address of the endpoint.</param>
        /// <param name="port">The port number of the endpoint.</param>
        /// <param name="path">The path segment of the endpoint URL.</param>
        /// <param name="isHttps">Indicates whether HTTPS is used. Default is true.</param>
        public WcfEndpoint(string host, int port, string path, bool isHttps = true)
        {
            Host = host;
            Port = port;
            Path = path;
            IsHttps = isHttps;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the host name or IP address of the endpoint.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether HTTPS is used.
        /// </summary>
        public bool IsHttps { get; set; } = true;

        /// <summary>
        /// Gets or sets the path segment of the endpoint URL.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the port number of the endpoint.
        /// </summary>
        public int Port { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="WcfEndpoint"/> instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is WcfEndpoint endpoint
                && Host == endpoint.Host
                && Port == endpoint.Port
                && Path == endpoint.Path
                && IsHttps == endpoint.IsHttps;
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="WcfEndpoint"/> instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + (Host?.GetHashCode() ?? 0);
                hash = hash * 23 + Port.GetHashCode();
                hash = hash * 23 + (Path?.GetHashCode() ?? 0);
                hash = hash * 23 + IsHttps.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current <see cref="WcfEndpoint"/> instance.
        /// </summary>
        /// <returns>A string representation of the endpoint URL.</returns>
        public override string ToString()
        {
            var protocol = IsHttps ? "https" : "http";
            return $"{protocol}://{Host}:{Port}/{Path?.TrimStart('/')}";
        }

        #endregion Public Methods
    }
}