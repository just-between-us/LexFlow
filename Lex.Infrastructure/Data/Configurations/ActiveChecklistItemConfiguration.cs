using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Infrastructure.Data.Configurations;

public class ActiveChecklistItemConfiguration : IEntityTypeConfiguration<ActiveChecklistItem>
{
    public void Configure(EntityTypeBuilder<ActiveChecklistItem> builder)
    {
        builder.HasOne(i => i.ActiveChecklist)
            .WithMany(ac => ac.Items)
            .HasForeignKey(i => i.ActiveChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}