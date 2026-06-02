using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ReviewConfig : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasKey(r => r.Id);

            builder.HasOne(r => r.Reviewer)
                   .WithMany()
                   .HasForeignKey(r => r.ReviewerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.TargetUser)
                   .WithMany()
                   .HasForeignKey(r => r.TargetUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}