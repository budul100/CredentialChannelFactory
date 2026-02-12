using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace CredentialChannelFactory.Tests.Helpers
{
    // Test Service Contract for the tests
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