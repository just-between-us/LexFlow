using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Lex.Infrastructure.Services;

public class ClientOrganizationService : IClientOrganizationService
{
    private readonly UserManager<User> _userManager;
    private readonly ClientOrganizationRepository _orgRepo;

    public ClientOrganizationService(UserManager<User> userManager, ClientOrganizationRepository orgRepo)
    {
        _userManager = userManager;
        _orgRepo = orgRepo;
    }

    public async Task<OrganizationDto?> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetForUserAsync(userId, ct);
        return org is null ? null : await MapToDtoAsync(org, userId, ct);
    }

    public async Task<OrganizationDto> CreateAsync(Guid ownerUserId, OrganizationEditModel model, CancellationToken ct = default)
    {
        var existing = await _orgRepo.GetForUserAsync(ownerUserId, ct);
        if (existing != null)
            throw new InvalidOperationException("Вы уже состоите в организации.");

        var user = await _userManager.FindByIdAsync(ownerUserId.ToString());
        if (user is null) throw new KeyNotFoundException("Пользователь не найден.");

        var org = new ClientOrganization
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            TaxId = model.TaxId?.Trim(),
            RegistrationNumber = model.RegistrationNumber?.Trim(),
            OwnerUserId = ownerUserId,
            IsActive = true
        };

        await _orgRepo.AddAsync(org, ct);

        user.ClientOrganizationId = org.Id;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var reloaded = await _orgRepo.GetByIdWithStaffAsync(org.Id, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить созданную организацию.");
        return await MapToDtoAsync(reloaded, ownerUserId, ct);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid organizationId, Guid requestingUserId, OrganizationEditModel model, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetByIdWithStaffAsync(organizationId, ct)
            ?? throw new KeyNotFoundException("Организация не найдена.");

        if (org.OwnerUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только владелец может редактировать организацию.");

        org.Name = model.Name.Trim();
        org.Description = model.Description?.Trim();
        org.TaxId = model.TaxId?.Trim();
        org.RegistrationNumber = model.RegistrationNumber?.Trim();
        org.UpdatedAtUtc = DateTime.UtcNow;

        await _orgRepo.UpdateAsync(org, ct);
        return await MapToDtoAsync(org, requestingUserId, ct);
    }

    public async Task<OrganizationDto> AddStaffMemberAsync(Guid organizationId, Guid requestingUserId, string emailOrUsername, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetByIdWithStaffAsync(organizationId, ct)
            ?? throw new KeyNotFoundException("Организация не найдена.");

        if (org.OwnerUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только владелец может добавлять сотрудников.");

        emailOrUsername = emailOrUsername.Trim();
        var target = await _userManager.FindByEmailAsync(emailOrUsername)
                     ?? await _userManager.FindByNameAsync(emailOrUsername);

        if (target is null)
            throw new InvalidOperationException("Пользователь с таким email или логином не найден.");

        if (target.Id == org.OwnerUserId)
            throw new InvalidOperationException("Владелец уже состоит в организации.");

        if (target.ClientOrganizationId == organizationId)
            throw new InvalidOperationException("Этот пользователь уже в организации.");

        if (target.ClientOrganizationId != null)
            throw new InvalidOperationException("Пользователь уже состоит в другой организации.");

        target.ClientOrganizationId = organizationId;
        var result = await _userManager.UpdateAsync(target);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var reloaded = await _orgRepo.GetByIdWithStaffAsync(organizationId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить организацию.");
        return await MapToDtoAsync(reloaded, requestingUserId, ct);
    }

    public async Task<OrganizationDto> RemoveStaffMemberAsync(Guid organizationId, Guid requestingUserId, Guid staffUserId, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetByIdWithStaffAsync(organizationId, ct)
            ?? throw new KeyNotFoundException("Организация не найдена.");

        if (org.OwnerUserId != requestingUserId)
            throw new UnauthorizedAccessException("Только владелец может удалять сотрудников.");

        if (staffUserId == org.OwnerUserId)
            throw new InvalidOperationException("Нельзя удалить владельца из организации.");

        var staffUser = await _userManager.FindByIdAsync(staffUserId.ToString());
        if (staffUser is null || staffUser.ClientOrganizationId != organizationId)
            throw new InvalidOperationException("Сотрудник не найден в этой организации.");

        staffUser.ClientOrganizationId = null;
        var result = await _userManager.UpdateAsync(staffUser);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var reloaded = await _orgRepo.GetByIdWithStaffAsync(organizationId, ct)
            ?? throw new InvalidOperationException("Не удалось загрузить организацию.");
        return await MapToDtoAsync(reloaded, requestingUserId, ct);
    }

    private async Task<OrganizationDto> MapToDtoAsync(ClientOrganization org, Guid currentUserId, CancellationToken ct)
    {
        var owner = org.OwnerUser ?? await _userManager.FindByIdAsync(org.OwnerUserId.ToString());

        var staffDtos = org.Staff
            .Where(u => u.Id != org.OwnerUserId) // владелец показывается отдельно, не дублируем
            .Select(u => new StaffMemberDto
            {
                UserId = u.Id,
                FullName = string.IsNullOrWhiteSpace(u.GetFullName()) ? (u.Email ?? "Без имени") : u.GetFullName()!,
                Email = u.Email
            })
            .OrderBy(s => s.FullName)
            .ToList();

        return new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Description = org.Description,
            TaxId = org.TaxId,
            RegistrationNumber = org.RegistrationNumber,
            IsActive = org.IsActive,
            CreatedAtUtc = org.CreatedAtUtc,
            OwnerUserId = org.OwnerUserId,
            OwnerFullName = owner is null ? "—" : (string.IsNullOrWhiteSpace(owner.GetFullName()) ? (owner.Email ?? "—") : owner.GetFullName()!),
            OwnerEmail = owner?.Email,
            IsCurrentUserOwner = org.OwnerUserId == currentUserId,
            Staff = staffDtos
        };
    }
}