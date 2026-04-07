namespace Gavel.Core.Infrastructure.Storage;

/// <summary>
/// Provides high-level storage operations for legal assets, 
/// ensuring persistence for both local development and cloud scenarios.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads content to storage and returns a unique URI or path for retrieval.
    /// </summary>
    Task<string> UploadAsync(string fileName, byte[] content, CancellationToken ct = default);
}
