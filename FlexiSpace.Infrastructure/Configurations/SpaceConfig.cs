using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class SpaceConfig : IEntityTypeConfiguration<Space>
    {
        public void Configure(EntityTypeBuilder<Space> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasOne(s => s.Owner)
                   .WithMany()
                   .HasForeignKey(s => s.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.PrimaryBookingRequest)
                   .WithOne(p => p.Space)
                   .HasForeignKey(p => p.SpaceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Listing)
                   .WithOne(l => l.Space)
                   .HasForeignKey(l => l.SpaceId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
