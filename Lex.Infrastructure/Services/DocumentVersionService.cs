using Lex.Domain.Entities;

namespace Lex.Infrastructure.Services;

public class DocumentVersionService
{
    /// <summary>
    /// Создаёт новую версию документа на основе переданного содержимого.
    /// Увеличивает CurrentVersionNumber документа.
    /// </summary>
    /// <param name="document">Документ, для которого создаётся версия.</param>
    /// <param name="newContent">Новое содержимое документа (Markdown).</param>
    /// <param name="changeSummary">Краткое описание изменений (опционально).</param>
    /// <param name="userId">ID пользователя, создающего версию.</param>
    /// <returns>Созданная версия документа.</returns>
    /// <exception cref="ArgumentNullException">Если документ или содержимое null.</exception>
    /// <exception cref="ArgumentException">Если содержимое пустое.</exception>
    public DocumentVersion CreateNewVersion(
        Document document,
        string newContent,
        string? changeSummary,
        Guid userId)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Содержимое версии не может быть пустым", nameof(newContent));

        // Если содержимое не изменилось, можно не создавать новую версию,
        // но по условию мы всё равно создаём, т.к. пользователь явно сохраняет.
        // Можно добавить проверку на изменение, но оставим как есть.

        // Увеличиваем номер версии
        var newVersionNumber = document.CurrentVersionNumber + 1;

        // Создаём объект версии
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Document = document, // навигационное свойство (может быть null, но мы не используем)
            VersionNumber = newVersionNumber,
            Content = newContent,
            ChangeSummary = changeSummary ?? $"Версия {newVersionNumber}",
            VersionCreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        // Обновляем состояние документа
        document.CurrentContent = newContent;
        document.CurrentVersionNumber = newVersionNumber;
        document.UpdatedAtUtc = DateTime.UtcNow;

        // Добавляем версию в коллекцию документа (если она уже загружена)
        // Это полезно, если мы работаем с уже загруженным документом с версиями.
        if (document.Versions != null)
        {
            document.Versions.Add(version);
        }

        return version;
    }

    /// <summary>
    /// Восстанавливает документ до состояния указанной версии.
    /// Создаёт новую версию с содержимым восстанавливаемой версии.
    /// </summary>
    /// <param name="document">Документ, который восстанавливается.</param>
    /// <param name="versionToRestore">Версия, на которую восстанавливаем.</param>
    /// <param name="userId">ID пользователя, выполняющего восстановление.</param>
    /// <returns>Новая созданная версия (содержит восстановленное содержимое).</returns>
    /// <exception cref="ArgumentNullException">Если документ или версия null.</exception>
    /// <exception cref="ArgumentException">Если версия не принадлежит этому документу.</exception>
    public DocumentVersion RestoreVersion(
        Document document,
        DocumentVersion versionToRestore,
        Guid userId)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));
        if (versionToRestore == null)
            throw new ArgumentNullException(nameof(versionToRestore));
        if (versionToRestore.DocumentId != document.Id)
            throw new ArgumentException("Указанная версия не принадлежит этому документу", nameof(versionToRestore));

        // Содержимое, которое будем восстанавливать
        var restoredContent = versionToRestore.Content;

        // Создаём новую версию на основе восстановленного содержимого
        var changeSummary = $"Восстановлена версия {versionToRestore.VersionNumber} от {versionToRestore.CreatedAtUtc:dd.MM.yyyy HH:mm}";

        // Используем метод создания новой версии (переиспользуем логику)
        var newVersion = CreateNewVersion(
            document,
            restoredContent,
            changeSummary,
            userId);

        // Дополнительно можно добавить пометку, что это восстановление
        // Например, записать в ChangeSummary дополнительную информацию.
        // Но мы уже указали в changeSummary.

        return newVersion;
    }

    /// <summary>
    /// Проверяет, изменилось ли содержимое документа относительно последней версии.
    /// </summary>
    public bool HasContentChanged(Document document, string newContent)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        // Если документ ещё не имеет версий, считаем что изменилось (если содержимое не пустое)
        if (!document.Versions.Any())
            return !string.IsNullOrWhiteSpace(newContent);

        // Берём последнюю версию (по номеру)
        var lastVersion = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (lastVersion == null)
            return true;

        return lastVersion.Content != newContent;
    }

    /// <summary>
    /// Получает разницу между двумя версиями (для отображения изменений).
    /// Возвращает кортеж (предыдущая версия, текущая версия).
    /// </summary>
    public (DocumentVersion? Previous, DocumentVersion Current) GetVersionDiff(
        Document document,
        int versionNumber)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        var current = document.Versions
            .FirstOrDefault(v => v.VersionNumber == versionNumber && !v.IsDeleted);

        if (current == null)
            throw new ArgumentException($"Версия с номером {versionNumber} не найдена", nameof(versionNumber));

        var previous = document.Versions
            .Where(v => v.VersionNumber < versionNumber && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        return (previous, current);
    }

    /// <summary>
    /// Проверяет, можно ли создать новую версию (например, не превышен ли лимит).
    /// </summary>
    public bool CanCreateVersion(Document document, int maxVersions = 100)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        var activeVersionsCount = document.Versions?.Count(v => !v.IsDeleted) ?? 0;
        return activeVersionsCount < maxVersions;
    }

    /// <summary>
    /// Генерирует краткое описание изменений на основе сравнения содержимого.
    /// (Упрощённая реализация – можно расширить).
    /// </summary>
    public string GenerateChangeSummary(string oldContent, string newContent)
    {
        if (string.IsNullOrEmpty(oldContent) && string.IsNullOrEmpty(newContent))
            return "Нет изменений";

        if (string.IsNullOrEmpty(oldContent))
            return "Создан документ";

        if (string.IsNullOrEmpty(newContent))
            return "Содержимое удалено";

        // Простая эвристика: если длина изменилась сильно
        var lengthDiff = newContent.Length - oldContent.Length;
        if (Math.Abs(lengthDiff) > 1000)
            return lengthDiff > 0 ? "Значительное добавление контента" : "Значительное удаление контента";

        return "Обновление содержимого";
    }
}