using System.IdentityModel.Selectors;
using System.Security.Cryptography.X509Certificates;

namespace CredentialChannelFactory.Security;

internal class AcceptAllCertificateValidator
    : X509CertificateValidator
{
    #region Public Methods

    public override void Validate(X509Certificate2 certificate)
    {
        // Intentionally accepts every certificate (development only)
    }

    #endregion Public Methods
}