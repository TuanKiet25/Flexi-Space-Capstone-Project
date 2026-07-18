using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ListingConfig : IEntityTypeConfiguration<Listing>
    {
        public void Configure(EntityTypeBuilder<Listing> builder)
        {
            builder.HasKey(l => l.Id);

            builder.HasOne(l => l.Lessor)
                   .WithMany()
                   .HasForeignKey(l => l.CreatorId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(l => l.PrimaryBookingRequests)
                   .WithOne(p => p.Listing)
                     .HasForeignKey("ListingId")
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(l => l.ShareSpaceDetail)
                     .WithOne(s => s.Listing)
                     .HasForeignKey<ShareSpaceDetail>(s => s.ListingId)
                     .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(l => l.PictureURLs)
                   .WithOne(p => p.Listing)
                     .HasForeignKey("ListingId")
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(l => l.Reports)
                     .WithOne(r => r.Listing)
                     .HasForeignKey("ListingId")
                     .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
