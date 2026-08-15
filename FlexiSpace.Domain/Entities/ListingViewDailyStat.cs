namespace FlexiSpace.Domain.Entities
{
    public class ListingViewDailyStat
    {
        public long Id { get; set; }
        public long ListingId { get; set; }
        public DateOnly Date { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual Listing Listing { get; set; } = null!;
    }
}
