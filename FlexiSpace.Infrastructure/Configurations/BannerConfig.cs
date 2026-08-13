using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class BannerConfig : IEntityTypeConfiguration<Banner>
    {
        public void Configure(EntityTypeBuilder<Banner> builder)
        {
            builder.HasKey(b => b.Id);

            builder.HasOne(b => b.Listing)
                   .WithOne(l => l.Banner)
                   .HasForeignKey<Banner>(b => b.ListingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.PictureURL)
                   .WithOne(p => p.Banner)
                   .HasForeignKey<PictureURL>(p => p.BannerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
