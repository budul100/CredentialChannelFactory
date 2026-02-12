using System;

namespace CredentialChannelFactory.Exceptions
{
    /// <summary>
    /// Represents errors that occur during channel creation.
    /// </summary>
    public class ChannelCreationException : Exception
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelCreationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ChannelCreationException(string message)
            : base(message: message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelCreationException"/> class
        /// with a specified error message and a reference to the inner exception that is
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ChannelCreationException(string message, Exception innerException)
            : base(message: message, innerException: innerException)
        { }

        #endregion Public Constructors
    }
}