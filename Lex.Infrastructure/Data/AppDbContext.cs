using Lex.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lex.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ClientOrganization> ClientOrganizations => Set<ClientOrganization>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ChecklistChip> ChecklistChips => Set<ChecklistChip>();
    public DbSet<TemplateHint> TemplateHints => Set<TemplateHint>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<ActiveChecklist> ActiveChecklists => Set<ActiveChecklist>();
    public DbSet<ActiveChecklistItem> ActiveChecklistItems => Set<ActiveChecklistItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
        });

        // User <-> ClientOrganization
        builder.Entity<User>()
            .HasOne(u => u.ClientOrganization)
            .WithMany(o => o.Staff)
            .HasForeignKey(u => u.ClientOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ClientOrganization>()
            .HasOne(o => o.OwnerUser)
            .WithMany()
            .HasForeignKey(o => o.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Document
        builder.Entity<Document>(entity =>
        {
            entity.HasOne(d => d.CreatedByUser)
                  .WithMany(u => u.CreatedDocuments)
                  .HasForeignKey(d => d.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(d => d.Editors)
                  .WithMany(u => u.EditableDocuments)
                  .UsingEntity(j => j.ToTable("DocumentEditors"));

            entity.HasOne(d => d.Template)
                  .WithMany()
                  .HasForeignKey(d => d.TemplateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ClientOrganization)
                  .WithMany(o => o.Documents)
                  .HasForeignKey(d => d.ClientOrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // DocumentVersion
        builder.Entity<DocumentVersion>(entity =>
        {
            entity.HasOne(v => v.Document)
                  .WithMany(d => d.Versions)
                  .HasForeignKey(v => v.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.VersionCreatedByUser)
                  .WithMany()
                  .HasForeignKey(v => v.VersionCreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // UserProfile 1:1
        builder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.HasOne(p => p.User)
                  .WithOne(u => u.Profile)
                  .HasForeignKey<UserProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ActiveChecklist
        builder.Entity<ActiveChecklist>(entity =>
        {
            entity.HasOne(ac => ac.User)
                  .WithMany(u => u.ActiveChecklists)
                  .HasForeignKey(ac => ac.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ac => ac.Checklist)
                  .WithMany()
                  .HasForeignKey(ac => ac.ChecklistId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ac => ac.ClientOrganization)
                  .WithMany(o => o.ActiveChecklists)
                  .HasForeignKey(ac => ac.ClientOrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(ac => ac.Editors)
                  .WithMany(u => u.EditableActiveChecklists)
                  .UsingEntity(j => j.ToTable("ActiveChecklistEditors"));

            entity.HasQueryFilter(ac => !ac.IsDeleted);
        });

        // ActiveChecklistItem
        builder.Entity<ActiveChecklistItem>(entity =>
        {
            entity.HasOne(i => i.ActiveChecklist)
                  .WithMany(ac => ac.Items)
                  .HasForeignKey(i => i.ActiveChecklistId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(i => !i.IsDeleted);
        });

        // ---------- ChecklistChip ----------
        builder.Entity<ChecklistChip>(entity =>
        {
            entity.HasKey(cc => cc.Id);
            entity.HasIndex(cc => cc.Chip);
            entity.HasIndex(cc => cc.ChecklistId);
            entity.HasOne(cc => cc.Checklist)
                  .WithMany(c => c.Chips)
                  .HasForeignKey(cc => cc.ChecklistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Checklist ----------
        builder.Entity<Checklist>(entity =>
        {
            entity.HasQueryFilter(c => !c.IsDeleted);
            entity.HasMany(c => c.Items)
                  .WithOne(i => i.Checklist)
                  .HasForeignKey(i => i.ChecklistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChecklistItem (только фильтр)
        builder.Entity<ChecklistItem>(entity =>
        {
            entity.HasQueryFilter(i => !i.IsDeleted);
        });

        // Наследование Document : DocumentTemplate (TPT)
        builder.Entity<DocumentTemplate>().UseTptMappingStrategy();

        // TemplateHint
        builder.Entity<TemplateHint>(entity =>
        {
            entity.HasOne(h => h.DocumentTemplate)
                  .WithMany(t => t.Hints)
                  .HasForeignKey(h => h.DocumentTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Глобальные фильтры для остальных сущностей
        builder.Entity<DocumentTemplate>().HasQueryFilter(t => !t.IsDeleted);
        builder.Entity<ClientOrganization>().HasQueryFilter(o => !o.IsDeleted);
        builder.Entity<DocumentVersion>().HasQueryFilter(v => !v.IsDeleted);
        builder.Entity<TemplateHint>().HasQueryFilter(h => !h.IsDeleted);
    }
}