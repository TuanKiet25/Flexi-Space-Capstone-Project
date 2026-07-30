using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexiSpace.Infrastructure.Configurations
{
    public class TransactionHistoryConfig : IEntityTypeConfiguration<TransactionHistory>
    {
        public void Configure(EntityTypeBuilder<TransactionHistory> builder)
        {
            builder.HasKey(th => th.Id);

            builder.HasOne(th => th.Wallet)
                   .WithMany(w => w.TransactionHistories)
                   .HasForeignKey(th => th.WalletId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
