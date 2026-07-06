using Lex.Application.DTOs;
using Lex.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Lex.Components.Auth;

public class UserInfoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserInfoMiddleware> _logger;

    public UserInfoMiddleware(RequestDelegate next, ILogger<UserInfoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        UserManager<User> userManager,
        PersistentComponentState state)
    {
        try
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = userManager.GetUserId(context.User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        var userInfo = new UserInfo
                        {
                            UserId = user.Id.ToString(),
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            IsAuthenticated = true,
                            Roles = roles.ToList()
                        };

                        try
                        {
                            context.Items["UserInfo"] = userInfo;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Не удалось сохранить информацию о пользователе в состояние");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении информации о пользователе");
        }

        await _next(context);
    }
}