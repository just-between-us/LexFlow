using Lex.Domain.DTOs;
using Lex.Domain.Enums;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Repositories;

namespace Lex.Infrastructure.Services;

public class TemplateCatalogService : ITemplateCatalogService
{
    private readonly DocumentTemplateRepository _repository;
    private static readonly TimeSpan NewThreshold = TimeSpan.FromDays(14);

    public TemplateCatalogService(DocumentTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<(IReadOnlyList<DocumentTemplateDto> Items, int TotalCount)> GetTemplatesAsync(
        string? searchTerm,
        DocumentType? type,
        int pageNumber,
        int pageSize,
        string? sortField = null,
        bool sortAscending = true,
        CancellationToken cancellationToken = default)
    {
        var (templates, totalCount) = await _repository.SearchTemplatesWithUsageAsync(
            searchTerm, type, pageNumber, pageSize, sortField, sortAscending, cancellationToken);

        if (!templates.Any())
            return (new List<DocumentTemplateDto>(), totalCount);

        var templateIds = templates.Select(t => t.Id).ToList();

        // Параллельно тянем usage-счётчики и среднее значение
        var usageCountsTask = _repository.GetUsageCountsAsync(templateIds, cancellationToken);
        var averageUsageTask = _repository.GetAverageUsageCountAsync(cancellationToken);
        await Task.WhenAll(usageCountsTask, averageUsageTask);

        var usageCounts = usageCountsTask.Result;
        var averageUsage = averageUsageTask.Result;
        var now = DateTime.UtcNow;

        var dtos = templates.Select(t =>
        {
            var usage = usageCounts.TryGetValue(t.Id, out var count) ? count : 0;
            return new DocumentTemplateDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Type = t.Type,
                HintsCount = t.Hints?.Count(h => !h.IsDeleted) ?? 0,
                UsageCount = usage,
                CreatedAtUtc = t.CreatedAtUtc,
                IsPopular = averageUsage > 0 && usage >= averageUsage * 1.5,
                IsNew = now - t.CreatedAtUtc <= NewThreshold
            };
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<Dictionary<DocumentType, int>> GetTypeStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetTemplateTypesStatsAsync(cancellationToken);
    }
}