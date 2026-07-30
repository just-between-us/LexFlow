using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class DocumentVersionRepository : Repository<DocumentVersion>
{
    public DocumentVersionRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory)
    {
    }

    public async Task UpdateVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default)
    {
        version.UpdatedAtUtc = DateTime.UtcNow;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.DocumentVersions.Update(version);
        await context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task CreateVersionAsync(DocumentVersion version,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var exists = await context.DocumentVersions
            .AnyAsync(v => !v.IsDeleted &&
                           v.DocumentId == version.DocumentId &&
                           v.VersionNumber == version.VersionNumber,
                cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Версия {version.VersionNumber} уже существует для документа {version.DocumentId}");

        var document = await context.Documents
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == version.DocumentId, cancellationToken);

        if (document == null)
            throw new KeyNotFoundException($"Документ с ID {version.DocumentId} не найден");

        document.CurrentVersionNumber = version.VersionNumber;
        document.UpdatedAtUtc = DateTime.UtcNow;

        version.Id = Guid.NewGuid();
        version.CreatedAtUtc = DateTime.UtcNow;
        version.IsDeleted = false;

        await context.DocumentVersions.AddAsync(version, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}