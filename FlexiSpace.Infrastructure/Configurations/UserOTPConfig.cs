using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class UserOTPConfig : IEntityTypeConfiguration<UserOTP>
    {
        public void Configure(EntityTypeBuilder<UserOTP> builder)
        {
            builder.HasKey(o => o.Id);

            builder.HasOne(o => o.User)
                   .WithMany(u => u.UserOTPs)
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}