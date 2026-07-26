using lex.Application.DTOs;
using lex.Application.Interfaces;
using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace lex.Application.Services;

public class DocumentEditingService : IDocumentEditingService
{
    private readonly DocumentRepository _documentRepo;
    private readonly DocumentVersionRepository _versionRepo;
    private readonly UserManager<User> _userManager;

    public DocumentEditingService(DocumentRepository documentRepo, DocumentVersionRepository versionRepo, UserManager<User> userManager)
    {
        _documentRepo = documentRepo;
        _versionRepo = versionRepo;
        _userManager = userManager;
    }

    public async Task<DocumentEditDto?> GetForEditingAsync(Guid documentId, Guid userId, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct);
        if (document is null || document.IsDeleted) return null;
        if (!CanEdit(document, userId)) return null;

        return MapToDto(document, userId);
    }

    public async Task<DocumentEditDto> CreateNewVersionAsync(
        Guid documentId, Guid userId, int basedOnVersionNumber,
        DocumentFieldsUpdateModel fields, string content, string? changeSummary,
        CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (!CanEdit(document, userId))
            throw new UnauthorizedAccessException("У вас нет прав на редактирование этого документа.");

        var newVersion = new DocumentVersion
        {
            DocumentId = document.Id,
            VersionNumber = document.CurrentVersionNumber + 1,
            Content = content,
            ChangeSummary = changeSummary ?? $"Создана на основе версии {basedOnVersionNumber}",
            VersionCreatedByUserId = userId
        };

        ApplyStatusChange(document, fields.Status);
        document.Title = fields.Title;
        document.Description = fields.Description;
        document.Type = fields.Type;
        document.Privacy = fields.Privacy;

        await _versionRepo.CreateVersionAsync(newVersion, ct);
        await _documentRepo.UpdateAsync(document, ct);

        return await GetForEditingAsync(documentId, userId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить документ после сохранения.");
    }

    public async Task<DocumentEditDto> UpdateExistingVersionAsync(
        Guid documentId, Guid userId, Guid versionId,
        DocumentFieldsUpdateModel fields, string content, string? changeSummary,
        CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (!CanEdit(document, userId))
            throw new UnauthorizedAccessException("У вас нет прав на редактирование этого документа.");

        var version = document.Versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new KeyNotFoundException("Версия не найдена.");

        version.Content = content;
        version.ChangeSummary = changeSummary;
        await _versionRepo.UpdateVersionAsync(version, ct);

        if (version.VersionNumber == document.CurrentVersionNumber)
        {
            ApplyStatusChange(document, fields.Status);
            document.Title = fields.Title;
            document.Description = fields.Description;
            document.Type = fields.Type;
            document.Privacy = fields.Privacy;
            document.UpdatedAtUtc = DateTime.UtcNow;
            await _documentRepo.UpdateAsync(document, ct);
        }

        return await GetForEditingAsync(documentId, userId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить документ после сохранения.");
    }

    public async Task<DocumentEditDto> AddEditorAsync(Guid documentId, Guid requestingUserId, string emailOrUsername, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только создатель документа может добавлять редакторов.");

        emailOrUsername = emailOrUsername.Trim();
        var target = await _userManager.FindByEmailAsync(emailOrUsername)
                     ?? await _userManager.FindByNameAsync(emailOrUsername);

        if (target is null) throw new InvalidOperationException("Пользователь с таким email или логином не найден.");
        if (target.Id == document.CreatedByUserId) throw new InvalidOperationException("Создатель уже имеет полный доступ.");
        if (document.Editors.Any(e => e.Id == target.Id)) throw new InvalidOperationException("Этот пользователь уже добавлен как редактор.");

        document.Editors.Add(target);
        document.UpdatedAtUtc = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(document, ct);

        return await GetForEditingAsync(documentId, requestingUserId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить документ после добавления редактора.");
    }

    public async Task<DocumentEditDto> RemoveEditorAsync(Guid documentId, Guid requestingUserId, Guid editorUserId, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только создатель документа может удалять редакторов.");

        await _documentRepo.RemoveEditorFromDocumentAsync(documentId, editorUserId, ct);

        return await GetForEditingAsync(documentId, requestingUserId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить документ после удаления редактора.");
    }

    public async Task<DocumentEditDto> AttachToOwnerOrganizationAsync(Guid documentId, Guid requestingUserId, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только создатель документа может привязать его к организации.");

        if (document.CreatedByUser.ClientOrganizationId is null)
            throw new InvalidOperationException("У вас нет организации.");

        document.ClientOrganizationId = document.CreatedByUser.ClientOrganizationId;
        document.UpdatedAtUtc = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(document, ct);

        return await GetForEditingAsync(documentId, requestingUserId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить документ после привязки.");
    }

    public async Task<DocumentEditDto> DetachFromOrganizationAsync(Guid documentId, Guid requestingUserId, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только создатель документа может отвязать его от организации.");

        document.ClientOrganizationId = null;
        document.UpdatedAtUtc = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(document, ct);

        return await GetForEditingAsync(documentId, requestingUserId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить документ после отвязки.");
    }

    private static bool CanEdit(Document document, Guid userId) =>
        document.CreatedByUserId == userId || document.Editors.Any(e => e.Id == userId);

    private static void ApplyStatusChange(Document document, DocumentStatus newStatus)
    {
        if (document.Status == newStatus) return;
        if (newStatus == DocumentStatus.Signed) document.SignedAtUtc = DateTime.UtcNow;
        if (newStatus == DocumentStatus.Archived) document.ArchivedAtUtc = DateTime.UtcNow;
        document.Status = newStatus;
    }

    private static DocumentEditDto MapToDto(Document document, Guid requestingUserId) => new()
    {
        Id = document.Id,
        Title = document.Title,
        Description = document.Description,
        Type = document.Type,
        Status = document.Status,
        Privacy = document.Privacy,
        CurrentVersionNumber = document.CurrentVersionNumber,
        TemplateId = document.TemplateId,
        Versions = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionSummaryDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                Content = v.Content,
                ChangeSummary = v.ChangeSummary,
                CreatedByName = v.VersionCreatedByUser?.GetFullName() ?? "Неизвестный",
                CreatedAtUtc = v.CreatedAtUtc
            })
            .ToList(),
        OwnerUserId = document.CreatedByUserId,
        CurrentUserIsOwner = document.CreatedByUserId == requestingUserId,
        Editors = document.Editors
            .Select(e => new DocumentEditorDto
            {
                UserId = e.Id,
                FullName = string.IsNullOrWhiteSpace(e.GetFullName()) ? (e.Email ?? "Без имени") : e.GetFullName()!,
                Email = e.Email
            })
            .OrderBy(e => e.FullName)
            .ToList(),
        ClientOrganizationId = document.ClientOrganizationId,
        ClientOrganizationName = document.ClientOrganization?.Name,
        OwnerHasOrganization = document.CreatedByUser.ClientOrganizationId.HasValue
    };
}