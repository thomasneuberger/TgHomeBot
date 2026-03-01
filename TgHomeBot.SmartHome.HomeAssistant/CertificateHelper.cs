using System.Security.Cryptography.X509Certificates;

namespace TgHomeBot.SmartHome.HomeAssistant;

internal static class CertificateHelper
{
    internal static X509Certificate2 LoadCertificate(string path)
    {
        const int maxScanLines = 50;
        var isPem = false;

        using (var reader = new StreamReader(path))
        {
            string? line;
            var linesRead = 0;

            while (linesRead < maxScanLines && (line = reader.ReadLine()) is not null)
            {
                linesRead++;

                if (line.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
                {
                    isPem = true;
                    break;
                }
            }
        }

        return isPem
            ? X509Certificate2.CreateFromPemFile(path)
            : X509CertificateLoader.LoadCertificateFromFile(path);
    }
}
