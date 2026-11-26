using System;
using System.IdentityModel.Tokens;
using System.Linq;
using CredentialChannelFactory.Security;
using CredentialChannelFactory.Tests.Helpers;
using Xunit;

namespace CredentialChannelFactory.Tests;

public class CertificateValidatorTests
{
    #region Public Methods

    [Fact]
    public void AcceptAll_WithAnyCertificate_ShouldNotThrow()
    {
        // Arrange
        using var certificate = CertificateHelper.CreateSelfSigned();
        var validator = new AcceptAllCertificateValidator();

        // Act & Assert
        validator.Validate(certificate);
    }

    [Fact]
    public void Thumbprint_WithFormattedThumbprint_ShouldNotThrow()
    {
        // Arrange
        using var certificate = CertificateHelper.CreateSelfSigned();

        var thumbprint = certificate.Thumbprint;

        string[] pairs = Enumerable.Range(0, thumbprint.Length / 2)
            .Select(i => thumbprint.Substring(i * 2, 2))
            .ToArray();

        var formatted = string.Join(":", pairs).ToLowerInvariant();
        var validator = new ThumbprintCertificateValidator([formatted]);

        // Act & Assert
        validator.Validate(certificate);
    }

    [Fact]
    public void Thumbprint_WithTrustedThumbprint_ShouldNotThrow()
    {
        // Arrange
        using var certificate = CertificateHelper.CreateSelfSigned();
        var validator = new ThumbprintCertificateValidator([certificate.Thumbprint]);

        // Act & Assert
        validator.Validate(certificate);
    }

    [Fact]
    public void Thumbprint_WithUnknownThumbprintAndSelfSignedCertificate_ShouldThrow()
    {
        // Arrange
        using var certificate = CertificateHelper.CreateSelfSigned();
        var validator = new ThumbprintCertificateValidator(["0000000000000000000000000000000000000000"]);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => validator.Validate(certificate));

        Assert.Throws<SecurityTokenValidationException>(() => validator.Validate(certificate));
    }

    #endregion Public Methods
}