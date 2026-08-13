using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class PictureURLConfig : IEntityTypeConfiguration<PictureURL>
    {
        public void Configure(EntityTypeBuilder<PictureURL> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.Contract)
                   .WithMany(c => c.PictureURLs)
                   .HasForeignKey(p => p.ContractId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Space)
                   .WithMany(s => s.PictureURL)
                   .HasForeignKey(p => p.SpaceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Listing)
                   .WithMany(l => l.PictureURLs)
                   .HasForeignKey(p => p.ListingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Banner)
                   .WithOne(b => b.PictureURL)
                   .HasForeignKey<PictureURL>(p => p.BannerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
