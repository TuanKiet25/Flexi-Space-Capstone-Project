namespace FlexiSpace.Domain.Entities
{
    public class FavoriteListing
    {
        public long FavoriteListId { get; set; }
        public long ListingId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual FavoriteList FavoriteList { get; set; } = null!;
        public virtual Listing Listing { get; set; } = null!;
    }
}
