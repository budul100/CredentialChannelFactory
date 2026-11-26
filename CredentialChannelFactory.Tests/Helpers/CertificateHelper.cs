using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CredentialChannelFactory.Tests.Helpers;

public static class CertificateHelper
{
    #region Public Methods

    public static X509Certificate2 CreateSelfSigned()
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            subjectName: "CN=test.example.com",
            key: rsa,
            hashAlgorithm: HashAlgorithmName.SHA256,
            padding: RSASignaturePadding.Pkcs1);

        using var certificate = request.CreateSelfSigned(
            notBefore: DateTimeOffset.UtcNow.AddDays(-1),
            notAfter: DateTimeOffset.UtcNow.AddDays(30));

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
#else
        return new X509Certificate2(certificate.Export(X509ContentType.Cert));
#endif
    }

    #endregion Public Methods
}