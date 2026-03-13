namespace mmex.net.importer;

public class TransactionImportDto
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Payee { get; set; }
    public string? Notes { get; set; }
    public string? RawText { get; set; }
}
