
using Lex.Components.Auth;

namespace Lex.Extensions;

public static class MiddlewarePipelineExtensions
{
    public static WebApplication UseLexPipeline(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserInfoMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseAntiforgery();

        return app;
    }
}