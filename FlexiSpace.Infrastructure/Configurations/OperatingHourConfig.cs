using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class OperatingHourConfig : IEntityTypeConfiguration<OperatingHour>
    {
        public void Configure(EntityTypeBuilder<OperatingHour> builder)
        {
            builder.HasKey(o => o.Id);

            builder.HasOne(o => o.Space)
                   .WithMany(s => s.OperatingHour)
                   .HasForeignKey(o => o.SpaceId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}