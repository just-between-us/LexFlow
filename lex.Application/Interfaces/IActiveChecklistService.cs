using lex.Application.DTOs;

namespace lex.Application.Interfaces;

public interface IActiveChecklistService
{
    Task<ActiveChecklistDto?> GetActiveForUserAsync(Guid userId, Guid checklistId, CancellationToken ct = default);
    Task<ActiveChecklistDto> StartNewAsync(Guid userId, Guid checklistId, Guid? clientOrganizationId = null, CancellationToken ct = default);
    Task<ActiveChecklistDto> RestoreAsync(Guid activeChecklistId, CancellationToken ct = default);

    Task<ActiveChecklistDetailsDto?> GetDetailsAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default);
    Task ToggleItemAsync(Guid activeChecklistItemId, Guid requestingUserId, bool isCompleted, CancellationToken ct = default);
    Task UpdateNoteAsync(Guid activeChecklistItemId, Guid requestingUserId, string? note, CancellationToken ct = default);
    Task DeleteAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default);

    Task<ActiveChecklistDetailsDto> AddEditorAsync(Guid activeChecklistId, Guid requestingUserId, string emailOrUsername, CancellationToken ct = default);
    Task<ActiveChecklistDetailsDto> RemoveEditorAsync(Guid activeChecklistId, Guid requestingUserId, Guid editorUserId, CancellationToken ct = default);

    Task<ActiveChecklistDetailsDto> AttachToOwnerOrganizationAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default);
    Task<ActiveChecklistDetailsDto> DetachFromOrganizationAsync(Guid activeChecklistId, Guid requestingUserId, CancellationToken ct = default);
}