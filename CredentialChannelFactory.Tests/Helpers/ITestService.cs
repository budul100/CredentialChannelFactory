using System.ServiceModel;
using System.ServiceModel.Channels;

namespace CredentialChannelFactory.Tests.Helpers
{
    // Test Service Contract für die Tests
    [ServiceContract]
    public interface ITestService
        : IChannel
    {
        #region Public Methods

        [OperationContract]
        string GetData(string value);

        [OperationContract]
        Task<string> GetDataAsync(string value);

        #endregion Public Methods
    }
}