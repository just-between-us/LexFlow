using Lex.Domain.DTOs;

namespace Lex.Domain.Interfaces;

public interface IClientOrganizationService
{
    Task<OrganizationDto?> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<OrganizationDto> CreateAsync(Guid ownerUserId, OrganizationEditModel model, CancellationToken ct = default);
    Task<OrganizationDto> UpdateAsync(Guid organizationId, Guid requestingUserId, OrganizationEditModel model, CancellationToken ct = default);
    Task<OrganizationDto> AddStaffMemberAsync(Guid organizationId, Guid requestingUserId, string emailOrUsername, CancellationToken ct = default);
    Task<OrganizationDto> RemoveStaffMemberAsync(Guid organizationId, Guid requestingUserId, Guid staffUserId, CancellationToken ct = default);
}
