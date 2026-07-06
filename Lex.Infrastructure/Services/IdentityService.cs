using System.Security.Claims;
using Lex.Domain.Entities;
using Lex.Domain.Enums;
using Lex.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Lex.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<IdentityService> _logger;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public IdentityService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager; 
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, User? User)> RegisterAsync(
        string email, 
        string password, 
        string? firstName = null, 
        string? lastName = null)
    {
        try
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));
            }
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                ActivityType = UserActivityType.IndividualEntrepreneur // По умолчанию
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors, null);
            }

            // Назначаем роль User по умолчанию
            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("Пользователь {Email} успешно зарегистрирован", email);
            return (true, null, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при регистрации пользователя {Email}", email);
            return (false, "Произошла ошибка при регистрации", null);
        }
    }

    public async Task<(bool Success, string? Error, User? User)> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, "Пользователь с таким email не найден", null);
            }

            if (!user.IsActive)
            {
                return (false, "Учетная запись деактивирована", null);
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.UpdatedAtUtc = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("Пользователь {Email} успешно вошел в систему", email);
                return (true, null, user);
            }

            if (result.IsLockedOut)
            {
                return (false, "Учетная запись заблокирована. Попробуйте позже", null);
            }

            if (result.IsNotAllowed)
            {
                return (false, "Вход не разрешен. Подтвердите email", null);
            }

            return (false, "Неверный email или пароль", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе пользователя {Email}", email);
            return (false, "Произошла ошибка при входе", null);
        }
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    
        foreach (var cookie in httpContext.Request.Cookies.Keys)
        {
            if (cookie == "Lex.Auth")
            {
                httpContext.Response.Cookies.Delete(cookie);
            }
        }
    
        await _signInManager.SignOutAsync();
    
        _logger.LogInformation("Пользователь вышел из системы");
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
            return null;

        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<bool> IsInRoleAsync(User user, string role)
    {
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> IsAdminAsync(User user)
    {
        return await IsInRoleAsync(user, "Admin");
    }

    public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> UpdateProfileAsync(User user, string? firstName, string? lastName)
    {
        user.FirstName = firstName;
        user.LastName = lastName;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<IList<string>> GetUserRolesAsync(User user)
    {
        return await _userManager.GetRolesAsync(user);
    }
}