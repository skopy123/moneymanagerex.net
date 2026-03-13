using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using Xunit;
using Xunit.Abstractions;

namespace mmex.net.tests;

public class DbContextTest : IDisposable
{
    // -----------------------------------------------------------------------
    // Point this at your .mmb file before running
    // -----------------------------------------------------------------------
    private const string DbPath = @"C:\path\to\your\database.mmb";
    // -----------------------------------------------------------------------

    private readonly MmexDbContext _db;
    private readonly ITestOutputHelper _out;

    public DbContextTest(ITestOutputHelper output)
    {
        _out = output;
        var options = new DbContextOptionsBuilder<MmexDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;
        _db = new MmexDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    // --- Accounts -----------------------------------------------------------

    [Fact]
    public async Task ReadAccounts_DoesNotThrow()
    {
        var accounts = await _db.Accounts.ToListAsync();
        _out.WriteLine($"Accounts: {accounts.Count}");
        foreach (var a in accounts)
            _out.WriteLine($"  [{a.Id}] {a.Name} | Type={a.Type} | Status={a.Status} | CurrencyId={a.CurrencyId} | InitialBal={a.InitialBalance} | StatementLocked={a.StatementLocked}");
        Assert.NotEmpty(accounts);
    }

    [Fact]
    public async Task ReadAccounts_WithCurrency_DoesNotThrow()
    {
        var accounts = await _db.Accounts.Include(a => a.Currency).ToListAsync();
        _out.WriteLine($"Accounts with currency: {accounts.Count}");
        foreach (var a in accounts)
            _out.WriteLine($"  [{a.Id}] {a.Name} | Currency={a.Currency?.Symbol} | Scale={a.Currency?.Scale}");
        Assert.NotEmpty(accounts);
    }

    // --- Currencies ---------------------------------------------------------

    [Fact]
    public async Task ReadCurrencies_DoesNotThrow()
    {
        var currencies = await _db.Currencies.ToListAsync();
        _out.WriteLine($"Currencies: {currencies.Count}");
        foreach (var c in currencies)
            _out.WriteLine($"  [{c.Id}] {c.Symbol} | Scale={c.Scale} | Rate={c.BaseConvRate} | Type={c.CurrencyType}");
        Assert.NotEmpty(currencies);
    }

    // --- Transactions -------------------------------------------------------

    [Fact]
    public async Task ReadTransactions_DoesNotThrow()
    {
        // IgnoreQueryFilters to also read soft-deleted rows
        var txns = await _db.Transactions.IgnoreQueryFilters().Take(100).ToListAsync();
        _out.WriteLine($"Transactions (first 100): {txns.Count}");
        foreach (var t in txns)
            _out.WriteLine($"  [{t.Id}] {t.Date} | {t.Type} | Amount={t.Amount} | Status={t.Status} | Color={t.Color} | PayeeId={t.PayeeId}");
        Assert.True(txns.Count >= 0);
    }

    [Fact]
    public async Task ReadTransactions_WithNavigation_DoesNotThrow()
    {
        var txns = await _db.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.Payee)
            .Include(t => t.Category)
            .Take(50)
            .ToListAsync();
        _out.WriteLine($"Transactions with nav: {txns.Count}");
        foreach (var t in txns)
            _out.WriteLine($"  [{t.Id}] Payee={t.Payee?.Name} | Category={t.Category?.Name}");
        Assert.True(txns.Count >= 0);
    }

    // --- Scheduled Transactions ---------------------------------------------

    [Fact]
    public async Task ReadScheduledTransactions_DoesNotThrow()
    {
        var scheds = await _db.ScheduledTransactions.ToListAsync();
        _out.WriteLine($"Scheduled transactions: {scheds.Count}");
        foreach (var s in scheds)
            _out.WriteLine($"  [{s.Id}] {s.NextOccurrenceDate} | {s.Type} | Amount={s.Amount} | Color={s.Color} | Repeats={s.Repeats}");
        Assert.True(scheds.Count >= 0);
    }

    // --- Split Transactions -------------------------------------------------

    [Fact]
    public async Task ReadSplitTransactions_DoesNotThrow()
    {
        var splits = await _db.SplitTransactions.Take(50).ToListAsync();
        _out.WriteLine($"Splits: {splits.Count}");
        foreach (var s in splits)
            _out.WriteLine($"  [{s.Id}] TransId={s.TransactionId} | CategoryId={s.CategoryId} | Amount={s.Amount}");
        Assert.True(splits.Count >= 0);
    }

    // --- Budget Split Transactions ------------------------------------------

    [Fact]
    public async Task ReadBudgetSplitTransactions_DoesNotThrow()
    {
        var splits = await _db.BudgetSplitTransactions.Take(50).ToListAsync();
        _out.WriteLine($"Budget splits: {splits.Count}");
        foreach (var s in splits)
            _out.WriteLine($"  [{s.Id}] TransId={s.TransactionId} | CategoryId={s.CategoryId} | Amount={s.Amount}");
        Assert.True(splits.Count >= 0);
    }

    // --- Payees -------------------------------------------------------------

    [Fact]
    public async Task ReadPayees_DoesNotThrow()
    {
        var payees = await _db.Payees.ToListAsync();
        _out.WriteLine($"Payees: {payees.Count}");
        foreach (var p in payees)
            _out.WriteLine($"  [{p.Id}] {p.Name} | Active={p.Active} | CategoryId={p.CategoryId}");
        Assert.True(payees.Count >= 0);
    }

    // --- Categories ---------------------------------------------------------

    [Fact]
    public async Task ReadCategories_DoesNotThrow()
    {
        var cats = await _db.Categories.ToListAsync();
        _out.WriteLine($"Categories: {cats.Count}");
        foreach (var c in cats)
            _out.WriteLine($"  [{c.Id}] {c.Name} | ParentId={c.ParentId} | Active={c.Active}");
        Assert.NotEmpty(cats);
    }

    // --- Currency History ---------------------------------------------------

    [Fact]
    public async Task ReadCurrencyHistory_DoesNotThrow()
    {
        var history = await _db.CurrencyHistory.Take(50).ToListAsync();
        _out.WriteLine($"Currency history (first 50): {history.Count}");
        foreach (var h in history)
            _out.WriteLine($"  [{h.Id}] CurrencyId={h.CurrencyId} | Date={h.Date} | Value={h.Value}");
        Assert.True(history.Count >= 0);
    }

    // --- Tags ---------------------------------------------------------------

    [Fact]
    public async Task ReadTags_DoesNotThrow()
    {
        var tags = await _db.Tags.ToListAsync();
        _out.WriteLine($"Tags: {tags.Count}");
        foreach (var t in tags)
            _out.WriteLine($"  [{t.Id}] {t.Name} | Active={t.Active}");
        Assert.True(tags.Count >= 0);
    }

    // --- Tag Links ----------------------------------------------------------

    [Fact]
    public async Task ReadTagLinks_DoesNotThrow()
    {
        var links = await _db.TagLinks.Take(50).ToListAsync();
        _out.WriteLine($"Tag links: {links.Count}");
        foreach (var l in links)
            _out.WriteLine($"  [{l.Id}] RefType={l.RefType} | RefId={l.RefId} | TagId={l.TagId}");
        Assert.True(links.Count >= 0);
    }

    // --- Transaction Links --------------------------------------------------

    [Fact]
    public async Task ReadTransactionLinks_DoesNotThrow()
    {
        var links = await _db.TransactionLinks.Take(50).ToListAsync();
        _out.WriteLine($"Transaction links: {links.Count}");
        foreach (var l in links)
            _out.WriteLine($"  [{l.Id}] CheckingId={l.CheckingAccountId} | LinkType={l.LinkType} | RecordId={l.LinkRecordId}");
        Assert.True(links.Count >= 0);
    }

    // --- Share Info ---------------------------------------------------------

    [Fact]
    public async Task ReadShareInfos_DoesNotThrow()
    {
        var shares = await _db.ShareInfos.Take(50).ToListAsync();
        _out.WriteLine($"Share infos: {shares.Count}");
        foreach (var s in shares)
            _out.WriteLine($"  [{s.Id}] CheckingId={s.CheckingAccountId} | Shares={s.ShareNumber} | Price={s.SharePrice}");
        Assert.True(shares.Count >= 0);
    }

    // --- Info Table ---------------------------------------------------------

    [Fact]
    public async Task ReadInfoTable_DoesNotThrow()
    {
        var rows = await _db.InfoTable.ToListAsync();
        _out.WriteLine($"InfoTable rows: {rows.Count}");
        foreach (var r in rows)
            _out.WriteLine($"  [{r.Id}] {r.Name} = {r.Value}");
        Assert.NotEmpty(rows);
    }

    // --- Settings -----------------------------------------------------------

    [Fact]
    public async Task ReadSettings_DoesNotThrow()
    {
        var settings = await _db.Settings.ToListAsync();
        _out.WriteLine($"Settings: {settings.Count}");
        foreach (var s in settings)
            _out.WriteLine($"  [{s.Id}] {s.Name} = {s.Value}");
        Assert.True(settings.Count >= 0);
    }

    // --- Attachments --------------------------------------------------------

    [Fact]
    public async Task ReadAttachments_DoesNotThrow()
    {
        var attachments = await _db.Attachments.ToListAsync();
        _out.WriteLine($"Attachments: {attachments.Count}");
        foreach (var a in attachments)
            _out.WriteLine($"  [{a.Id}] RefType={a.RefType} | RefId={a.RefId} | File={a.FileName}");
        Assert.True(attachments.Count >= 0);
    }

    // --- Custom Fields ------------------------------------------------------

    [Fact]
    public async Task ReadCustomFields_DoesNotThrow()
    {
        var fields = await _db.CustomFields.ToListAsync();
        _out.WriteLine($"Custom fields: {fields.Count}");
        foreach (var f in fields)
            _out.WriteLine($"  [{f.Id}] RefType={f.RefType} | Type={f.Type} | Desc={f.Description}");
        Assert.True(fields.Count >= 0);
    }
}
