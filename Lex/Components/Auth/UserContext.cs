using Lex.Application.DTOs;
using Lex.Domain.DTOs;

namespace Lex.Components.Auth;

public class UserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserInfo? CurrentUser
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("UserInfo", out var userInfo) == true)
            {
                return userInfo as UserInfo;
            }
            return null;
        }
    }

    public bool IsAuthenticated => CurrentUser?.IsAuthenticated ?? false;
    public bool IsAdmin => CurrentUser?.Roles.Contains("Admin") ?? false;

    public bool IsInRole(string role)
    {
        return CurrentUser?.Roles.Contains(role) ?? false;
    }
}