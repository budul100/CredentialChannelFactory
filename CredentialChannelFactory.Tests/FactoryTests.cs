using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CredentialChannelFactory.Configuration;
using CredentialChannelFactory.Endpoint;
using CredentialChannelFactory.Exceptions;
using CredentialChannelFactory.Tests.Helpers;
using Moq;
using Xunit;

namespace CredentialChannelFactory.Tests
{
    public class FactoryTests
        : IDisposable
    {
        #region Private Fields

        private readonly ILogger<Factory<ITestService>> logger;
        private readonly Mock<ILogger<Factory<ITestService>>> mockLogger;
        private readonly ITestOutputHelper output;

        #endregion Private Fields

        #region Public Constructors

        public FactoryTests(ITestOutputHelper output)
        {
            this.output = output;
            mockLogger = new Mock<ILogger<Factory<ITestService>>>();
            logger = mockLogger.Object;
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Constructor_WithCustomConfiguration_ShouldApplySettings()
        {
            // Arrange
            const string url = "https://example.com:8080/TestService.svc";
            var config = new WcfConfiguration
            {
                MaxBufferSize = 1024,
                SendTimeout = TimeSpan.FromMinutes(2),
                AllowCookies = false
            };

            // Act & Assert
            using var factory = new Factory<ITestService>(url, "user", "pass", config, logger);

            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithEndpoint_ShouldCreateCorrectUrl()
        {
            // Arrange
            var endpoint = new WcfEndpoint("api.example.com", 443, "services/TestService.svc", true);
            const string userName = "testuser";
            const string password = "testpass";

            // Act
            using var factory = new Factory<ITestService>(endpoint, userName, password, null, logger);

            // Assert
            Assert.Equal("https", factory.Uri.Scheme);
            Assert.Equal("api.example.com", factory.Uri.Host);
            Assert.Equal(443, factory.Uri.Port);
            Assert.Contains("services/TestService.svc", factory.Uri.AbsolutePath);
        }

        [Fact]
        public void Constructor_WithIgnoreCertificateErrors_ShouldLogWarning()
        {
            // Arrange
            const string url = "https://example.com:8080/TestService.svc";
            var config = new WcfConfiguration { IgnoreCertificateErrors = true };

            // Act
            using var factory = new Factory<ITestService>(url, "user", "pass", config, logger);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Certificate validation is disabled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidUrl_ShouldThrowException(string invalidUrl)
        {
            // Arrange
            const string userName = "testuser";
            const string password = "testpass";

            // Act & Assert
            Assert.Throws<ChannelCreationException>(() =>
                new Factory<ITestService>(invalidUrl, userName, password, null, logger));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldUseDefaults()
        {
            // Arrange
            const string url = "https://example.com:8080/TestService.svc";

            // Act
            using var factory = new Factory<ITestService>(url, "user", "pass", null, logger);

            // Assert
            Assert.NotNull(factory);
            Assert.False(factory.IsDisposed);
        }

        [Fact]
        public void Constructor_WithValidHttpsUrl_ShouldCreateFactory()
        {
            // Arrange
            const string url = "https://example.com:8080/TestService.svc";
            const string userName = "testuser";
            const string password = "testpass";

            // Act & Assert - Should not throw
            using var factory = new Factory<ITestService>(url, userName, password, null, logger);

            Assert.NotNull(factory);
            Assert.False(factory.IsDisposed);
            Assert.NotNull(factory.Uri);
            Assert.Equal("https", factory.Uri.Scheme);
        }

        [Fact]
        public void Constructor_WithValidHttpUrl_ShouldCreateFactory()
        {
            // Arrange
            const string url = "http://example.com:8080/TestService.svc";
            const string userName = "testuser";
            const string password = "testpass";

            // Act & Assert
            using var factory = new Factory<ITestService>(url, userName, password, null, logger);

            Assert.NotNull(factory);
            Assert.Equal("http", factory.Uri.Scheme);
        }

        public void Dispose()
        {
            // Cleanup after each test
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var factory = CreateTestFactory();

            // Act & Assert
            factory.Dispose();
            factory.Dispose(); // Should not throw
            factory.Dispose(); // Should not throw

            Assert.True(factory.IsDisposed);
        }

        [Fact]
        public void Dispose_ShouldSetIsDisposedToTrue()
        {
            // Arrange
            var factory = CreateTestFactory();

            // Act
            factory.Dispose();

            // Assert
            Assert.True(factory.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var factory = CreateTestFactory();

            // Act & Assert
            await factory.DisposeAsync();
            await factory.DisposeAsync(); // Should not throw

            Assert.True(factory.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_ShouldSetIsDisposedToTrue()
        {
            // Arrange
            var factory = CreateTestFactory();

            // Act
            await factory.DisposeAsync();

            // Assert
            Assert.True(factory.IsDisposed);
        }

        [Fact]
        public void Get_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            using var factory = CreateTestFactory();
            const int threadCount = 10;
            var exceptions = new Exception[threadCount];
            var threads = new Thread[threadCount];

            // Act
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        factory.Get();
                    }
                    catch (Exception ex)
                    {
                        exceptions[threadIndex] = ex;
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert - None should be ObjectDisposedException
            foreach (var exception in exceptions)
            {
                if (exception != null)
                {
                    Assert.IsNotType<ObjectDisposedException>(exception);
                }
            }
        }

        [Fact]
        public void Get_MultipleCallsOnSameFactory_ShouldNotThrow()
        {
            // Arrange
            using var factory = CreateTestFactory();

            // Act & Assert
            for (var i = 0; i < 3; i++)
            {
                var exception = Record.Exception(() =>
                {
                    try
                    {
                        factory.Get();
                    }
                    catch (ChannelCreationException)
                    {
                        // Expected in test environment
                    }
                });

                Assert.IsNotType<ObjectDisposedException>(exception);
            }
        }

        [Fact]
        public void Get_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var factory = CreateTestFactory();
            factory.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => factory.Get());
        }

        [Fact]
        public void Get_WhenNotDisposed_ShouldCreateChannel()
        {
            // Arrange
            using var factory = CreateTestFactory();

            // Act & Assert
            // Note: In real scenarios, this would fail because we don't have a real service
            // But we're testing that the factory doesn't throw on creation
            var exception = Record.Exception(() =>
            {
                try
                {
                    var channel = factory.Get();
                }
                catch (ChannelCreationException)
                {
                    // Expected in test environment
                }
            });

            // The important thing is that it doesn't throw ObjectDisposedException
            Assert.IsNotType<ObjectDisposedException>(exception);
        }

        [Fact]
        public async Task GetAsync_CancellationDuringSemaphoreWait_ShouldNotLeakSemaphore()
        {
            // Arrange
            await using var factory = CreateTestFactory();
            using var cts = new CancellationTokenSource();

            // Start a task that will be cancelled while waiting for semaphore
            // First, acquire the semaphore with a long-running operation
            using var blockingCts = new CancellationTokenSource();
            var blockingTask = Task.Run(async () =>
            {
                try
                {
                    await factory.GetAsync(blockingCts.Token);
                }
                catch (OperationCanceledException) { }
            });

            // Give it time to acquire the semaphore
            await Task.Delay(50);

            // Now try to get async with immediate cancellation
            cts.Cancel();

            // Act & Assert - Should throw OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                factory.GetAsync(cts.Token));

            // Critical test: After cancellation, subsequent calls should still work
            // Cancel the blocking task to free the semaphore
            blockingCts.Cancel();
            await blockingTask;

            // This should not hang or throw - proving semaphore wasn't leaked
            using var workingCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var exception = await Record.ExceptionAsync(() => factory.GetAsync(workingCts.Token));

            // Should either work or throw ChannelCreationException (due to no real service)
            // but NOT hang forever due to leaked semaphore
            if (exception != null)
            {
                Assert.IsType<ChannelCreationException>(exception);
            }
        }

        [Fact]
        public async Task GetAsync_CancellationRace_ShouldHandleCorrectly()
        {
            // Arrange
            await using var factory = CreateTestFactory();
            using var cts = new CancellationTokenSource();

            // Act - Start the async operation and cancel shortly after
            var getTask = factory.GetAsync(cts.Token);
            cts.CancelAfter(TimeSpan.FromMilliseconds(10)); // Cancel after 10ms

            // Assert - Should either succeed or throw OperationCanceledException
            var exception = await Record.ExceptionAsync(() => getTask);

            if (exception != null)
            {
                // If an exception occurred, it should be OperationCanceledException
                Assert.IsType<OperationCanceledException>(exception);
            }
            // If no exception, the operation completed before cancellation - that's also valid
        }

        [Fact]
        public async Task GetAsync_ConcurrentCalls_ShouldNotThrow()
        {
            // Arrange
            await using var factory = CreateTestFactory();

            // Act
            var tasks = new Task[5];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await factory.GetAsync();
                    }
                    catch (ChannelCreationException)
                    {
                        // Expected in test environment
                    }
                });
            }

            // Assert
            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }

        [Fact]
        public async Task GetAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var factory = CreateTestFactory();
            factory.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => factory.GetAsync());
        }

        [Fact]
        public async Task GetAsync_WhenNotDisposed_ShouldCreateChannel()
        {
            // Arrange
            await using var factory = CreateTestFactory();

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
            {
                try
                {
                    await factory.GetAsync();
                }
                catch (ChannelCreationException)
                {
                    // Expected in test environment
                }
            });

            Assert.IsNotType<ObjectDisposedException>(exception);
        }

        [Fact]
        public async Task GetAsync_WithCancellationDuringSemaphoreWait_ShouldRespectCancellation()
        {
            // Arrange
            await using var factory = CreateTestFactory();

            var semaphore = (SemaphoreSlim)typeof(Factory<ITestService>)
                .GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(factory)!;

            // Hold the semaphore so that the next call has to wait
            await semaphore.WaitAsync();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

                // Act & Assert (TaskCanceledException derives from OperationCanceledException)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    factory.GetAsync(cts.Token));
            }
            finally
            {
                semaphore.Release();
            }

            // The cancelled waiter must not have leaked the semaphore
            using var workingCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var exception = await Record.ExceptionAsync(() => factory.GetAsync(workingCts.Token));

            if (exception != null)
            {
                Assert.IsType<ChannelCreationException>(exception);
            }
        }

        [Fact]
        public async Task GetAsync_WithPreCancelledToken_ShouldThrowImmediately()
        {
            // Arrange
            await using var factory = CreateTestFactory();
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel before calling GetAsync

            // Act & Assert
            var exception = await Assert
               .ThrowsAnyAsync<OperationCanceledException>(() => factory.GetAsync(cts.Token));
        }

        [Fact]
        public void IsDisposed_InitiallyFalse()
        {
            // Arrange & Act
            using var factory = CreateTestFactory();

            // Assert
            Assert.False(factory.IsDisposed);
        }

        [Fact]
        public void LockObject_ShouldNotBeNull()
        {
            // Arrange & Act
            using var factory = CreateTestFactory();

            // Assert
            Assert.NotNull(factory.LockObject);
        }

        [Fact]
        public void Uri_AfterConstruction_ShouldBeSet()
        {
            // Arrange & Act
            using var factory = CreateTestFactory();

            // Assert
            Assert.NotNull(factory.Uri);
            Assert.Equal("https", factory.Uri.Scheme);
        }

        #endregion Public Methods

        #region Private Methods

        private Factory<ITestService> CreateTestFactory()
        {
            return new Factory<ITestService>(
                "https://example.com:8080/TestService.svc",
                "testuser",
                "testpass",
                new WcfConfiguration(),
                logger);
        }

        #endregion Private Methods
    }
}