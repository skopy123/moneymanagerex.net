namespace mmex.net.importer;

/// <summary>Excel/XLSX bank statement importer. Not yet implemented.</summary>
public class XlsxImporter : IImporter
{
    public Task<IList<TransactionImportDto>> ImportAsync(Stream stream)
    {
        // TODO: implement with ClosedXML
        throw new NotImplementedException("XLSX import is not yet implemented.");
    }
}
