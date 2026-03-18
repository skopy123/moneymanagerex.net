using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IAttachmentService
{
    Task<IList<Attachment>> GetByRefAsync(string refType, long refId);
    Task<Attachment> AddAsync(Attachment attachment, string sourcePath, string attachmentFolder);
    Task DeleteAsync(long id, string attachmentFolder);
}
