namespace mmex.net.importer;

public interface IImporter
{
    Task<IList<TransactionImportDto>> ImportAsync(Stream stream);
}
