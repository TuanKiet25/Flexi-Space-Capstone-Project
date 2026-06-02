using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class SpaceAmenityConfig : IEntityTypeConfiguration<SpaceAmenity>
    {
        public void Configure(EntityTypeBuilder<SpaceAmenity> builder)
        {
            builder.HasKey(sa => new { sa.SpaceId, sa.AmenityId });

            builder.HasOne(sa => sa.Space)
                   .WithMany(s => s.SpaceAmenity)
                     .HasForeignKey(sa => sa.SpaceId)
                     .OnDelete(DeleteBehavior.Cascade);

                 builder.HasOne(sa => sa.Amenity)
                     .WithMany(a => a.SpaceAmenity)
                     .HasForeignKey(sa => sa.AmenityId)
                     .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
