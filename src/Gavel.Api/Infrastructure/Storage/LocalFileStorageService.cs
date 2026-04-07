using Gavel.Core.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Gavel.Api.Infrastructure.Storage;

public class StorageOptions
{
    public string BasePath { get; set; } = "storage";
}

/// <summary>
/// Simple local storage provider, optimized for Native AOT and high-performance file I/O.
/// Used to store legal assets (Sale Notes, Auction Minutes).
/// </summary>
public class LocalFileStorageService(IOptions<StorageOptions> options) : IStorageService
{
    private readonly string _basePath = options.Value.BasePath;

    public async Task<string> UploadAsync(string fileName, byte[] content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Filename cannot be empty.", nameof(fileName));

        // Basic security check for local implementation
        if (fileName.Contains("..") || Path.IsPathRooted(fileName))
            throw new ArgumentException("Invalid filename.", nameof(fileName));

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        var fullPath = Path.Combine(_basePath, fileName);
        
        // File.WriteAllBytesAsync is idempotent (overwrites if exists)
        await File.WriteAllBytesAsync(fullPath, content, ct);

        // For local development, we return the absolute file path as the URL.
        return new Uri(Path.GetFullPath(fullPath)).AbsoluteUri;
    }
}
