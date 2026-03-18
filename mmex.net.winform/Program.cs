using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mmex.net.core.Data;
using mmex.net.core.Services;
using mmex.net.winform;
using mmex.net.winform.Forms;
using System.Diagnostics;

namespace mmex.net.winform;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        string? dbPath = PickDatabase();
        if (dbPath == null) return;

        var startupSw = Stopwatch.StartNew();

        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(o =>
                {
                    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                    o.SingleLine = true;
                    o.IncludeScopes = false;
                });
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((_, services) =>
            {
                var attachmentFolder = Path.Combine(
                    Path.GetDirectoryName(dbPath)!,
                    Path.GetFileNameWithoutExtension(dbPath) + "_attachments");

                services.AddSingleton(new AppSettings
                {
                    DatabasePath = dbPath,
                    AttachmentFolder = attachmentFolder
                });

                services.AddDbContext<MmexDbContext>(o =>
                    o.UseSqlite($"Data Source={dbPath}")
                     .EnableDetailedErrors());
                services.AddScoped<IAccountService, AccountService>();
                services.AddScoped<ITransactionService, TransactionService>();
                services.AddScoped<IScheduledTransactionService, ScheduledTransactionService>();
                services.AddScoped<IPayeeService, PayeeService>();
                services.AddScoped<ICategoryService, CategoryService>();
                services.AddScoped<ICurrencyService, CurrencyService>();
                services.AddScoped<IAttachmentService, AttachmentService>();
                services.AddTransient<MainForm>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup");
        logger.LogInformation("Host built in {Elapsed}ms", startupSw.ElapsedMilliseconds);

        var mainForm = host.Services.GetRequiredService<MainForm>();
        logger.LogInformation("MainForm resolved in {Elapsed}ms", startupSw.ElapsedMilliseconds);

        Application.Run(mainForm);
    }

    private static string? PickDatabase()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open MoneyManager database",
            Filter = "MMEX Database (*.mmb)|*.mmb|SQLite Database (*.db)|*.db|All files (*.*)|*.*",
            CheckFileExists = false
        };

        if (dlg.ShowDialog() == DialogResult.OK)
            return dlg.FileName;

        var create = MessageBox.Show(
            "No database selected. Create a new one?",
            "MoneyManager .NET",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (create != DialogResult.Yes) return null;

        using var save = new SaveFileDialog
        {
            Title = "Create new MoneyManager database",
            Filter = "MMEX Database (*.mmb)|*.mmb",
            DefaultExt = "mmb"
        };
        return save.ShowDialog() == DialogResult.OK ? save.FileName : null;
    }
}
