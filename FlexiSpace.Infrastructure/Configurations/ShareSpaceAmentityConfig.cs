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
    public class ShareSpaceAmentityConfig : IEntityTypeConfiguration<SharedSpaceAmenities>
    {
        public void Configure(EntityTypeBuilder<SharedSpaceAmenities> builder)
        {
            builder.HasOne(s => s.Amenity)
                   .WithMany()
                   .HasForeignKey(s => s.AmenityId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
