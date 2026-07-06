using System.Security.Claims;
using Lex.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Lex.Domain.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, string? Error, User? User)> RegisterAsync(string email, string password, string? firstName = null, string? lastName = null);
    Task<(bool Success, string? Error, User? User)> LoginAsync(string email, string password, bool rememberMe = false);
    Task LogoutAsync(HttpContext httpContext);
    Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task<bool> IsInRoleAsync(User user, string role);
    Task<bool> IsAdminAsync(User user);
    Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword);
    Task<bool> UpdateProfileAsync(User user, string? firstName, string? lastName);
    Task<IList<string>> GetUserRolesAsync(User user);
}