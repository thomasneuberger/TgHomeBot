using System.Security.Cryptography.X509Certificates;

namespace TgHomeBot.SmartHome.HomeAssistant;

internal static class CertificateHelper
{
    internal static X509Certificate2 LoadCertificate(string path)
    {
        return X509CertificateLoader.LoadCertificateFromFile(path);
    }
}
