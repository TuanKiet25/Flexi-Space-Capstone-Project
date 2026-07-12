using FlexiSpace.Application.ViewModels.Responses;
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
    public class ContractVerificationConfig : IEntityTypeConfiguration<ContractVerification>
    {
        public void Configure(EntityTypeBuilder<ContractVerification> builder)
        {

            builder.HasKey(v => v.ContractId); 
            builder.Property(v => v.IsLessorAgreed)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(v => v.LessorSignedAt)
                   .HasColumnType("timestamp with time zone"); // Chuẩn múi giờ UTC của PostgreSQL

            builder.Property(v => v.LessorIpAddress)
                   .HasMaxLength(45); 

            builder.Property(v => v.LessorSignatureData)
                   .HasMaxLength(250);

            builder.Property(v => v.IsLesseeAgreed)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(v => v.LesseeSignedAt)
                   .HasColumnType("timestamp with time zone");

            builder.Property(v => v.LesseeIpAddress)
                   .HasMaxLength(45);

            builder.Property(v => v.LesseeSignatureData)
                   .HasMaxLength(250);


        }
    }
}
