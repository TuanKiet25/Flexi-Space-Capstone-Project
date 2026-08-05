using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class PriorityLevelConfig : IEntityTypeConfiguration<PriorityLevel>
    {
        public void Configure(EntityTypeBuilder<PriorityLevel> builder)
        {
            builder.HasKey(pl => pl.Id);

            builder.Property(pl => pl.Price)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();
        }
    }
}
