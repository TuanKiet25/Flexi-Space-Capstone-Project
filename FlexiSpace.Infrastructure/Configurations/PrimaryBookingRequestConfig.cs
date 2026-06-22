using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class PrimaryBookingRequestConfig : IEntityTypeConfiguration<PrimaryBookingRequest>
    {
        public void Configure(EntityTypeBuilder<PrimaryBookingRequest> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.LessorId)
                   .IsRequired();

            builder.Property(p => p.LesseeId)
                   .IsRequired();

            builder.Property(p => p.Duration)
                   .IsRequired();

            builder.Property(p => p.ExpectedEndDate)
                   .IsRequired();

            builder.Property(p => p.ExpectedStartDate)
                   .IsRequired();

            builder.Property(p => p.Status)
                   .IsRequired();

            builder.HasOne(p => p.Lessor)
                   .WithMany()
                   .HasForeignKey(p => p.LessorId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.Lessee)
                    .WithMany()
                    .HasForeignKey(p => p.LesseeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Space)
                   .WithMany(s => s.PrimaryBookingRequest)
                   .HasForeignKey(p => p.SpaceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Listing)
                   .WithMany(l => l.PrimaryBookingRequests)
                   .HasForeignKey(p => p.ListingId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Review)
                   .WithOne()
                   .HasForeignKey<Review>(r => r.BookingRequestId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
