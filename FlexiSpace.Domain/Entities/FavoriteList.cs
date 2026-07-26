namespace FlexiSpace.Domain.Entities
{
    public class FavoriteList : BaseEntity
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<FavoriteListing> FavoriteListings { get; set; } = new HashSet<FavoriteListing>();
    }
}
