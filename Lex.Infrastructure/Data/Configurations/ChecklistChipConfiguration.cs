using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Infrastructure.Data.Configurations;

public class ChecklistChipConfiguration : IEntityTypeConfiguration<ChecklistChip>
{
    public void Configure(EntityTypeBuilder<ChecklistChip> builder)
    {
        builder.HasKey(cc => cc.Id);
        builder.HasIndex(cc => cc.Chip);
        builder.HasIndex(cc => cc.ChecklistId);

        builder.HasOne(cc => cc.Checklist)
            .WithMany(c => c.Chips)
            .HasForeignKey(cc => cc.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}   