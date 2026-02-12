using System;

namespace CredentialChannelFactory.Configuration
{
    /// <summary>
    /// Represents the configuration settings for a WCF channel.
    /// </summary>
    public class WcfConfiguration
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WcfConfiguration"/> class with default values.
        /// </summary>
        public WcfConfiguration()
        {
            TrustedCertificateThumbprints = [];
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether cookies are allowed in the WCF channel.
        /// </summary>
        public bool AllowCookies { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether certificate errors should be ignored.
        /// </summary>
        public bool IgnoreCertificateErrors { get; set; }

        /// <summary>
        /// Gets or sets the maximum buffer size for the WCF channel.
        /// </summary>
        public int MaxBufferSize { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets the maximum received message size for the WCF channel.
        /// </summary>
        public int MaxReceivedMessageSize { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets the timeout for sending messages.
        /// </summary>
        public TimeSpan? SendTimeout { get; set; }

        /// <summary>
        /// Gets the list of trusted certificate thumbprints.
        /// </summary>
        public string[] TrustedCertificateThumbprints { get; }

        #endregion Public Properties
    }
}