using System.ComponentModel.DataAnnotations;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class AddFavoriteListingsRequest
    {
        [Required]
        [MinLength(1)]
        public List<long> ListingIds { get; set; } = new();
    }
}
