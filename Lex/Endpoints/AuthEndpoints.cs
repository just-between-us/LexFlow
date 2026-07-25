using Lex.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapLexAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login-handler", HandleLoginAsync);
        app.MapGet("/logout-hendler", HandleLogoutAsync);
        return app;
    }

    private static async Task<IResult> HandleLoginAsync(
        HttpContext context,
        [FromServices] SignInManager<User> signInManager,
        [FromServices] UserManager<User> userManager)
    {
        var form = await context.Request.ReadFormAsync();
        var email = form["email"].ToString();
        var password = form["password"].ToString();
        var rememberMe = form["rememberMe"] == "on";

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return Results.Redirect("/login?ErrorMessage=Пользователь не найден");

        if (!user.IsActive)
            return Results.Redirect("/login?ErrorMessage=Учетная запись деактивирована");

        var result = await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            user.UpdatedAtUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            return Results.Redirect("/");
        }

        return Results.Redirect("/login?ErrorMessage=Неверный email или пароль");
    }

    private static async Task<IResult> HandleLogoutAsync(
        HttpContext context,
        [FromServices] SignInManager<User> signInManager)
    {
        await signInManager.SignOutAsync();

        foreach (var cookie in context.Request.Cookies.Keys)
        {
            if (cookie.StartsWith(".AspNetCore.") || cookie == "Lex.Auth" || cookie.StartsWith("AspNetCore."))
            {
                context.Response.Cookies.Delete(cookie, new CookieOptions
                {
                    Path = "/",
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = true
                });
            }
        }

        return Results.Redirect("/login");
    }
}