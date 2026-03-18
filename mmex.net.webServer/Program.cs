using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using mmex.net.core.Data;
using mmex.net.core.Services;
using mmex.net.webServer;
using mmex.net.webServer.Components;

var builder = WebApplication.CreateBuilder(args);

// Ensure static web assets from NuGet packages (MudBlazor etc.) are served correctly.
// In development, CreateBuilder auto-calls UseStaticWebAssets() only when
// ASPNETCORE_ENVIRONMENT=Development AND the manifest is found. We call it
// explicitly to avoid 404s when running the project directly.
if (builder.Environment.IsDevelopment())
    builder.WebHost.UseStaticWebAssets();

// Database path: CLI arg > env var > default
var dbPath = args.FirstOrDefault(a => !a.StartsWith('-'))
    ?? Environment.GetEnvironmentVariable("MMEX_DB_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "data", "mmex.mmb");

if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"Database not found: {dbPath}");
    Console.Error.WriteLine("Usage: dotnet run [path-to-database.mmb]");
    Console.Error.WriteLine("   or: set MMEX_DB_PATH environment variable");
    return;
}

var attachmentFolder = Path.Combine(
    Path.GetDirectoryName(dbPath)!,
    "Attachments",
    $"MMEX_{Path.GetFileNameWithoutExtension(dbPath)}_Attachments");

builder.Services.AddSingleton(new AppSettings
{
    DatabasePath = dbPath,
    AttachmentFolder = attachmentFolder
});

builder.Services.AddDbContext<MmexDbContext>(o =>
    o.UseSqlite($"Data Source={dbPath}")
     .EnableDetailedErrors());

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IScheduledTransactionService, ScheduledTransactionService>();
builder.Services.AddScoped<IPayeeService, PayeeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Serve attachment files
app.MapGet("/api/attachments/{refType}/{fileName}", (string refType, string fileName, AppSettings settings) =>
{
    var filePath = Path.Combine(settings.AttachmentFolder, refType, fileName);
    if (!File.Exists(filePath))
        return Results.NotFound();
    var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
    return Results.File(filePath, contentType, fileName);
});

// Upload attachment
app.MapPost("/api/attachments/upload", async (HttpRequest request, IAttachmentService attachmentService, AppSettings settings) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    var refType = form["refType"].ToString();
    var refIdStr = form["refId"].ToString();
    var description = form["description"].ToString();

    if (file == null || string.IsNullOrEmpty(refType) || !long.TryParse(refIdStr, out var refId))
        return Results.BadRequest("Missing required fields");

    // Save to temp file first
    var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
    await using (var stream = File.Create(tempPath))
        await file.CopyToAsync(stream);

    try
    {
        var attachment = new mmex.net.core.Entities.Attachment
        {
            RefType = refType,
            RefId = refId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        };
        var result = await attachmentService.AddAsync(attachment, tempPath, settings.AttachmentFolder);
        return Results.Ok(new { result.Id, result.FileName, result.Description });
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
});

Console.WriteLine($"Database: {dbPath}");
Console.WriteLine($"Attachments: {attachmentFolder}");

app.Run();
