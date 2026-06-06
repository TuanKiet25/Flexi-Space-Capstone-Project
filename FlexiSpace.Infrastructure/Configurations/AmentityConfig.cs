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

            builder.HasOne(s => s.Space)
                   .WithMany(s => s.Amenity)
                   .HasForeignKey(s => s.SpaceId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}