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

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> SearchDocumentsPagedAsync(
        Guid? userId = null,
        string? searchTerm = null,
        DocumentStatus? status = null,
        DocumentPrivacy? privacy = null,
        DocumentLifecycleFilter lifecycleFilter = DocumentLifecycleFilter.NotDeleted,
        string? sortField = null,
        bool sortAscending = false,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.IgnoreQueryFilters().AsNoTracking().AsQueryable();

        query = lifecycleFilter switch
        {
            DocumentLifecycleFilter.ActiveOnly => query.Where(d => !d.IsDeleted && !d.ArchivedAtUtc.HasValue),
            DocumentLifecycleFilter.ArchivedOnly => query.Where(d => !d.IsDeleted && d.ArchivedAtUtc.HasValue),
            DocumentLifecycleFilter.DeletedOnly => query.Where(d => d.IsDeleted),
            _ => query.Where(d => !d.IsDeleted) // NotDeleted
        };

        if (userId.HasValue)
            query = query.Where(d => d.CreatedByUserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term) || d.Description.ToLower().Contains(term));
        }
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        if (privacy.HasValue)
            query = query.Where(d => d.Privacy == privacy.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        IOrderedQueryable<Document> orderedQuery = sortField?.ToLower() switch
        {
            "title" => sortAscending ? query.OrderBy(d => d.Title) : query.OrderByDescending(d => d.Title),
            "status" => sortAscending ? query.OrderBy(d => d.Status) : query.OrderByDescending(d => d.Status),
            "type" => sortAscending ? query.OrderBy(d => d.Type) : query.OrderByDescending(d => d.Type),
            _ => sortAscending
                ? query.OrderBy(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
                : query.OrderByDescending(d => d.UpdatedAtUtc ?? d.CreatedAtUtc)
        };

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// Получить документ по Id, включая удалённые (в обход глобального query filter)
    public async Task<Document?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
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
}