using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public class AttachmentService : IAttachmentService
{
    private readonly MmexDbContext _db;

    public AttachmentService(MmexDbContext db) => _db = db;

    public async Task<IList<Attachment>> GetByRefAsync(string refType, long refId) =>
        await _db.Attachments
            .Where(a => a.RefType == refType && a.RefId == refId)
            .ToListAsync();

    public async Task<Attachment> AddAsync(Attachment attachment, string sourcePath, string attachmentFolder)
    {
        Directory.CreateDirectory(attachmentFolder);

        var destName = UniqueFileName(sourcePath, attachmentFolder);
        File.Copy(sourcePath, Path.Combine(attachmentFolder, destName));

        attachment.FileName = destName;
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAsync(long id, string attachmentFolder)
    {
        var att = await _db.Attachments.FindAsync(id);
        if (att == null) return;

        var filePath = Path.Combine(attachmentFolder, att.FileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        _db.Attachments.Remove(att);
        await _db.SaveChangesAsync();
    }

    private static string UniqueFileName(string sourcePath, string folder)
    {
        var ext = Path.GetExtension(sourcePath);
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        var candidate = Path.GetFileName(sourcePath);
        int i = 1;
        while (File.Exists(Path.Combine(folder, candidate)))
            candidate = $"{name}_{i++}{ext}";
        return candidate;
    }
}
