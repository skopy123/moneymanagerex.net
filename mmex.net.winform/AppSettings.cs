namespace mmex.net.winform;

/// <summary>Singleton holding runtime paths derived from the chosen database file.</summary>
public sealed class AppSettings
{
    public required string DatabasePath { get; init; }

    /// <summary>
    /// Folder where attachment files are stored.
    /// Defaults to &lt;dbname&gt;_attachments/ next to the database file.
    /// </summary>
    public required string AttachmentFolder { get; init; }
}
