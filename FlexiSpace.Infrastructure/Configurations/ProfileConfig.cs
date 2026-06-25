using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ProfileConfig : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.HasKey(p => p.UserId);
            builder.HasOne(p => p.User)
                   .WithOne(u => u.Profile)
                   .HasForeignKey<UserProfile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
