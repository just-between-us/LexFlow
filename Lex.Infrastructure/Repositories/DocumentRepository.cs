using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Lex.Domain.Enums;
using Lex.Infrastructure.Data;

namespace Lex.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>
{
    public DocumentRepository(AppDbContext context) : base(context)
    {
    }

    /// Получить документ со всеми связанными данными
    public virtual async Task<Document?> GetDocumentWithDetailsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => !d.IsDeleted && d.Id == documentId)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Template)
            .Include(d => d.ClientOrganization)
            .Include(d => d.Editors.Where(e => e.IsActive))
            .Include(d => d.Versions.Where(v => !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber))
                .ThenInclude(v => v.VersionCreatedByUser)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// Получить документы пользователя
    public virtual async Task<IReadOnlyList<Document>> GetUserDocumentsAsync(
        Guid userId,
        bool includeTemplates = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(d => !d.IsDeleted && d.CreatedByUserId == userId);

        if (includeTemplates)
        {
            query = query.Include(d => d.Template);
        }

        return await query
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Получить документы, доступные пользователю для редактирования
    public virtual async Task<IReadOnlyList<Document>> GetEditableDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => !d.IsDeleted && (
                d.CreatedByUserId == userId ||
                d.Editors.Any(e => e.Id == userId) ||
                (d.ClientOrganizationId != null && 
                 _context.Users.Any(u => u.Id == userId && u.ClientOrganizationId == d.ClientOrganizationId))
            ))
            .Include(d => d.ClientOrganization)
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Получить документы организации
    public virtual async Task<IReadOnlyList<Document>> GetOrganizationDocumentsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => !d.IsDeleted && d.ClientOrganizationId == organizationId)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ClientOrganization)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Получить публичные документы
    public virtual async Task<IReadOnlyList<Document>> GetPublicDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => !d.IsDeleted && d.Privacy == DocumentPrivacy.Public)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ClientOrganization)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Получить документы по статусу
    public virtual async Task<IReadOnlyList<Document>> GetDocumentsByStatusAsync(
        DocumentStatus status,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => !d.IsDeleted && d.Status == status);

        if (userId.HasValue)
        {
            query = query.Where(d => d.CreatedByUserId == userId.Value);
        }

        return await query
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Получить документы по видимости
    public virtual async Task<IReadOnlyList<Document>> GetDocumentsByPrivacyAsync(
        DocumentPrivacy privacy,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => !d.IsDeleted && d.Privacy == privacy);

        if (userId.HasValue)
        {
            query = query.Where(d => d.CreatedByUserId == userId.Value);
        }

        return await query
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Поиск документов
    public virtual async Task<IReadOnlyList<Document>> SearchDocumentsAsync(
        string? searchTerm,
        DocumentType? type = null,
        DocumentStatus? status = null,
        Guid? userId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(d => 
                d.Title.ToLower().Contains(searchTerm) ||
                d.Description.ToLower().Contains(searchTerm) ||
                d.CurrentContent.ToLower().Contains(searchTerm));
        }

        if (type.HasValue)
        {
            query = query.Where(d => d.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(d => d.CreatedByUserId == userId.Value);
        }

        return await query
            .Include(d => d.CreatedByUser)
            .Include(d => d.ClientOrganization)
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// Получить документы с версиями
    public virtual async Task<Document?> GetDocumentWithVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => !d.IsDeleted && d.Id == documentId)
            .Include(d => d.Versions.Where(v => !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber))
                .ThenInclude(v => v.VersionCreatedByUser)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Template)
            .Include(d => d.Editors)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// Добавить версию документа
    public virtual async Task AddDocumentVersionAsync(
        DocumentVersion version,
        CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(version.DocumentId, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {version.DocumentId} не найден");

        version.Id = Guid.NewGuid();
        version.CreatedAtUtc = DateTime.UtcNow;
        version.IsDeleted = false;

        document.CurrentVersionNumber = version.VersionNumber;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await _context.DocumentVersions.AddAsync(version, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// Обновить статус документа
    public virtual async Task<Document> UpdateDocumentStatusAsync(
        Guid documentId,
        DocumentStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(documentId, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {documentId} не найден");

        document.Status = newStatus;
        
        if (newStatus == DocumentStatus.Signed)
        {
            document.SignedAtUtc = DateTime.UtcNow;
        }
        else if (newStatus == DocumentStatus.Archived)
        {
            document.ArchivedAtUtc = DateTime.UtcNow;
        }
        else if (newStatus == DocumentStatus.Deleted)
        {
            document.DeletedAtUtc = DateTime.UtcNow;
        }

        document.UpdatedAtUtc = DateTime.UtcNow;
        await UpdateAsync(document, cancellationToken);

        return document;
    }

    /// Добавить редактора документа
    public virtual async Task AddEditorToDocumentAsync(
        Guid documentId,
        Guid editorId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbSet
            .Include(d => d.Editors)
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == documentId, cancellationToken);

        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {documentId} не найден");

        var editor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == editorId && u.IsActive, cancellationToken);

        if (editor == null)
            throw new KeyNotFoundException($"Пользователь с ID {editorId} не найден");

        if (document.Editors.All(e => e.Id != editorId))
        {
            document.Editors.Add(editor);
            document.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// Удалить редактора документа
    public virtual async Task RemoveEditorFromDocumentAsync(
        Guid documentId,
        Guid editorId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbSet
            .Include(d => d.Editors)
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == documentId, cancellationToken);

        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {documentId} не найден");

        var editor = document.Editors.FirstOrDefault(e => e.Id == editorId);
        if (editor != null)
        {
            document.Editors.Remove(editor);
            document.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// Проверить, имеет ли пользователь доступ к документу
    public virtual async Task<bool> HasUserAccessToDocumentAsync(
        Guid documentId,
        Guid userId,
        bool requireEdit = false,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbSet
            .Where(d => !d.IsDeleted && d.Id == documentId)
            .Include(d => d.Editors)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
            return false;

        // Создатель всегда имеет доступ
        if (document.CreatedByUserId == userId)
            return true;

        // Проверка на редактирование
        if (requireEdit && document.Editors.Any(e => e.Id == userId))
            return true;

        // Проверка на чтение
        if (!requireEdit)
        {
            // Публичный документ доступен всем
            if (document.Privacy == DocumentPrivacy.Public)
                return true;

            // Документ организации доступен участникам
            if (document.ClientOrganizationId.HasValue)
            {
                var isMember = await _context.Users
                    .AnyAsync(u => u.Id == userId && 
                           u.ClientOrganizationId == document.ClientOrganizationId.Value, 
                           cancellationToken);
                if (isMember)
                    return true;
            }

            // Редакторы имеют доступ на чтение
            if (document.Editors.Any(e => e.Id == userId))
                return true;
        }

        return false;
    }

    /// Получить статистику по документам пользователя
    public virtual async Task<(int Total, int Draft, int InReview, int Ready, int Signed, int Archived)> 
        GetUserDocumentsStatsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => !d.IsDeleted && d.CreatedByUserId == userId);

        var total = await query.CountAsync(cancellationToken);
        var draft = await query.CountAsync(d => d.Status == DocumentStatus.Draft, cancellationToken);
        var inReview = await query.CountAsync(d => d.Status == DocumentStatus.InReview, cancellationToken);
        var ready = await query.CountAsync(d => d.Status == DocumentStatus.Ready, cancellationToken);
        var signed = await query.CountAsync(d => d.Status == DocumentStatus.Signed, cancellationToken);
        var archived = await query.CountAsync(d => d.Status == DocumentStatus.Archived, cancellationToken);

        return (total, draft, inReview, ready, signed, archived);
    }

    /// Получить недавно измененные документы
    public virtual async Task<IReadOnlyList<Document>> GetRecentDocumentsAsync(
        Guid? userId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => !d.IsDeleted);

        if (userId.HasValue)
        {
            query = query.Where(d => d.CreatedByUserId == userId.Value);
        }

        return await query
            .OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
            .Take(limit)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ClientOrganization)
            .ToListAsync(cancellationToken);
    }

    /// Получить документы с фильтрацией по дате
    public virtual async Task<IReadOnlyList<Document>> GetDocumentsByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(d => !d.IsDeleted && 
                   d.CreatedAtUtc >= fromDate && 
                   d.CreatedAtUtc <= toDate);

        if (userId.HasValue)
        {
            query = query.Where(d => d.CreatedByUserId == userId.Value);
        }

        return await query
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// Перенести документы в другую организацию
    public virtual async Task<int> TransferDocumentsToOrganizationAsync(
        IEnumerable<Guid> documentIds,
        Guid newOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var ids = documentIds.ToList();
        if (!ids.Any())
            return 0;

        var documents = await _dbSet
            .Where(d => ids.Contains(d.Id) && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var doc in documents)
        {
            doc.ClientOrganizationId = newOrganizationId;
            doc.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return documents.Count;
    }

    /// Экспорт документов (получить в формате для экспорта)
    public virtual async Task<IReadOnlyList<Document>> GetDocumentsForExportAsync(
        IEnumerable<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = documentIds.ToList();
        if (!ids.Any())
            return new List<Document>();

        return await _dbSet
            .Where(d => ids.Contains(d.Id) && !d.IsDeleted)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Template)
            .Include(d => d.Versions.Where(v => !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber))
                .ThenInclude(v => v.VersionCreatedByUser)
            .ToListAsync(cancellationToken);
    }
}