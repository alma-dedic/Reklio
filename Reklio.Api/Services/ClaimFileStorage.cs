using Reklio.Api.Services.Interfaces;

namespace Reklio.Api.Services;

// Lokalno skladište dokaza. Putanja iz konfiguracije (Storage:ClaimUploadsPath),
// default App_Data/claim-uploads. Dozvoljeni PNG/JPEG do 8 MB.
public class ClaimFileStorage : IFileStorageService
{
    private static readonly string[] AllowedTypes = ["image/png", "image/jpeg"];
    private const long MaxBytes = 8 * 1024 * 1024;

    private readonly string _root;

    public ClaimFileStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["Storage:ClaimUploadsPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "claim-uploads")
            : configured;
    }

    public async Task<string> SaveClaimFileAsync(
        int claimId, IFormFile file, string kind, int index, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Priloženi fajl je prazan.");
        }
        if (file.Length > MaxBytes)
        {
            throw new InvalidOperationException("Fajl je veći od dozvoljenih 8 MB.");
        }
        if (!AllowedTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Dozvoljene su samo PNG i JPEG slike.");
        }

        var extension = file.ContentType == "image/png" ? ".png" : ".jpg";
        var directory = Path.Combine(_root, claimId.ToString());
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{kind}-{index}{extension}");
        await using var stream = File.Create(path);
        await file.CopyToAsync(stream, cancellationToken);
        return path;
    }
}