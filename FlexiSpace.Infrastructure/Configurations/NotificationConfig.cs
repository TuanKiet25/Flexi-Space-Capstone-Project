using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notification)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}