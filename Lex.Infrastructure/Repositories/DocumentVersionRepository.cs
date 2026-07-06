using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class DocumentVersionRepository : Repository<DocumentVersion>
{
    public DocumentVersionRepository(AppDbContext context) : base(context)
    {
    }

    /// Получить все версии документа
    public virtual async Task<IReadOnlyList<DocumentVersion>> GetVersionsForDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => !v.IsDeleted && v.DocumentId == documentId)
            .Include(v => v.VersionCreatedByUser)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    /// Получить конкретную версию документа
    public virtual async Task<DocumentVersion?> GetDocumentVersionAsync(
        Guid documentId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => !v.IsDeleted && 
                   v.DocumentId == documentId && 
                   v.VersionNumber == versionNumber)
            .Include(v => v.VersionCreatedByUser)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// Получить последнюю версию документа
    public virtual async Task<DocumentVersion?> GetLatestVersionAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => !v.IsDeleted && v.DocumentId == documentId)
            .Include(v => v.VersionCreatedByUser)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// Получить версию, созданную пользователем
    public virtual async Task<IReadOnlyList<DocumentVersion>> GetVersionsByUserAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => !v.IsDeleted && 
                   v.VersionCreatedByUserId == userId)
            .Include(v => v.Document)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// Получить историю изменений документа за период
    public virtual async Task<IReadOnlyList<DocumentVersion>> GetVersionHistoryAsync(
        Guid documentId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(v => !v.IsDeleted && v.DocumentId == documentId);

        if (fromDate.HasValue)
            query = query.Where(v => v.CreatedAtUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(v => v.CreatedAtUtc <= toDate.Value);

        return await query
            .Include(v => v.VersionCreatedByUser)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Создать новую версию документа
    public virtual async Task<DocumentVersion> CreateVersionAsync(
        DocumentVersion version,
        CancellationToken cancellationToken = default)
    {
        // Проверяем, что версия с таким номером не существует
        var exists = await _dbSet
            .AnyAsync(v => !v.IsDeleted && 
                   v.DocumentId == version.DocumentId && 
                   v.VersionNumber == version.VersionNumber, 
                   cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Версия {version.VersionNumber} уже существует для документа {version.DocumentId}");

        // Получаем текущий документ для обновления CurrentVersionNumber
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == version.DocumentId, cancellationToken);

        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {version.DocumentId} не найден");

        // Обновляем номер текущей версии документа
        document.CurrentVersionNumber = version.VersionNumber;
        document.UpdatedAtUtc = DateTime.UtcNow;

        // Сохраняем версию
        version.Id = Guid.NewGuid();
        version.CreatedAtUtc = DateTime.UtcNow;
        version.IsDeleted = false;

        await _dbSet.AddAsync(version, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return version;
    }

    /// Получить разницу между двумя версиями (для отображения изменений)
    public virtual async Task<(DocumentVersion? Previous, DocumentVersion Current)> GetVersionDiffAsync(
        Guid documentId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var current = await GetDocumentVersionAsync(documentId, versionNumber, cancellationToken);
        if (current == null)
            throw new KeyNotFoundException($"Версия {versionNumber} документа {documentId} не найдена");

        var previous = await _dbSet
            .Where(v => !v.IsDeleted && 
                   v.DocumentId == documentId && 
                   v.VersionNumber < versionNumber)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return (previous, current);
    }

    /// Получить количество версий документа
    public virtual async Task<int> GetVersionCountAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(v => !v.IsDeleted && v.DocumentId == documentId, cancellationToken);
    }

    /// Получить все версии для нескольких документов
    public virtual async Task<Dictionary<Guid, IReadOnlyList<DocumentVersion>>> GetVersionsForDocumentsAsync(
        IEnumerable<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = documentIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<Guid, IReadOnlyList<DocumentVersion>>();

        var versions = await _dbSet
            .Where(v => !v.IsDeleted && ids.Contains(v.DocumentId))
            .Include(v => v.VersionCreatedByUser)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions
            .GroupBy(v => v.DocumentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<DocumentVersion>)g.ToList()
            );
    }

    /// Очистить старые версии (оставить только последние N)
    public virtual async Task<int> CleanupOldVersionsAsync(
        Guid documentId,
        int keepLastVersions = 10,
        CancellationToken cancellationToken = default)
    {
        var versionsToDelete = await _dbSet
            .Where(v => !v.IsDeleted && v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Skip(keepLastVersions)
            .ToListAsync(cancellationToken);

        if (!versionsToDelete.Any())
            return 0;

        foreach (var version in versionsToDelete)
        {
            version.IsDeleted = true;
            version.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return versionsToDelete.Count;
    }

    /// Восстановить документ из версии (создать новую версию на основе старой
    public virtual async Task<DocumentVersion> RestoreVersionAsync(
        Guid documentId,
        int versionNumber,
        Guid restoredByUserId,
        CancellationToken cancellationToken = default)
    {
        var sourceVersion = await GetDocumentVersionAsync(documentId, versionNumber, cancellationToken);
        if (sourceVersion == null)
            throw new KeyNotFoundException($"Версия {versionNumber} документа {documentId} не найдена");

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == documentId, cancellationToken);
        
        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {documentId} не найден");

        var newVersionNumber = document.CurrentVersionNumber + 1;

        var restoredVersion = new DocumentVersion
        {
            DocumentId = documentId,
            VersionNumber = newVersionNumber,
            Content = sourceVersion.Content,
            ChangeSummary = $"Восстановлена версия {versionNumber} от {sourceVersion.CreatedAtUtc:dd.MM.yyyy HH:mm}",
            VersionCreatedByUserId = restoredByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Обновляем документ
        document.CurrentContent = sourceVersion.Content;
        document.CurrentVersionNumber = newVersionNumber;
        document.UpdatedAtUtc = DateTime.UtcNow;

        return await CreateVersionAsync(restoredVersion, cancellationToken);
    }

    /// Получить авторов версий документа
    public virtual async Task<IReadOnlyList<User>> GetVersionAuthorsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var authorIds = await _dbSet
            .Where(v => !v.IsDeleted && v.DocumentId == documentId && v.VersionCreatedByUserId.HasValue)
            .Select(v => v.VersionCreatedByUserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!authorIds.Any())
            return new List<User>();

        return await _context.Users
            .Where(u => authorIds.Contains(u.Id) && u.IsActive)
            .ToListAsync(cancellationToken);
    }
}