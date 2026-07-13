using BlazorKit.CopyToClipboard;
using MudBlazor.Services;
using Lex.Components;
using Lex.Components.Auth;
using Lex.Components.Utils;
using Lex.Domain.Entities;
using Lex.Domain.Interfaces;
using Lex.Infrastructure.Data;
using Lex.Infrastructure.Repositories;
using Lex.Infrastructure.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite("Data Source=Lex.db"), ServiceLifetime.Scoped); 

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Lex.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IClipboardService, ClipboardService>();

//Репы
builder.Services.AddScoped<DocumentTemplateRepository>();
builder.Services.AddScoped<ChecklistRepository>();
builder.Services.AddScoped<DocumentRepository>();
builder.Services.AddScoped<DocumentVersionRepository>();

//Сервисы
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChecklistCatalogService, ChecklistCatalogService>();
builder.Services.AddScoped<ITemplateCatalogService, TemplateCatalogService>();

//Утилитарный сервис для получения имени из enum
builder.Services.AddScoped<IDocumentHelperService, DocumentHelperService>();

//Auth
builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

builder.Services.AddScoped<UserContext>();
builder.Services.AddScoped<PersistentAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<PersistentAuthenticationStateProvider>());

builder.Services.AddAuthorization();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>(); 
        logger.LogError(ex, "Ошибка при инициализации данных");
    }
}

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
app.MapRazorPages();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/login-handler", async (
    HttpContext context,
    [FromServices] SignInManager<User> signInManager,
    [FromServices] UserManager<User> userManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"] == "on";

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        return Results.Redirect("/login?ErrorMessage=Пользователь не найден");
    }

    if (!user.IsActive)
    {
        return Results.Redirect("/login?ErrorMessage=Учетная запись деактивирована");
    }

    var result = await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return Results.Redirect("/");
    }

    return Results.Redirect("/login?ErrorMessage=Неверный email или пароль");
});

app.MapGet("/logout-hendler", async (
    HttpContext context,
    [FromServices] SignInManager<User> signInManager) =>
{
    // Выход из системы
    await signInManager.SignOutAsync();
    
    // Удаляем все куки аутентификации вручную
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
    
    // Перенаправляем на страницу входа
    return Results.Redirect("/login");
});

app.Run();