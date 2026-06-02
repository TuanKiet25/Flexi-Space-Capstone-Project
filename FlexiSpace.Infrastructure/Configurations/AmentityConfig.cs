using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class AmentityConfig : IEntityTypeConfiguration<Amentity>
    {
        public void Configure(EntityTypeBuilder<Amentity> builder)
        {
            builder.HasKey(a => a.Id);

            builder.HasMany(a => a.SpaceAmenity)
                   .WithOne(sa => sa.Amenity)
                   .HasForeignKey(sa => sa.AmenityId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}