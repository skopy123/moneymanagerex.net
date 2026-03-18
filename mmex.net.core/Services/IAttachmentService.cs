using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IAttachmentService
{
    Task<IList<Attachment>> GetByRefAsync(string refType, long refId);
    Task<Dictionary<long, int>> GetCountsByRefAsync(string refType, IEnumerable<long> ids);
    Task<string[]> GetAllDescriptionsAsync();
    Task<Attachment> AddAsync(Attachment attachment, string sourcePath, string attachmentFolder);
    Task DeleteAsync(long id, string attachmentFolder);
}
