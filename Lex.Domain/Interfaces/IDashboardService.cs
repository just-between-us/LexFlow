using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lex.Domain.Interfaces;

public interface IDashboardService
{
    Task<PersonalDashboardDto> GetPersonalDashboardAsync(Guid userId, CancellationToken ct = default);
    Task<OrganizationDashboardDto?> GetOrganizationDashboardAsync(Guid userId, CancellationToken ct = default);
    Task<ActivityDashboardDto> GetActivityAsync(Guid userId, bool organizationScope, CancellationToken ct = default);

}
