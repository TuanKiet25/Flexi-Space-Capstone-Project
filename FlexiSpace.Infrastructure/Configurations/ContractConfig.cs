using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class ContractConfig : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            // Relationships
            builder.HasOne(c => c.Lessor)
                   .WithMany()
                   .HasForeignKey(c => c.LessorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Lessee)
                   .WithMany()
                   .HasForeignKey(c => c.LesseeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Space)
                   .WithMany(s => s.Contract)
                   .HasForeignKey(c => c.SpaceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.PrimaryBookingRequest)
                   .WithMany(p => p.Contracts)
                   .HasForeignKey(c => c.PrimaryBookingRequestId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Conversation)
                   .WithMany(conv => conv.Contracts)
                   .HasForeignKey(c => c.ConversationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.ContractVerification)
                   .WithOne(cv => cv.Contract)
                   .HasForeignKey<ContractVerification>(cv => cv.ContractId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.ContractSchedules)
                   .WithOne(cs => cs.Contract)
                   .HasForeignKey(cs => cs.ContractId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
