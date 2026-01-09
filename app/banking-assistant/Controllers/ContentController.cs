namespace BankingAssistant.Controllers;

/// <summary>
/// Controller for handling content-related requests, such as fetching images from blob storage.
/// </summary>
[Route("api/content")]
[ApiController]
public class ContentController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IBlobStorage _blobStorage;
    private readonly ILogger<ContentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentController"/> class.
    /// </summary>
    /// <param name="environment">The web host environment.</param>
    /// <param name="blobStorage">The blob storage service.</param>
    /// <param name="logger">The logger instance.</param>
    public ContentController(IWebHostEnvironment environment, IBlobStorage blobStorage, ILogger<ContentController> logger)
    {
        _environment = environment;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a file from blob storage.
    /// </summary>
    /// <param name="fileName">The name of the file to retrieve.</param>
    /// <returns>The file content with appropriate content type.</returns>
    /// <response code="200">Returns the file content.</response>
    /// <response code="400">If no file name is provided.</response>
    /// <response code="500">If an error occurs while fetching the file.</response>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Index(string fileName)
    {
        if (fileName == null || fileName.Length == 0)
        {
            _logger.LogWarning("No file name provided.");
            return BadRequest("No file name provided.");
        }
        try
        {
            byte[] imageBytes = await _blobStorage.GetFileAsBytesAsync(fileName);

            // Determine the content type based on the file extension
            string contentType = GetContentType(fileName);

            return File(imageBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file from blob storage.");
            return StatusCode(500, "Error fetching file from blob storage.");
        }
    }

    /// <summary>
    /// Uploads a file to blob storage.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <returns>The uploaded file name.</returns>
    /// <response code="200">Returns the uploaded file name.</response>
    /// <response code="400">If the file is missing or empty.</response>
    /// <response code="500">If an error occurs while uploading the file.</response>
    [HttpPost]
    public async Task<IActionResult> UploadContent([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File is missing.");
            return BadRequest("File is missing.");
        }
        var fileName = Path.GetFileName(file.FileName);

        try
        {
            using var blobStream = file.OpenReadStream();
            await _blobStorage.StoreFileAsync(fileName, blobStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to blob storage.");
            return StatusCode(500, "Error uploading file to blob storage.");
        }

        return Ok(fileName);
    }

    /// <summary>
    /// Determines the MIME content type based on file extension.
    /// </summary>
    /// <param name="fileName">The file name with extension.</param>
    /// <returns>The MIME content type string.</returns>
    private string GetContentType(string fileName)
    {
        // You can expand this method to handle more file types
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

}
