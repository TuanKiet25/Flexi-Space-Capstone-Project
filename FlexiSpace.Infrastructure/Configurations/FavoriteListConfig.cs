using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class FavoriteListConfig : IEntityTypeConfiguration<FavoriteList>
    {
        public void Configure(EntityTypeBuilder<FavoriteList> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserId).IsRequired();
            builder.HasIndex(x => x.UserId).IsUnique();

            builder.HasOne(x => x.User)
                .WithOne(x => x.FavoriteList)
                .HasForeignKey<FavoriteList>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.FavoriteListings)
                .WithOne(x => x.FavoriteList)
                .HasForeignKey(x => x.FavoriteListId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
