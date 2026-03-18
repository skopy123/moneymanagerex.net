using mmex.net.core.Entities;

namespace mmex.net.webServer;

public class TransactionDialogResult
{
    public required Transaction Transaction { get; init; }
    public IList<SplitTransaction>? Splits { get; init; }
}
