using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ShareSpaceDetailConfig : IEntityTypeConfiguration<ShareSpaceDetail>
    {
        public void Configure(EntityTypeBuilder<ShareSpaceDetail> builder)
        {
            builder.HasKey(x => x.ListingId);
            builder.HasOne(x => x.Listing)
            .WithOne(l => l.ShareSpaceDetail)
            .HasForeignKey<ShareSpaceDetail>(x => x.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AvailabilitiesTimes)
            .WithOne(a => a.ShareSpaceDetail)
            .HasForeignKey(a => a.ShareSpaceDetailId)
            .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.ShareSpaceAmenities)
            .WithOne(a => a.ShareSpaceDetail)
            .HasForeignKey(a => a.ShareSpaceDetailId)
            .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.ShareSpaceCategories)
            .WithOne(c => c.ShareSpaceDetail)
            .HasForeignKey(c => c.ShareSpaceDetailId)
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
