using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class SubBookingRequestConfig : IEntityTypeConfiguration<SubBookingRequest>
    {
        public void Configure(EntityTypeBuilder<SubBookingRequest> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasOne(s => s.Lessor)
                   .WithMany()
                   .HasForeignKey(s => s.LessorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.PrimaryBookingRequest)
                   .WithOne(p => p.SubBookingRequest)
                   .HasForeignKey<SubBookingRequest>(s => s.PrimaryBookingRequestId)
                   .OnDelete(DeleteBehavior.Cascade);

                 builder.HasOne(s => s.Review)
                     .WithOne()
                     .HasForeignKey<Review>(r => r.SubBookingRequestId)
                     .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
