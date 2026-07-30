using Lex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lex.Infrastructure.Data.Configurations;

public class ClientOrganizationConfiguration : IEntityTypeConfiguration<ClientOrganization>
{
    public void Configure(EntityTypeBuilder<ClientOrganization> builder)
    {
        builder.HasOne(o => o.OwnerUser)
            .WithMany()
            .HasForeignKey(o => o.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}