using lex.Application.Interfaces;
using lex.Application.Services;
using Lex.Components.Auth;
using Lex.Infrastructure.Repositories;
using Microsoft.AspNetCore.Components.Authorization;

namespace Lex.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLexRepositories(this IServiceCollection services)
    {
        services.AddScoped<DocumentTemplateRepository>();
        services.AddScoped<DocumentRepository>();
        services.AddScoped<DocumentVersionRepository>();
        services.AddScoped<ActiveChecklistRepository>();
        services.AddScoped<UserProfileRepository>();
        services.AddScoped<ClientOrganizationRepository>();
        return services;
    }

    public static IServiceCollection AddLexApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ITemplateCatalogService, TemplateCatalogService>();
        services.AddScoped<IChecklistCatalogService, ChecklistCatalogService>();
        services.AddScoped<IActiveChecklistService, ActiveChecklistService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IClientOrganizationService, ClientOrganizationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IOrganizationPublicService, OrganizationPublicService>();
        services.AddScoped<IDocumentEditingService, DocumentEditingService>();
        return services;
    }

    public static IServiceCollection AddLexAuth(this IServiceCollection services)
    {
        services.AddScoped<IdentityService>();
        services.AddScoped<PersistentAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<PersistentAuthenticationStateProvider>());
        services.AddScoped<UserContext>();
        services.AddAuthorization();
        return services;
    }
}