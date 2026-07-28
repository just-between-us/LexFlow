using lex.Application.DTOs;
using lex.Application.Interfaces;
using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Infrastructure.Data;
using Lex.Infrastructure.Repositories;

namespace lex.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly DocumentTemplateRepository _templateRepo;
    private readonly DocumentRepository _documentRepo;
    private readonly DocumentVersionRepository _versionRepo;
    private readonly AppDbContext _dbContext;

    public DocumentService(
        DocumentTemplateRepository templateRepo,
        DocumentRepository documentRepo,
        DocumentVersionRepository versionRepo,
        AppDbContext dbContext)
    {
        _templateRepo = templateRepo;
        _documentRepo = documentRepo;
        _versionRepo = versionRepo;
        _dbContext = dbContext;
    }

    public async Task<TemplateForEditModel?> GetTemplateForEditingAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetTemplateWithHintsAsync(templateId, cancellationToken);
        if (template == null) return null;

        return new TemplateForEditModel
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            CurrentContent = template.CurrentContent,
            Type = template.Type
        };
    }

    public async Task<Document> CreateDocumentFromTemplateAsync(
        Guid templateId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetTemplateWithHintsAsync(templateId, cancellationToken);
        if (template == null)
            throw new KeyNotFoundException($"Шаблон с ID {templateId} не найден");

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = template.Title,
            Description = template.Description,
            Type = template.Type,
            CurrentContent = template.CurrentContent,
            TemplateId = template.Id,
            Status = DocumentStatus.Draft,
            Privacy = DocumentPrivacy.Private,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        var firstVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = 1,
            Content = document.CurrentContent,
            ChangeSummary = "Первоначальная версия (создана из шаблона)",
            VersionCreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _documentRepo.AddAsync(document, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _versionRepo.CreateVersionAsync(firstVersion, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return document;
    }

    public async Task<Document> CreateDocumentFromTemplateAsync(
        Guid templateId,
        Guid userId,
        DocumentPrivacy privacy,
        bool archiveImmediately = false,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetTemplateWithHintsAsync(templateId, cancellationToken);
        if (template == null)
            throw new KeyNotFoundException($"Шаблон с ID {templateId} не найден");

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = template.Title,
            Description = template.Description,
            Type = template.Type,
            CurrentContent = template.CurrentContent,
            TemplateId = template.Id,
            Status = archiveImmediately ? DocumentStatus.Archived : DocumentStatus.Draft,
            Privacy = privacy,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            ArchivedAtUtc = archiveImmediately ? DateTime.UtcNow : null
        };

        var firstVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = 1,
            Content = document.CurrentContent,
            ChangeSummary = archiveImmediately
                ? "Первоначальная версия (создана из шаблона, сразу помещена в архив)"
                : "Первоначальная версия (создана из шаблона)",
            VersionCreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _documentRepo.AddAsync(document, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _versionRepo.CreateVersionAsync(firstVersion, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return document;
    }

    public async Task<(IReadOnlyList<DocumentSummaryDto> Items, int TotalCount)> GetMyDocumentsAsync(
        Guid userId, string? searchTerm, DocumentStatus? status, DocumentPrivacy? privacy,
        DocumentLifecycleFilter lifecycleFilter,
        string sortField, bool sortAscending, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _documentRepo.SearchDocumentsPagedAsync(
            userId, searchTerm, status, privacy, lifecycleFilter, sortField, sortAscending, pageNumber, pageSize, cancellationToken);

        var dtos = items.Select(MapToSummaryDto).ToList();
        return (dtos, total);
    }

    public async Task ToggleArchiveAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepo.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Только создатель документа может архивировать его.");

        if (document.ArchivedAtUtc.HasValue)
        {
            document.ArchivedAtUtc = null;
            document.Status = DocumentStatus.Draft;
        }
        else
        {
            document.ArchivedAtUtc = DateTime.UtcNow;
            document.Status = DocumentStatus.Archived;
        }

        document.UpdatedAtUtc = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(document, cancellationToken);
    }

    public async Task DeleteAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepo.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Только создатель документа может удалить его.");

        document.IsDeleted = true;
        document.DeletedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(document, cancellationToken);
    }

    public async Task RestoreAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepo.GetByIdIncludingDeletedAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Документ не найден.");

        if (document.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Только создатель документа может восстановить его.");

        if (!document.IsDeleted) return; // уже не удалён — идемпотентно, без ошибки

        document.IsDeleted = false;
        document.DeletedAtUtc = null;
        document.UpdatedAtUtc = DateTime.UtcNow;
        await _documentRepo.UpdateAsync(document, cancellationToken);
    }

    private static DocumentSummaryDto MapToSummaryDto(Document d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        Description = d.Description,
        Type = d.Type,
        Status = d.Status,
        Privacy = d.Privacy,
        CurrentVersionNumber = d.CurrentVersionNumber,
        CreatedAtUtc = d.CreatedAtUtc,
        UpdatedAtUtc = d.UpdatedAtUtc,
        ArchivedAtUtc = d.ArchivedAtUtc,
        IsDeleted = d.IsDeleted,
        DeletedAtUtc = d.DeletedAtUtc
    };
}