using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Infrastructure.Data.Configurations;

public class ActiveChecklistConfiguration : IEntityTypeConfiguration<ActiveChecklist>
{
    public void Configure(EntityTypeBuilder<ActiveChecklist> builder)
    {
        builder.HasOne(ac => ac.User)
            .WithMany(u => u.ActiveChecklists)
            .HasForeignKey(ac => ac.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ac => ac.Checklist)
            .WithMany()
            .HasForeignKey(ac => ac.ChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ac => ac.ClientOrganization)
            .WithMany(o => o.ActiveChecklists)
            .HasForeignKey(ac => ac.ClientOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(ac => ac.Editors)
            .WithMany(u => u.EditableActiveChecklists)
            .UsingEntity(j => j.ToTable("ActiveChecklistEditors"));

        builder.HasQueryFilter(ac => !ac.IsDeleted);
    }
}