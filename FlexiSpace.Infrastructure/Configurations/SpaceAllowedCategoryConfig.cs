using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class SpaceAllowedCategoryConfig : IEntityTypeConfiguration<SpaceAllowedCategory>
    {
        public void Configure(EntityTypeBuilder<SpaceAllowedCategory> builder)
        {
            builder.HasKey(sac => new { sac.SpaceId, sac.BussinessCategoryId });

            builder.HasOne(sac => sac.Space)
                   .WithMany(s => s.SpaceAllowedCategory)
                   .HasForeignKey(sac => sac.SpaceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sac => sac.BussinessCategory)
                   .WithMany(b => b.SpaceAllowedCategories)
                   .HasForeignKey(sac => sac.BussinessCategoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}