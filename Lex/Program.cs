using BlazorKit.CopyToClipboard;
using MudBlazor.Services;
using Lex.Components;
using Lex.Infrastructure.Data;
using Lex.Endpoints;
using Lex.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();
builder.Services.AddDbContextFactory<AppDbContext>(
    options => options.UseSqlite("Data Source=Lex.db"), ServiceLifetime.Scoped);

builder.Services.AddLexIdentity();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClipboardService, ClipboardService>();

builder.Services.AddLexRepositories();
builder.Services.AddLexApplicationServices();
builder.Services.AddLexAuth();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        await SeedData.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Ошибка при инициализации данных");
    }
}

app.UseLexPipeline();

app.MapRazorPages();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapLexAuthEndpoints();

app.Run();