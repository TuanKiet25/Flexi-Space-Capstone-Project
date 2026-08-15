using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ListingViewDailyStatConfig : IEntityTypeConfiguration<ListingViewDailyStat>
    {
        public void Configure(EntityTypeBuilder<ListingViewDailyStat> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Date)
                .IsRequired();

            builder.Property(x => x.ViewCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(x => new { x.ListingId, x.Date })
                .IsUnique();

            builder.HasOne(x => x.Listing)
                .WithMany(x => x.ViewDailyStats)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
