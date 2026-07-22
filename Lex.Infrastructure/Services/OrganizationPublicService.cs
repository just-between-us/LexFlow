using Lex.Domain.DTOs;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Repositories;

namespace Lex.Infrastructure.Services;

public class OrganizationPublicService : IOrganizationPublicService
{
    private readonly ClientOrganizationRepository _orgRepo;

    public OrganizationPublicService(ClientOrganizationRepository orgRepo)
    {
        _orgRepo = orgRepo;
    }

    public async Task<(IReadOnlyList<PublicOrganizationListItemDto> Items, int TotalCount)> GetPublicOrganizationsAsync(
        string? searchTerm,
        string? sortField,
        bool sortAscending,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var (orgs, total) = await _orgRepo.GetPublicOrganizationsAsync(
            searchTerm, sortField, sortAscending, pageNumber, pageSize, ct);

        var list = orgs.Select(o => new PublicOrganizationListItemDto
        {
            Id = o.Id,
            Name = o.Name,
            Description = o.Description,
            OwnerFullName = o.OwnerUser.GetFullName(),
            StaffCount = o.Staff.Count,
            CreatedAtUtc = o.CreatedAtUtc,
            IsActive = o.IsActive
        }).ToList();

        return (list, total);
    }
    public async Task<OrganizationDetailsDto?> GetOrganizationDetailsAsync(Guid organizationId, Guid currentUserId, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetOrganizationDetailsAsync(organizationId, ct);
        if (org == null) return null;

        // Проверяем, является ли текущий пользователь участником
        bool isMember = org.Staff.Any(u => u.Id == currentUserId) || org.OwnerUserId == currentUserId;

        // Загружаем документы и активные чек-листы параллельно
        var documentsTask = _orgRepo.GetPublicDocumentsByOrganizationAsync(organizationId, ct);
        var activeChecklistsTask = _orgRepo.GetActiveChecklistsByOrganizationAsync(organizationId, ct);
        await Task.WhenAll(documentsTask, activeChecklistsTask);

        var documents = (await documentsTask).Select(d => new OrganizationDocumentDto
        {
            Id = d.Id,
            Title = d.Title,
            Description = d.Description,
            Type = d.Type,
            Status = d.Status,
            CurrentVersionNumber = d.CurrentVersionNumber,
            UpdatedAtUtc = d.UpdatedAtUtc
        }).ToList();

        var checklists = (await activeChecklistsTask).Select(ac => new OrganizationActiveChecklistDto
        {
            ActiveChecklistId = ac.Id,
            ChecklistId = ac.ChecklistId,
            Title = ac.Checklist.Title,
            TotalItems = ac.Items.Count,
            CompletedItems = ac.Items.Count(i => i.IsCompleted)
        }).ToList();

        return new OrganizationDetailsDto
        {
            Id = org.Id,
            Name = org.Name,
            Description = org.Description,
            TaxId = org.TaxId,
            RegistrationNumber = org.RegistrationNumber,
            IsActive = org.IsActive,
            CreatedAtUtc = org.CreatedAtUtc,
            OwnerFullName = org.OwnerUser?.GetFullName() ?? "—",
            StaffCount = org.Staff.Count,
            IsCurrentUserMember = isMember,
            Documents = documents,
            ActiveChecklists = checklists
        };
    }
}