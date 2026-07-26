using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class FavoriteListingConfig : IEntityTypeConfiguration<FavoriteListing>
    {
        public void Configure(EntityTypeBuilder<FavoriteListing> builder)
        {
            builder.HasKey(x => new { x.FavoriteListId, x.ListingId });

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.FavoriteListings)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
