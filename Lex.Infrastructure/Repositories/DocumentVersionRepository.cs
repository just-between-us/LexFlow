using Lex.Domain.Entities;
using Lex.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Repositories;

public class DocumentVersionRepository : Repository<DocumentVersion>
{
    public DocumentVersionRepository(AppDbContext context) : base(context)
    {
    }
    public async Task UpdateVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default)
    {
        version.UpdatedAtUtc = DateTime.UtcNow;
        _dbSet.Update(version);
        await _context.SaveChangesAsync(cancellationToken);
    }
    

    /// Создать новую версию документа
    public virtual async Task CreateVersionAsync(DocumentVersion version,
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
    }
}