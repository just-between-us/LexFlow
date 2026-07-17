using Lex.Domain.DTOs;
using Lex.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Lex.Domain.Interfaces;

public interface IUserProfileService
{
    Task<ProfileEditModel> GetProfileForEditAsync(Guid userId, CancellationToken ct = default);
    Task SaveProfileAsync(Guid userId, ProfileEditModel model, CancellationToken ct = default);
}

