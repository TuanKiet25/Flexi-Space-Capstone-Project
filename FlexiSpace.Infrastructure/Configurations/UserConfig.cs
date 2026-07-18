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
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasMany(u => u.UserOTPs)
                   .WithOne(otp => otp.User)
                   .HasForeignKey(otp => otp.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
             builder.HasMany(u => u.Notification)
                   .WithOne(n => n.User)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(u => u.Profile)
                   .WithOne(p => p.User)
                   .HasForeignKey<UserProfile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(u => u.SubmittedReports)
                   .WithOne(r => r.Reporter)
                   .HasForeignKey(r => r.ReporterId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
