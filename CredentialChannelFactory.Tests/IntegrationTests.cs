using System;
using System.ServiceModel;
using System.Threading.Tasks;
using CredentialChannelFactory.Configuration;
using CredentialChannelFactory.Tests.Helpers;
using Xunit;

namespace CredentialChannelFactory.Tests;

/// <summary>
/// Integration tests that require a real WCF service to be running.
/// These are marked with a custom trait so they can be excluded from regular test runs.
/// </summary>
[Trait("Category", "Integration")]
public class IntegrationTests
{
    // These tests would require a real WCF service
    // You can skip them in CI/CD by filtering out "Category=Integration"
    #region Public Methods

    [Fact(Skip = "Requires real WCF service")]
    public async Task GetAsync_WithRealService_ShouldReturnWorkingChannel()
    {
        // This would test against a real service
        // Arrange
        var config = new WcfConfiguration
        {
            SendTimeout = TimeSpan.FromSeconds(30)
        };

        using var factory = new Factory<ITestService>(
            "https://your-test-service.com/Service.svc",
            "testuser",
            "testpass",
            config);

        // Act
        var channel = await factory.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(channel);

        // Test actual service call
        var result = await channel.GetDataAsync("test");
        Assert.NotNull(result);

        // Cleanup
        if (channel is IClientChannel clientChannel)
        {
            clientChannel.Close();
        }
    }

    #endregion Public Methods
}
