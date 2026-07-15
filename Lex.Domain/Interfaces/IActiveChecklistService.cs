using Lex.Domain.DTOs;

namespace Lex.Domain.Interfaces;

public interface IActiveChecklistService
{
    Task<ActiveChecklistDto?> GetActiveForUserAsync(Guid? userId, Guid checklistId, CancellationToken ct = default);
    Task<ActiveChecklistDto> StartNewAsync(Guid userId, Guid checklistId, Guid? clientOrganizationId = null,
        CancellationToken ct = default);
    Task<ActiveChecklistDto> RestoreAsync(Guid activeChecklistId, CancellationToken ct = default);
    Task<ActiveChecklistDetailsDto?> GetDetailsAsync(Guid activeChecklistId, CancellationToken ct = default);
    Task ToggleItemAsync(Guid activeChecklistItemId, bool isCompleted, CancellationToken ct = default);
    Task UpdateNoteAsync(Guid activeChecklistItemId, string? note, CancellationToken ct = default);
    Task DeleteAsync(Guid activeChecklistId, CancellationToken ct = default);
}