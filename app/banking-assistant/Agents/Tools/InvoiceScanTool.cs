using System.ComponentModel;
using System.Diagnostics;

namespace BankingAssistant.Agents.Tools;

/// <summary>
/// Tool for scanning invoices and extracting structured data using Azure Document Intelligence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvoiceScanTool"/> class.
/// </remarks>
/// <param name="documentScanner">The document scanner service.</param>
/// <param name="logger">The logger instance.</param>
public class InvoiceScanTool(IDocumentScanner documentScanner, ILogger<InvoiceScanTool> logger)
{
    private readonly ILogger<InvoiceScanTool> _logger = logger;
    private IDocumentScanner _documentScanner = documentScanner;
    private static readonly ActivitySource ToolActivitySource = new ActivitySource("BankingAssistant.Tools.InvoiceScanTool");

	/// <summary>
	/// Scans an invoice image and extracts structured data.
	/// </summary>
	/// <param name="filePath">The path to the file containing the invoice image or photo.</param>
	/// <returns>A JSON string containing the extracted invoice data.</returns>
	[Description("Extract the invoice or bill data scanning a photo or image")]
    public async Task<string> ScanInvoiceAsync([Description("the path to the file containing the image or photo")] string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath, nameof(filePath));

        // Create OpenTelemetry activity for tool invocation
        using var activity = ToolActivitySource.StartActivity("ScanInvoiceAsync");
        activity?.SetTag("tool.name", "ScanInvoiceAsync");
        activity?.SetTag("tool.input.filePath", filePath);

        Dictionary<string, string>? scanData;
        _logger.LogInformation("Attempting to scan: {FilePath}", filePath);

        try
        {
            scanData = await _documentScanner.ScanAsync(filePath);
            activity?.SetTag("tool.output.status", "success");
            activity?.SetTag("tool.output.itemCount", scanData?.Count ?? 0);
        }
        catch (Exception ex)
        {
            activity?.SetTag("tool.output.status", "error");
            activity?.SetTag("tool.output.exception", ex.GetType().Name);
            activity?.SetTag("tool.output.message", ex.Message);
            _logger.LogError("Error extracting data from invoice {FilePath}: {Exception}", filePath, ex);
            scanData = [];
        }

        _logger.LogInformation("SK scanInvoice plugin: Data extracted {FilePath}:{ScanData}", filePath, scanData);
        return JsonSerializer.Serialize(scanData);
    }
}

