namespace mmex.net.importer;

/// <summary>PDF bank statement importer. Not yet implemented.</summary>
public class PdfImporter : IImporter
{
    public Task<IList<TransactionImportDto>> ImportAsync(Stream stream)
    {
        // TODO: implement with itext7 or PdfPig
        throw new NotImplementedException("PDF import is not yet implemented.");
    }
}
