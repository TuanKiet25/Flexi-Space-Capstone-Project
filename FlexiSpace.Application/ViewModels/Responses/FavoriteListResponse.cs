namespace FlexiSpace.Application.ViewModels.Responses
{
    public class FavoriteListResponse
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<ListingResponse> Listings { get; set; } = new();
    }
}
