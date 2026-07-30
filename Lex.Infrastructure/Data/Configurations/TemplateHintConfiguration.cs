using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Infrastructure.Data.Configurations;

public class TemplateHintConfiguration : IEntityTypeConfiguration<TemplateHint>
{
    public void Configure(EntityTypeBuilder<TemplateHint> builder)
    {
        builder.HasOne(h => h.DocumentTemplate)
            .WithMany(t => t.Hints)
            .HasForeignKey(h => h.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}