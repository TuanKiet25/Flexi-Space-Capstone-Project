using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class AvailabilitiesTimeConfig : IEntityTypeConfiguration<AvailabilitiesTime>
    {
        public void Configure(EntityTypeBuilder<AvailabilitiesTime> builder)
        {
            builder.Property(e => e.DaysOfWeek)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), 
                v => string.IsNullOrEmpty(v)
             ? new List<DayOfWeek>()
             : JsonSerializer.Deserialize<List<DayOfWeek>>(v, JsonSerializerOptions.Default) ?? new List<DayOfWeek>()
            );  
        }
    }
}
