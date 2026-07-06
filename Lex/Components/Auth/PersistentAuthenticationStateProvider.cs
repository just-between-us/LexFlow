using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Lex.Application.DTOs;

namespace Lex.Components.Auth;

public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ClaimsPrincipal _anonymousPrincipal = new(new ClaimsIdentity());

    public PersistentAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        var userInfo = context?.Items["UserInfo"] as UserInfo;

        if (userInfo?.IsAuthenticated == true)
        {
            var identity = CreateIdentity(userInfo);
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        return Task.FromResult(new AuthenticationState(_anonymousPrincipal));
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private ClaimsIdentity CreateIdentity(UserInfo userInfo)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.UserId),
            new(ClaimTypes.Name, userInfo.Email ?? userInfo.UserId),
            new(ClaimTypes.Email, userInfo.Email ?? string.Empty)
        };

        if (!string.IsNullOrEmpty(userInfo.FirstName))
            claims.Add(new Claim(ClaimTypes.GivenName, userInfo.FirstName));
        if (!string.IsNullOrEmpty(userInfo.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, userInfo.LastName));

        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsIdentity(claims, "Custom Authentication");
    }
    
}