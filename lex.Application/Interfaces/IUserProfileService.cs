using lex.Application.DTOs;

namespace lex.Application.Interfaces;

public interface IUserProfileService
{
    Task<ProfileEditModel> GetProfileForEditAsync(Guid userId, CancellationToken ct = default);
    Task SaveProfileAsync(Guid userId, ProfileEditModel model, CancellationToken ct = default);
}

