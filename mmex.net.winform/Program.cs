using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mmex.net.core.Data;
using mmex.net.core.Services;
using mmex.net.winform.Forms;

namespace mmex.net.winform;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        string? dbPath = PickDatabase();
        if (dbPath == null) return;

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddDbContext<MmexDbContext>(o =>
                    o.UseSqlite($"Data Source={dbPath}"));
                services.AddScoped<IAccountService, AccountService>();
                services.AddScoped<ITransactionService, TransactionService>();
                services.AddScoped<IScheduledTransactionService, ScheduledTransactionService>();
                services.AddScoped<IPayeeService, PayeeService>();
                services.AddScoped<ICategoryService, CategoryService>();
                services.AddScoped<ICurrencyService, CurrencyService>();
                services.AddTransient<MainForm>();
            })
            .Build();

        var mainForm = host.Services.GetRequiredService<MainForm>();
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
