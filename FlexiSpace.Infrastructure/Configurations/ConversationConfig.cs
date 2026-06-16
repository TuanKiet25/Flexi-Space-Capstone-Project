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
    public class ConversationConfig : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasOne(c => c.Lessee)
                   .WithMany()
                   .HasForeignKey(c => c.LesseeId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(builder => builder.Lessor)
                   .WithMany()
                   .HasForeignKey(c => c.LessorId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(c => c.Messages)
                   .WithOne(m => m.Conversation)
                   .HasForeignKey(m => m.ConversationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
