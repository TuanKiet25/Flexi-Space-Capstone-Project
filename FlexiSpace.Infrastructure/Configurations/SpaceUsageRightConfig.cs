using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class SpaceUsageRightConfig : IEntityTypeConfiguration<SpaceUsageRight>
    {
        public void Configure(EntityTypeBuilder<SpaceUsageRight> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                   .IsRequired();

            builder.Property(x => x.GrantedByUserId)
                   .IsRequired();

            builder.Property(x => x.ValidFrom)
                   .IsRequired();

            builder.Property(x => x.ValidTo)
                   .IsRequired();

            builder.HasOne(x => x.Space)
                   .WithMany()
                   .HasForeignKey(x => x.SpaceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Contract)
                   .WithMany()
                   .HasForeignKey(x => x.ContractId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GrantedByUser)
                   .WithMany()
                   .HasForeignKey(x => x.GrantedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.SpaceId, x.UserId });
            builder.HasIndex(x => new { x.SpaceId, x.ValidFrom, x.ValidTo });
            builder.HasIndex(x => x.ContractId)
                   .IsUnique()
                   .HasFilter("\"ContractId\" IS NOT NULL");
        }
    }
}
