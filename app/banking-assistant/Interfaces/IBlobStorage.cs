namespace BankingAssistant.Interfaces;

/// <summary>
/// Interface for Azure Blob Storage operations.
/// </summary>
public interface IBlobStorage
{
    /// <summary>
    /// Retrieves a file from blob storage as a byte array.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file content as a byte array.</returns>
    public Task<byte[]> GetFileAsBytesAsync(string fileName);
    
    /// <summary>
    /// Stores a file in blob storage.
    /// </summary>
    /// <param name="fileName">The name of the file to store.</param>
    /// <param name="content">The stream containing the file content.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StoreFileAsync(string fileName, Stream content);

    /// <summary>
    /// Gets the URI for a file in blob storage.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The URI of the file.</returns>
    public Uri GetFileUri(string fileName);
}
