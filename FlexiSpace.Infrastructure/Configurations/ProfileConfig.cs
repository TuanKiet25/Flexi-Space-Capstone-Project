using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ProfileConfig : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.User)
                   .WithOne(u => u.Profile)
                   .HasForeignKey<Profile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
