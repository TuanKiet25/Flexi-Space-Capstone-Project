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

                 builder.HasOne(l => l.BussinessCategory)
                     .WithMany(b => b.Listings)
                     .HasForeignKey("BussinessCategoryId")
                     .OnDelete(DeleteBehavior.Restrict);

                 builder.HasOne(l => l.ListingSlot)
                     .WithOne(s => s.Listing)
                     .HasForeignKey<ListingSlot>(s => s.ListingId)
                     .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.PrimaryBookingRequests)
                   .WithOne(p => p.Listing)
                     .HasForeignKey("ListingId")
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
