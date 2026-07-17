using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Services;


public class UserProfileService : IUserProfileService
{
    private readonly UserManager<User> _userManager;
    private readonly UserProfileRepository _profileRepo;

    public UserProfileService(UserManager<User> userManager, UserProfileRepository profileRepo)
    {
        _userManager = userManager;
        _profileRepo = profileRepo;
    }

    public async Task<ProfileEditModel> GetProfileForEditAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .Include(u => u.ClientOrganization)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null) throw new KeyNotFoundException("User not found");

        var profile = await _profileRepo.GetProfileWithUserAsync(userId, ct);

        return new ProfileEditModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            ActivityType = user.ActivityType,
            JobTitle = profile?.JobTitle,
            CompanyName = profile?.CompanyName,
            Region = profile?.Region,
            BirthDate = profile?.BirthDate,
            Email = user.Email,
            MemberSinceUtc = user.CreatedAtUtc,
            IsActive = user.IsActive,
            OrganizationName = user.ClientOrganization?.Name
        };
    }

    public async Task SaveProfileAsync(Guid userId, ProfileEditModel model, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new KeyNotFoundException("User not found");

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.ActivityType = model.ActivityType;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var profile = await _profileRepo.GetProfileWithUserAsync(userId, ct);
        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                JobTitle = model.JobTitle,
                CompanyName = model.CompanyName,
                Region = model.Region,
                BirthDate = model.BirthDate
            };
            await _profileRepo.AddNewUserProfileAsync(profile, ct);
        }
        else
        {
            profile.JobTitle = model.JobTitle;
            profile.CompanyName = model.CompanyName;
            profile.Region = model.Region;
            profile.BirthDate = model.BirthDate;
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await _profileRepo.UpdateUserProfileAsync(profile, ct);
        }
    }
}