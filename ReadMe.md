# CredentialChannelFactory

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/budul100/CredentialChannelFactory/blob/main/LICENSE) [![NuGet](https://img.shields.io/nuget/v/budul.CredentialChannelFactory.svg)](https://www.nuget.org/packages/budul.CredentialChannelFactory/)

Factory for creating WCF/SOAP web service channels with credentials.

## Features

- Credential handling for WCF services
- Thread-safe channel factory utilities
- Async/await support with proper cancellation handling
- Configurable SSL certificate validation

## Supported frameworks

net8.0, net9.0, net10.0

## Installation

```bash
dotnet add package budul.CredentialChannelFactory
```

Or via PackageReference:

```xml
<PackageReference Include="budul.CredentialChannelFactory" Version="3.0.0" />
```

## Quick Start

```csharp
using CredentialChannelFactory;
using System.ServiceModel;

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

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (builds all target frameworks).

```bash
git clone https://github.com/budul100/CredentialChannelFactory.git
cd CredentialChannelFactory
dotnet test
```

## License

This project is licensed under the MIT License.