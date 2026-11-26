using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace CredentialChannelFactory.Security;

internal class ThumbprintCertificateValidator(string[] trustedThumbprints)
    : X509CertificateValidator
{
    #region Public Methods

    public override void Validate(X509Certificate2 certificate)
    {
        var thumbprint = Normalize(certificate.Thumbprint);

        // Accept explicitly trusted thumbprints
        if (Array.Exists(
            array: trustedThumbprints,
            match: t => string.Equals(
                a: Normalize(t),
                b: thumbprint,
                comparisonType: StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        // Otherwise fall back to the regular chain validation
        using var chain = new X509Chain();

        if (!chain.Build(certificate))
        {
            throw new SecurityTokenValidationException(
                $"Certificate validation failed for thumbprint {thumbprint}.");
        }
    }

    #endregion Public Methods

    #region Private Methods

    private static string Normalize(string thumbprint)
    {
        return thumbprint?
            .Replace(" ", string.Empty)
            .Replace(":", string.Empty);
    }

    #endregion Private Methods
}