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

    public async Task<Dictionary<long, int>> GetCountsByRefAsync(string refType, IEnumerable<long> ids)
    {
        var idSet = ids.ToList();
        return await _db.Attachments
            .Where(a => a.RefType == refType && idSet.Contains(a.RefId))
            .GroupBy(a => a.RefId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task<string[]> GetAllDescriptionsAsync() =>
        await _db.Attachments
            .Where(a => a.Description != null && a.Description != "")
            .Select(a => a.Description!)
            .Distinct()
            .OrderBy(d => d)
            .ToArrayAsync();

    public async Task<Attachment> AddAsync(Attachment attachment, string sourcePath, string attachmentFolder)
    {
        var typeFolder = Path.Combine(attachmentFolder, attachment.RefType);
        Directory.CreateDirectory(typeFolder);

        var destName = MmexFileName(sourcePath, attachment.RefType, attachment.RefId, typeFolder);
        File.Copy(sourcePath, Path.Combine(typeFolder, destName));

        attachment.FileName = destName;
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAsync(long id, string attachmentFolder)
    {
        var att = await _db.Attachments.FindAsync(id);
        if (att == null) return;

        var filePath = Path.Combine(attachmentFolder, att.RefType, att.FileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        _db.Attachments.Remove(att);
        await _db.SaveChangesAsync();
    }

    private static string MmexFileName(string sourcePath, string refType, long refId, string folder)
    {
        var ext = Path.GetExtension(sourcePath);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var candidate = $"{refType}_{refId}_{baseName}{ext}";
        int i = 2;
        while (File.Exists(Path.Combine(folder, candidate)))
            candidate = $"{refType}_{refId}_{baseName}_{i++}{ext}";
        return candidate;
    }
}
