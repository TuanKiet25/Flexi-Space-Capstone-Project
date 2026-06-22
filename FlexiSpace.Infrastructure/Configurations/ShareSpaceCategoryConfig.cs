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
    public class ShareSpaceCategoryConfig : IEntityTypeConfiguration<ShareSpaceCategory>
    {
        public void Configure(EntityTypeBuilder<ShareSpaceCategory> builder)
        {
            builder.HasOne(s => s.ShareSpaceDetail)
                .WithMany(d => d.ShareSpaceCategories)
                .HasForeignKey(s => s.ShareSpaceDetailId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(s => s.BusinessCategory)
                .WithMany()
                .HasForeignKey(s => s.BussinessCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
