namespace BankingAssistant.Interfaces;

/// <summary>
/// Interface for document scanning operations using Azure Document Intelligence.
/// </summary>
public interface IDocumentScanner
{
    /// <summary>
    /// Scans a document and extracts key-value pairs.
    /// </summary>
    /// <param name="fileName">The name of the document file to scan.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of extracted fields.</returns>
    public Task<Dictionary<string, string>> Scan(string fileName);
}

