using lex.Application.DTOs;
using lex.Application.Interfaces;
using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Repositories;

namespace lex.Application.Services;

public class DocumentEditingService : IDocumentEditingService
{
    private readonly DocumentRepository _documentRepo;
    private readonly DocumentVersionRepository _versionRepo;

    public DocumentEditingService(DocumentRepository documentRepo, DocumentVersionRepository versionRepo)
    {
        _documentRepo = documentRepo;
        _versionRepo = versionRepo;
    }

    public async Task<DocumentEditDto?> GetForEditingAsync(Guid documentId, Guid userId, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetDocumentWithDetailsAsync(documentId, ct);
        if (document is null || document.IsDeleted) return null;
        if (!CanEdit(document, userId)) return null;

        return MapToDto(document);
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

        await _versionRepo.CreateVersionAsync(newVersion, ct); // сам проставит CurrentVersionNumber на документе
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

        // поля документа обновляются, только если правится актуальная версия или можно случайно перезаписать заголовок черновиком из старой версии
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

    private static bool CanEdit(Document document, Guid userId) =>
        document.CreatedByUserId == userId || document.Editors.Any(e => e.Id == userId);

    private static DocumentEditDto MapToDto(Document document) => new()
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
            .ToList()
    };
    private static void ApplyStatusChange(Document document, DocumentStatus newStatus)
    {
        if (document.Status == newStatus) return;

        if (newStatus == DocumentStatus.Signed)
            document.SignedAtUtc = DateTime.UtcNow;

        if (newStatus == DocumentStatus.Archived)
            document.ArchivedAtUtc = DateTime.UtcNow;

        document.Status = newStatus;
    }
}