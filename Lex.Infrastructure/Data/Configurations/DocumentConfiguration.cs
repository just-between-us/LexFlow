using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.CurrentContent).HasMaxLength(150000).IsRequired();
        builder.Property(d => d.Type).IsRequired();

        builder.HasOne(d => d.CreatedByUser)
            .WithMany(u => u.CreatedDocuments)
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Editors)
            .WithMany(u => u.EditableDocuments)
            .UsingEntity(j => j.ToTable("DocumentEditors"));

        builder.HasOne(d => d.Template)
            .WithMany()
            .HasForeignKey(d => d.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ClientOrganization)
            .WithMany(o => o.Documents)
            .HasForeignKey(d => d.ClientOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}