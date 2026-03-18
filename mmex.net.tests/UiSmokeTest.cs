using mmex.net.core.Entities;
using mmex.net.core.Services;
using mmex.net.winform.Panels;

namespace mmex.net.tests;

/// <summary>
/// Verifies that WinForms controls can be constructed without throwing.
/// These tests catch property-ordering bugs (e.g. AutoCompleteMode before AutoCompleteSource)
/// that only surface at runtime, not compile time.
/// WinForms controls require an STA thread; each test spins one up explicitly.
/// </summary>
public class UiSmokeTest
{
    /// <summary>
    /// Constructing TransactionListPanel must not throw.
    /// Previously crashed with NotSupportedException because AutoCompleteMode was
    /// set before AutoCompleteSource on a DropDownList-style ComboBox.
    /// </summary>
    [Fact]
    public void TransactionListPanel_Constructor_DoesNotThrow()
    {
        RunOnSta(() =>
        {
            using var panel = new TransactionListPanel(
                new StubTransactionService(),
                new StubAccountService());
        });
    }

    // ── STA helper ────────────────────────────────────────────────────────────

    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var t = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (caught != null)
            throw new Xunit.Sdk.XunitException($"Exception on STA thread: {caught}");
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubTransactionService : ITransactionService
    {
        public Task<IList<Transaction>> GetByAccountAsync(long accountId, DateOnly? from = null, DateOnly? to = null)
            => Task.FromResult<IList<Transaction>>([]);

        public Task<IList<(Transaction Transaction, decimal RunningBalance)>> GetRunningBalanceAsync(long accountId)
            => Task.FromResult<IList<(Transaction, decimal)>>([]);

        public Task<Transaction?> GetByIdAsync(long id)
            => Task.FromResult<Transaction?>(null);

        public Task<Transaction> CreateAsync(Transaction transaction, IList<SplitTransaction>? splits = null)
            => Task.FromResult(transaction);

        public Task<Transaction> UpdateAsync(Transaction transaction, IList<SplitTransaction>? splits = null)
            => Task.FromResult(transaction);

        public Task SoftDeleteAsync(long id) => Task.CompletedTask;
        public Task HardDeleteAsync(long id) => Task.CompletedTask;
    }

    private sealed class StubAccountService : IAccountService
    {
        public Task<IList<Account>> GetAllAsync()         => Task.FromResult<IList<Account>>([]);
        public Task<IList<Account>> GetAllOpenAsync()     => Task.FromResult<IList<Account>>([]);
        public Task<IList<Account>> GetFavoritesAsync()   => Task.FromResult<IList<Account>>([]);
        public Task<Account?> GetByIdAsync(long id)       => Task.FromResult<Account?>(null);
        public Task<Account> CreateAsync(Account account) => Task.FromResult(account);
        public Task<Account> UpdateAsync(Account account) => Task.FromResult(account);
        public Task DeleteAsync(long id)                  => Task.CompletedTask;
        public Task<decimal> GetBalanceAsync(long accountId)                       => Task.FromResult(0m);
        public Task<decimal> GetBalanceAtDateAsync(long accountId, DateOnly date)  => Task.FromResult(0m);
    }
}
