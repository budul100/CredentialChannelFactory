# CredentialChannelFactory

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE) [![NuGet](https://img.shields.io/nuget/v/budul.CredentialChannelFactory.svg)](https://www.nuget.org/packages/budul.CredentialChannelFactory/)

A .NET library for secure credential management and WCF channel creation, supporting both .NET 8 and .NET Standard 2.1.

## Features

- Secure credential handling with WCF services
- Thread-safe channel factory utilities
- Async/await support with proper cancellation handling
- Configurable SSL certificate validation
- Unit tests included for robust validation
- Cross-platform support via .NET Standard 2.1

## Installation

### Via NuGet Package Manager

```bash
Install-Package CredentialChannelFactory
```

### Via .NET CLI

```bash
dotnet add package CredentialChannelFactory
```

### Via PackageReference

Add this to your `.csproj` file:

```xml
<PackageReference Include="CredentialChannelFactory" Version="1.0.0" />
```

## Quick Start

```csharp
using CredentialChannelFactory;

// Create factory
using var factory = new Factory<IMyService>(
    url: "https://api.example.com/MyService.svc",
    userName: "your-username",
    password: "your-password");

// Create channel and make service call
var service = factory.Get();
var result = service.GetData("test");

// Clean up
if (service is IClientChannel channel)
    channel.Close();
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or compatible runtime
- Optionally, any environment supporting .NET Standard 2.1

### Build from Source

Clone the repository:

```bash
git clone https://github.com/yourusername/CredentialChannelFactory.git 
cd CredentialChannelFactory
```

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

### Running Tests

To run the included unit tests:

```bash
dotnet test
```

### Usage in Your Project

After installing the NuGet package, import the relevant namespaces:

```csharp
using CredentialChannelFactory;
using System.ServiceModel;
```

Then use the provided factories and helpers as needed in your WCF client applications.

## Configuration

```csharp
var config = new WcfConfiguration
{
    SendTimeout = TimeSpan.FromMinutes(5),
    MaxBufferSize = 1024 * 1024,
    IgnoreCertificateErrors = false // Never true in production!
};

var endpoint = new WcfEndpoint("api.example.com", 443, "service.svc");
using var factory = new Factory<IMyService>(endpoint, "user", "pass", config);
```

## Contributing

Contributions are welcome! Please open issues or submit pull requests for improvements and bug fixes.

## License

This project is licensed under the MIT License.