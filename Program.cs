using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;

if (args.Length == 0 || !Uri.TryCreate(args[0], UriKind.Absolute, out var vaultUri))
{
    throw new Exception("Missing required vault URI parameter");
}

if (args.Length < 2 || !uint.TryParse(args[1], out var count))
{
    count = 50;
}

const string exportableCertName = "issue27356-exportable";
const string nonExportableCertName = "issue27356-nonexportable";

DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions()
{
    ExcludeManagedIdentityCredential = true,
    ExcludeInteractiveBrowserCredential = Console.IsOutputRedirected,
});
CertificateClient client = new(vaultUri, credential);

Task Create(string name, bool exportable)
{
    CertificatePolicy policy = new(WellKnownIssuerNames.Self, $"CN={name}")
    {
        KeyType = CertificateKeyType.Rsa,
        KeySize = 4096,
        Exportable = exportable,
    };

    Console.WriteLine($"Creating certificate {name}...");

    return client.StartCreateCertificateAsync(name, policy)
                 .ContinueWith(t => t.Result.WaitForCompletionAsync().AsTask());
}

await Task.WhenAll(new[]
{
    Create(exportableCertName, true),
    Create(nonExportableCertName, false),
});

Console.WriteLine("Certificates created");
Console.WriteLine("Downloading certificates...");

async Task Download(string name)
{
    try
    {
        X509Certificate2 cert = await client.DownloadCertificateAsync(name);
        Console.WriteLine($"{name} has private key: {cert.HasPrivateKey}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\x1b[31mFailed to download {name}:\x1b[0m {ex.Message}");
    }
}

List<Task> tasks = new((int)(count * 2));
for (var i = 0; i < count; i++)
{
    tasks.Add(Download(exportableCertName));
    tasks.Add(Download(nonExportableCertName));
}

await Task.WhenAll(tasks);
