using Lex.Domain.Entities;

namespace Lex.Domain.Interfaces;

public interface IActiveChecklistService
{
    Task<ActiveChecklist> CreateFromTemplateAsync(Guid templateId, Guid userId, CancellationToken cancellationToken = default);
    Task<ActiveChecklist?> GetActiveWithItemsAsync(Guid activeChecklistId, CancellationToken cancellationToken = default);
}