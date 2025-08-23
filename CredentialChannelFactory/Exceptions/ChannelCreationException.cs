using System;

namespace CredentialChannelFactory.Exceptions
{
    // Custom Exceptions
    public class ChannelCreationException : Exception
    {
        public ChannelCreationException(string message) : base(message) { }
        public ChannelCreationException(string message, Exception innerException) : base(message, innerException) { }
    }
}