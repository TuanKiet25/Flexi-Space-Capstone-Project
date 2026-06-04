using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class BussinessCategoryConfig : IEntityTypeConfiguration<BussinessCategory>
    {
        public void Configure(EntityTypeBuilder<BussinessCategory> builder)
        {
            builder.HasKey(b => b.Id);

            builder.HasMany(b => b.SpaceAllowedCategories)
                   .WithOne(sac => sac.BussinessCategory)
                   .HasForeignKey(sac => sac.BussinessCategoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}