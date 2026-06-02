using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ListingSlotConfig : IEntityTypeConfiguration<ListingSlot>
    {
        public void Configure(EntityTypeBuilder<ListingSlot> builder)
        {
            builder.HasKey(ls => ls.Id);

            builder.HasOne(ls => ls.Listing)
                   .WithOne(l => l.ListingSlot)
                   .HasForeignKey<ListingSlot>(ls => ls.ListingId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}