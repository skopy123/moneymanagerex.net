using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using mmex.net.core.Entities;
using mmex.net.core.Enums;

namespace mmex.net.core.Data;

public class MmexDbContext : DbContext
{
    public MmexDbContext(DbContextOptions<MmexDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ScheduledTransaction> ScheduledTransactions => Set<ScheduledTransaction>();
    public DbSet<SplitTransaction> SplitTransactions => Set<SplitTransaction>();
    public DbSet<BudgetSplitTransaction> BudgetSplitTransactions => Set<BudgetSplitTransaction>();
    public DbSet<Payee> Payees => Set<Payee>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CurrencyHistory> CurrencyHistory => Set<CurrencyHistory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TagLink> TagLinks => Set<TagLink>();
    public DbSet<TransactionLink> TransactionLinks => Set<TransactionLink>();
    public DbSet<ShareInfo> ShareInfos => Set<ShareInfo>();
    public DbSet<InfoTable> InfoTable => Set<InfoTable>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<CustomField> CustomFields => Set<CustomField>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Value converters ---
        var accountTypeConverter = new ValueConverter<AccountType, string>(
            v => AccountTypeToString(v),
            v => StringToAccountType(v));

        var accountStatusConverter = new ValueConverter<AccountStatus, string>(
            v => v == AccountStatus.Open ? "Open" : "Closed",
            v => v == "Open" ? AccountStatus.Open : AccountStatus.Closed);

        var transactionTypeConverter = new ValueConverter<TransactionType, string>(
            v => TransactionTypeToString(v),
            v => StringToTransactionType(v));

        var transactionStatusConverter = new ValueConverter<TransactionStatus, string>(
            v => TransactionStatusToString(v),
            v => StringToTransactionStatus(v));

        var currencyTypeConverter = new ValueConverter<CurrencyType, string>(
            v => v == CurrencyType.Fiat ? "Fiat" : "Crypto",
            v => v == "Crypto" ? CurrencyType.Crypto : CurrencyType.Fiat);

        // --- ACCOUNTLIST_V1 ---
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("ACCOUNTLIST_V1");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("ACCOUNTID");
            e.Property(a => a.Name).HasColumnName("ACCOUNTNAME").IsRequired();
            e.Property(a => a.Type).HasColumnName("ACCOUNTTYPE").IsRequired()
                .HasConversion(accountTypeConverter);
            e.Property(a => a.AccountNum).HasColumnName("ACCOUNTNUM");
            e.Property(a => a.Status).HasColumnName("STATUS").IsRequired()
                .HasConversion(accountStatusConverter);
            e.Property(a => a.Notes).HasColumnName("NOTES");
            e.Property(a => a.HeldAt).HasColumnName("HELDAT");
            e.Property(a => a.Website).HasColumnName("WEBSITE");
            e.Property(a => a.ContactInfo).HasColumnName("CONTACTINFO");
            e.Property(a => a.AccessInfo).HasColumnName("ACCESSINFO");
            e.Property(a => a.InitialBalance).HasColumnName("INITIALBAL");
            e.Property(a => a.InitialDate).HasColumnName("INITIALDATE");
            e.Property(a => a.IsFavorite).HasColumnName("FAVORITEACCT")
                .HasConversion(
                    v => v ? "TRUE" : "FALSE",
                    v => v == "TRUE" || v == "true" || v == "1");
            e.Property(a => a.CurrencyId).HasColumnName("CURRENCYID").IsRequired();
            e.Property(a => a.StatementLocked).HasColumnName("STATEMENTLOCKED");
            e.Property(a => a.StatementDate).HasColumnName("STATEMENTDATE");
            e.Property(a => a.MinimumBalance).HasColumnName("MINIMUMBALANCE");
            e.Property(a => a.CreditLimit).HasColumnName("CREDITLIMIT");
            e.Property(a => a.InterestRate).HasColumnName("INTERESTRATE");
            e.Property(a => a.PaymentDueDate).HasColumnName("PAYMENTDUEDATE");
            e.Property(a => a.MinimumPayment).HasColumnName("MINIMUMPAYMENT");

            e.HasOne(a => a.Currency).WithMany()
                .HasForeignKey(a => a.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(a => a.Transactions).WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId);
            e.HasMany(a => a.ToTransactions).WithOne(t => t.ToAccount)
                .HasForeignKey(t => t.ToAccountId);
        });

        // --- CHECKINGACCOUNT_V1 ---
        modelBuilder.Entity<Transaction>(e =>
        {
            e.ToTable("CHECKINGACCOUNT_V1");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("TRANSID");
            e.Property(t => t.AccountId).HasColumnName("ACCOUNTID").IsRequired();
            e.Property(t => t.ToAccountId).HasColumnName("TOACCOUNTID");
            e.Property(t => t.PayeeId).HasColumnName("PAYEEID").IsRequired();
            e.Property(t => t.Type).HasColumnName("TRANSCODE").IsRequired()
                .HasConversion(transactionTypeConverter);
            e.Property(t => t.Amount).HasColumnName("TRANSAMOUNT").IsRequired();
            e.Property(t => t.Status).HasColumnName("STATUS")
                .HasConversion(transactionStatusConverter);
            e.Property(t => t.Number).HasColumnName("TRANSACTIONNUMBER");
            e.Property(t => t.Notes).HasColumnName("NOTES");
            e.Property(t => t.CategoryId).HasColumnName("CATEGID");
            e.Property(t => t.Date).HasColumnName("TRANSDATE");
            e.Property(t => t.LastUpdatedTime).HasColumnName("LASTUPDATEDTIME");
            e.Property(t => t.DeletedTime).HasColumnName("DELETEDTIME");
            e.Property(t => t.FollowUpId).HasColumnName("FOLLOWUPID");
            e.Property(t => t.ToAmount).HasColumnName("TOTRANSAMOUNT");
            e.Property(t => t.Color).HasColumnName("COLOR").HasDefaultValue(-1);

            // Soft delete global query filter
            e.HasQueryFilter(t => t.DeletedTime == null);

            e.HasOne(t => t.Account).WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ToAccount).WithMany(a => a.ToTransactions)
                .HasForeignKey(t => t.ToAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Payee).WithMany()
                .HasForeignKey(t => t.PayeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Category).WithMany()
                .HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(t => t.Splits).WithOne(s => s.Transaction)
                .HasForeignKey(s => s.TransactionId);
            e.HasMany(t => t.TagLinks).WithOne()
                .HasForeignKey(tl => tl.RefId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- BILLSDEPOSITS_V1 ---
        modelBuilder.Entity<ScheduledTransaction>(e =>
        {
            e.ToTable("BILLSDEPOSITS_V1");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("BDID");
            e.Property(s => s.AccountId).HasColumnName("ACCOUNTID").IsRequired();
            e.Property(s => s.ToAccountId).HasColumnName("TOACCOUNTID");
            e.Property(s => s.PayeeId).HasColumnName("PAYEEID").IsRequired();
            e.Property(s => s.Type).HasColumnName("TRANSCODE").IsRequired()
                .HasConversion(transactionTypeConverter);
            e.Property(s => s.Amount).HasColumnName("TRANSAMOUNT").IsRequired();
            e.Property(s => s.Status).HasColumnName("STATUS")
                .HasConversion(transactionStatusConverter);
            e.Property(s => s.Number).HasColumnName("TRANSACTIONNUMBER");
            e.Property(s => s.Notes).HasColumnName("NOTES");
            e.Property(s => s.CategoryId).HasColumnName("CATEGID");
            e.Property(s => s.Date).HasColumnName("TRANSDATE");
            e.Property(s => s.FollowUpId).HasColumnName("FOLLOWUPID");
            e.Property(s => s.ToAmount).HasColumnName("TOTRANSAMOUNT");
            e.Property(s => s.Color).HasColumnName("COLOR").HasDefaultValue(-1);
            e.Property(s => s.Repeats).HasColumnName("REPEATS");
            e.Property(s => s.NextOccurrenceDate).HasColumnName("NEXTOCCURRENCEDATE");
            e.Property(s => s.NumOccurrences).HasColumnName("NUMOCCURRENCES");

            e.HasOne(s => s.Account).WithMany()
                .HasForeignKey(s => s.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ToAccount).WithMany()
                .HasForeignKey(s => s.ToAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Payee).WithMany()
                .HasForeignKey(s => s.PayeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Category).WithMany()
                .HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(s => s.Splits).WithOne(bs => bs.ScheduledTransaction)
                .HasForeignKey(bs => bs.TransactionId);
        });

        // --- SPLITTRANSACTIONS_V1 ---
        modelBuilder.Entity<SplitTransaction>(e =>
        {
            e.ToTable("SPLITTRANSACTIONS_V1");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("SPLITTRANSID");
            e.Property(s => s.TransactionId).HasColumnName("TRANSID").IsRequired();
            e.Property(s => s.CategoryId).HasColumnName("CATEGID");
            e.Property(s => s.Amount).HasColumnName("SPLITTRANSAMOUNT");
            e.Property(s => s.Notes).HasColumnName("NOTES");
        });

        // --- BUDGETSPLITTRANSACTIONS_V1 ---
        modelBuilder.Entity<BudgetSplitTransaction>(e =>
        {
            e.ToTable("BUDGETSPLITTRANSACTIONS_V1");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("SPLITTRANSID");
            e.Property(s => s.TransactionId).HasColumnName("TRANSID").IsRequired();
            e.Property(s => s.CategoryId).HasColumnName("CATEGID");
            e.Property(s => s.Amount).HasColumnName("SPLITTRANSAMOUNT");
            e.Property(s => s.Notes).HasColumnName("NOTES");
        });

        // --- PAYEE_V1 ---
        modelBuilder.Entity<Payee>(e =>
        {
            e.ToTable("PAYEE_V1");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("PAYEEID");
            e.Property(p => p.Name).HasColumnName("PAYEENAME").IsRequired();
            e.Property(p => p.CategoryId).HasColumnName("CATEGID");
            e.Property(p => p.Number).HasColumnName("NUMBER");
            e.Property(p => p.Website).HasColumnName("WEBSITE");
            e.Property(p => p.Notes).HasColumnName("NOTES");
            e.Property(p => p.Active).HasColumnName("ACTIVE");
            e.Property(p => p.Pattern).HasColumnName("PATTERN").HasDefaultValue(string.Empty);
        });

        // --- CATEGORY_V1 ---
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("CATEGORY_V1");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("CATEGID");
            e.Property(c => c.Name).HasColumnName("CATEGNAME").IsRequired();
            e.Property(c => c.Active).HasColumnName("ACTIVE");
            e.Property(c => c.ParentId).HasColumnName("PARENTID");

            e.HasOne(c => c.Parent).WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- CURRENCYFORMATS_V1 ---
        modelBuilder.Entity<Currency>(e =>
        {
            e.ToTable("CURRENCYFORMATS_V1");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("CURRENCYID");
            e.Property(c => c.Name).HasColumnName("CURRENCYNAME").IsRequired();
            e.Property(c => c.PrefixSymbol).HasColumnName("PFX_SYMBOL");
            e.Property(c => c.SuffixSymbol).HasColumnName("SFX_SYMBOL");
            e.Property(c => c.DecimalPoint).HasColumnName("DECIMAL_POINT");
            e.Property(c => c.GroupSeparator).HasColumnName("GROUP_SEPARATOR");
            e.Property(c => c.UnitName).HasColumnName("UNIT_NAME");
            e.Property(c => c.CentName).HasColumnName("CENT_NAME");
            e.Property(c => c.Scale).HasColumnName("SCALE");
            e.Property(c => c.BaseConvRate).HasColumnName("BASECONVRATE");
            e.Property(c => c.Symbol).HasColumnName("CURRENCY_SYMBOL").IsRequired();
            e.Property(c => c.CurrencyType).HasColumnName("CURRENCY_TYPE").IsRequired()
                .HasConversion(currencyTypeConverter);
        });

        // --- CURRENCYHISTORY_V1 ---
        modelBuilder.Entity<CurrencyHistory>(e =>
        {
            e.ToTable("CURRENCYHISTORY_V1");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("CURRHISTID");
            e.Property(h => h.CurrencyId).HasColumnName("CURRENCYID").IsRequired();
            e.Property(h => h.Date).HasColumnName("CURRDATE").IsRequired();
            e.Property(h => h.Value).HasColumnName("CURRVALUE").IsRequired();
            e.Property(h => h.UpdateType).HasColumnName("CURRUPDTYPE");

            e.HasOne(h => h.Currency).WithMany(c => c.History)
                .HasForeignKey(h => h.CurrencyId);
        });

        // --- TAG_V1 ---
        modelBuilder.Entity<Tag>(e =>
        {
            e.ToTable("TAG_V1");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("TAGID");
            e.Property(t => t.Name).HasColumnName("TAGNAME").IsRequired();
            e.Property(t => t.Active).HasColumnName("ACTIVE");
        });

        // --- TAGLINK_V1 ---
        modelBuilder.Entity<TagLink>(e =>
        {
            e.ToTable("TAGLINK_V1");
            e.HasKey(tl => tl.Id);
            e.Property(tl => tl.Id).HasColumnName("TAGLINKID");
            e.Property(tl => tl.RefType).HasColumnName("REFTYPE").IsRequired();
            e.Property(tl => tl.RefId).HasColumnName("REFID").IsRequired();
            e.Property(tl => tl.TagId).HasColumnName("TAGID").IsRequired();

            e.HasOne(tl => tl.Tag).WithMany(t => t.TagLinks)
                .HasForeignKey(tl => tl.TagId);
        });

        // --- TRANSLINK_V1 ---
        modelBuilder.Entity<TransactionLink>(e =>
        {
            e.ToTable("TRANSLINK_V1");
            e.HasKey(tl => tl.Id);
            e.Property(tl => tl.Id).HasColumnName("TRANSLINKID");
            e.Property(tl => tl.CheckingAccountId).HasColumnName("CHECKINGACCOUNTID").IsRequired();
            e.Property(tl => tl.LinkType).HasColumnName("LINKTYPE").IsRequired();
            e.Property(tl => tl.LinkRecordId).HasColumnName("LINKRECORDID").IsRequired();

            e.HasOne(tl => tl.Transaction).WithMany()
                .HasForeignKey(tl => tl.CheckingAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- SHAREINFO_V1 ---
        modelBuilder.Entity<ShareInfo>(e =>
        {
            e.ToTable("SHAREINFO_V1");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("SHAREINFOID");
            e.Property(s => s.CheckingAccountId).HasColumnName("CHECKINGACCOUNTID").IsRequired();
            e.Property(s => s.ShareNumber).HasColumnName("SHARENUMBER");
            e.Property(s => s.SharePrice).HasColumnName("SHAREPRICE");
            e.Property(s => s.ShareCommission).HasColumnName("SHARECOMMISSION");
            e.Property(s => s.ShareLot).HasColumnName("SHARELOT");

            e.HasOne(s => s.Transaction).WithMany()
                .HasForeignKey(s => s.CheckingAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- INFOTABLE_V1 ---
        modelBuilder.Entity<InfoTable>(e =>
        {
            e.ToTable("INFOTABLE_V1");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("INFOID");
            e.Property(i => i.Name).HasColumnName("INFONAME").IsRequired();
            e.Property(i => i.Value).HasColumnName("INFOVALUE").IsRequired();
        });

        // --- SETTING_V1 ---
        modelBuilder.Entity<Setting>(e =>
        {
            e.ToTable("SETTING_V1");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("SETTINGID");
            e.Property(s => s.Name).HasColumnName("SETTINGNAME").IsRequired();
            e.Property(s => s.Value).HasColumnName("SETTINGVALUE");
        });

        // --- ATTACHMENT_V1 ---
        modelBuilder.Entity<Attachment>(e =>
        {
            e.ToTable("ATTACHMENT_V1");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("ATTACHMENTID");
            e.Property(a => a.RefType).HasColumnName("REFTYPE").IsRequired();
            e.Property(a => a.RefId).HasColumnName("REFID").IsRequired();
            e.Property(a => a.Description).HasColumnName("DESCRIPTION");
            e.Property(a => a.FileName).HasColumnName("FILENAME").IsRequired();
        });

        // --- CUSTOMFIELD_V1 ---
        modelBuilder.Entity<CustomField>(e =>
        {
            e.ToTable("CUSTOMFIELD_V1");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("FIELDID");
            e.Property(c => c.RefType).HasColumnName("REFTYPE").IsRequired();
            e.Property(c => c.Description).HasColumnName("DESCRIPTION");
            e.Property(c => c.Type).HasColumnName("TYPE").IsRequired();
            e.Property(c => c.Properties).HasColumnName("PROPERTIES").IsRequired();
        });
    }

    private static string AccountTypeToString(AccountType t) => t switch
    {
        AccountType.Cash => "Cash",
        AccountType.Checking => "Checking",
        AccountType.Term => "Term",
        AccountType.Investment => "Investment",
        AccountType.CreditCard => "Credit Card",
        AccountType.Loan => "Loan",
        AccountType.Asset => "Asset",
        AccountType.Shares => "Shares",
        _ => "Checking"
    };

    private static AccountType StringToAccountType(string s) => s switch
    {
        "Cash" => AccountType.Cash,
        "Term" => AccountType.Term,
        "Investment" => AccountType.Investment,
        "Credit Card" => AccountType.CreditCard,
        "Loan" => AccountType.Loan,
        "Asset" => AccountType.Asset,
        "Shares" => AccountType.Shares,
        _ => AccountType.Checking
    };

    private static string TransactionTypeToString(TransactionType t)
    {
        if (t == TransactionType.Deposit) return "Deposit";
        if (t == TransactionType.Transfer) return "Transfer";
        return "Withdrawal";
    }

    private static TransactionType StringToTransactionType(string s)
    {
        if (s == "Deposit") return TransactionType.Deposit;
        if (s == "Transfer") return TransactionType.Transfer;
        return TransactionType.Withdrawal;
    }

    private static string TransactionStatusToString(TransactionStatus s)
    {
        if (s == TransactionStatus.Reconciled) return "R";
        if (s == TransactionStatus.Void) return "V";
        if (s == TransactionStatus.FollowUp) return "F";
        if (s == TransactionStatus.Duplicate) return "D";
        return "";
    }

    private static TransactionStatus StringToTransactionStatus(string s)
    {
        if (s == "R") return TransactionStatus.Reconciled;
        if (s == "V") return TransactionStatus.Void;
        if (s == "F") return TransactionStatus.FollowUp;
        if (s == "D") return TransactionStatus.Duplicate;
        return TransactionStatus.None;
    }
}
