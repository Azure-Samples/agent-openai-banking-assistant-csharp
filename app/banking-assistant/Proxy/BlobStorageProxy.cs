using BankingAssistant.Exceptions;

namespace BankingAssistant.Proxy;

/// <summary>
/// Proxy class for Azure Blob Storage interactions.
/// Provides methods to upload, download, and retrieve URIs for files stored in Azure Blob Storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlobStorageProxy"/> class.
/// </remarks>
/// <param name="blobServiceClient">The Azure Blob Service client for interacting with blob storage.</param>
/// <param name="logger">The logger for tracking operations and errors.</param>
/// <param name="configuration">The application configuration containing the storage container name.</param>
/// <exception cref="ArgumentNullException">Thrown when the storage container configuration is missing.</exception>
/// <exception cref="ItemNotFoundException">Thrown when a requested blob is not found.</exception>
public class BlobStorageProxy(BlobServiceClient blobServiceClient, ILogger<BlobStorageProxy> logger, IConfiguration configuration) : IBlobStorage
{
    /// <summary>
    /// The Azure Blob Service client used to interact with blob storage.
    /// </summary>
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

    /// <summary>
    /// The name of the container where files are stored.
    /// </summary>
    private readonly string _containerName = configuration[key: "Storage:ContainerName"] ?? throw new ArgumentNullException(nameof(configuration), "Storage container configuration is missing.");

    /// <summary>
    /// Logger for tracking blob storage operations and errors.
    /// </summary>
    private ILogger<BlobStorageProxy> _logger = logger;

    /// <summary>
    /// Downloads a file from blob storage and returns its content as a byte array.
    /// </summary>
    /// <param name="fileName">The name of the file to download.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file content as a byte array.</returns>
    /// <exception cref="ItemNotFoundException">Thrown when the blob file is not found.</exception>
    public async Task<byte[]> GetFileAsBytesAsync(string fileName)
    {
        try
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();

            return downloadResult.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob file '{FileName}' not found in container '{ContainerName}'.", fileName, _containerName);
            throw new ItemNotFoundException($"Blob file '{fileName}' not found.", ex);
        }
    }

    /// <summary>
    /// Gets the URI of a file stored in blob storage.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The URI of the specified file.</returns>
    public Uri GetFileUri(string fileName)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient blobClient = containerClient.GetBlobClient(fileName);

        return blobClient.Uri;
    }

    /// <summary>
    /// Uploads a file to blob storage.
    /// Creates the container if it doesn't exist.
    /// </summary>
    /// <param name="fileName">The name to give the file in blob storage.</param>
    /// <param name="content">The file content as a stream.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    /// <exception cref="ConflictException">Thrown when a conflict occurs during upload (e.g., blob already exists with incompatible conditions).</exception>
    /// <exception cref="BadRequestException">Thrown when an error occurs during file upload.</exception>
    public async Task StoreFileAsync(string fileName, Stream content)
    {
        try
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Create the container if it doesn't exist.
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(content, overwrite: true);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning("Conflict uploading file '{FileName}' to blob storage.", fileName);
            throw new ConflictException($"Conflict uploading file '{fileName}'.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file '{FileName}' to blob storage.", fileName);
            throw new BadRequestException($"Failed to upload file '{fileName}'.", ex);
        }
    }
}
