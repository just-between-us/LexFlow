using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Lex.Infrastructure.Repositories;

namespace Lex.Infrastructure.Services;

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
        // 1. Загружаем шаблон
        var template = await _templateRepo.GetTemplateWithHintsAsync(templateId, cancellationToken);
        if (template == null)
            throw new KeyNotFoundException($"Шаблон с ID {templateId} не найден");

        // 2. Создаём документ на основе шаблона
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

        // 3. Создаём первую версию документа
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

        // 4. Сохраняем всё в одной транзакции
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Сохраняем документ через репозиторий
            await _documentRepo.AddAsync(document, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Сохраняем версию (CreateVersionAsync сам обновит CurrentVersionNumber)
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
}