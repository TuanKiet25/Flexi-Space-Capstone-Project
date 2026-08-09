using FlexiSpace.Application.ViewModels.Requests.Space;

namespace FlexiSpace.Application.ViewModels.Responses.Space
{
    public class SpacePartResponse : BaseVModel
    {
        public long Id { get; set; }
        public long ParentSpaceId { get; set; }
        public string? ParentSpaceName { get; set; }
        public string? OwnerId { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public decimal Area { get; set; }
        public bool IsDeleted { get; set; }
        public List<AmenityVModel>? Amenities { get; set; }
        public List<OperatingHourVmodel>? OperatingHours { get; set; }
        public List<SpaceAllowedCategoryVModel>? SpaceAllowedCategories { get; set; }
        public List<PictureURLVModel>? PictureURLs { get; set; }
    }
}
