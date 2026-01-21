
namespace BankingAssistant.Proxy;

/// <summary>
/// Proxy class for Azure Document Intelligence interactions.
/// Provides document scanning capabilities using Azure's prebuilt invoice model to extract structured data from invoice documents.
/// </summary>
public class DocumentIntelligenceProxy : IDocumentScanner
{
    /// <summary>
    /// The blob storage proxy used to retrieve document files.
    /// </summary>
    private readonly IBlobStorage _blobStorageProxy;

    /// <summary>
    /// The Azure Document Intelligence client used to analyze documents.
    /// </summary>
    private readonly DocumentIntelligenceClient _documentIntelligenceClient;

    /// <summary>
    /// Logger for tracking document scanning operations.
    /// </summary>
    private readonly ILogger<DocumentIntelligenceProxy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentIntelligenceProxy"/> class.
    /// </summary>
    /// <param name="blobStorageProxy">The blob storage proxy for retrieving document files.</param>
    /// <param name="documentIntelligenceClient">The Azure Document Intelligence client for analyzing documents.</param>
    /// <param name="logger">The logger for tracking operations.</param>
    public DocumentIntelligenceProxy(IBlobStorage blobStorageProxy, DocumentIntelligenceClient documentIntelligenceClient, ILogger<DocumentIntelligenceProxy> logger)
    {
        _blobStorageProxy = blobStorageProxy;
        _documentIntelligenceClient = documentIntelligenceClient;
        _logger = logger;
    }

    /// <summary>
    /// Scans an invoice document using Azure Document Intelligence's prebuilt invoice model.
    /// Extracts key fields such as vendor information, customer details, invoice ID, date, and total.
    /// </summary>
    /// <param name="fileName">The name of the file to scan in blob storage.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of extracted fields and their values.</returns>
    public async Task<Dictionary<string, string>> ScanAsync(string fileName)
    {
        var image = await _blobStorageProxy.GetFileAsBytesAsync(fileName);
        _logger.LogInformation($"Scanning: {fileName} {image.Length}");
        
        var modelId = "prebuilt-invoice";
        Operation<AnalyzeResult> operation = _documentIntelligenceClient.AnalyzeDocument(WaitUntil.Completed, modelId, BinaryData.FromBytes(image));
        AnalyzeResult result = operation.Value;

        Dictionary<string, string> scanData = new Dictionary<string, string>();

        for (var i = 0; i < result.Documents.Count; i++)
        {
            AnalyzedDocument analyzedInvoice = result.Documents[i];

            if (analyzedInvoice.Fields.TryGetValue("VendorName", out DocumentField vendorNameField)
                && vendorNameField.FieldType == DocumentFieldType.String)
            {
                scanData.Add("VendorName", vendorNameField.ValueString);
            }

            if (analyzedInvoice.Fields.TryGetValue("VendorAddress", out DocumentField vendorAddressField)
                && vendorAddressField.FieldType == DocumentFieldType.Address)
            {
                scanData.Add("VendorAddress", vendorAddressField.ValueString);
            }

            if (analyzedInvoice.Fields.TryGetValue("CustomerName", out DocumentField customerNameField)
                && customerNameField.FieldType == DocumentFieldType.String)
            {
                scanData.Add("CustomerName", customerNameField.ValueString);
            }

            if (analyzedInvoice.Fields.TryGetValue("CustomerAddressRecipient", out DocumentField customerAddressRecipientField)
                && customerAddressRecipientField.FieldType == DocumentFieldType.String)
            {
                scanData.Add("CustomerAddressRecipient", customerAddressRecipientField.ValueString);
            }

            if (analyzedInvoice.Fields.TryGetValue("InvoiceId", out DocumentField invoiceIdField)
                && invoiceIdField.FieldType == DocumentFieldType.String)
            {
                scanData.Add("InvoiceId", invoiceIdField.ValueString);
            }

            if (analyzedInvoice.Fields.TryGetValue("InvoiceDate", out DocumentField invoiceDateField)
                && invoiceDateField.FieldType == DocumentFieldType.String)
            {
                scanData.Add("InvoiceDate", invoiceDateField.ValueString);
            }

            if (analyzedInvoice.Fields.TryGetValue("InvoiceTotal", out DocumentField invoiceTotalField)
                && invoiceTotalField.FieldType == DocumentFieldType.String)
            {
                scanData.Add("InvoiceTotal", invoiceTotalField.ValueString);
            }
        }
        
        return scanData;
    }
}
