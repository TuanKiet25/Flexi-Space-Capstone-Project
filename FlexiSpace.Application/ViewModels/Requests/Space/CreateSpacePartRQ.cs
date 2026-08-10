namespace FlexiSpace.Application.ViewModels.Requests.Space
{
    public class CreateSpacePartRQ
    {
        public string? Name { get; set; }
        public decimal Area { get; set; }
        public bool IsActive { get; set; } = true;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<AmenityVModel>? Amenities { get; set; }
        public List<OperatingHourVmodel>? OperatingHours { get; set; }
        public List<SpaceAllowedCategoryVModel>? SpaceAllowedCategories { get; set; }
    }

    public class UpdateSpacePartRQ : CreateSpacePartRQ
    {
    }

    public class CreateSpacePartsRQ
    {
        public List<CreateSpacePartRQ> Parts { get; set; } = new List<CreateSpacePartRQ>();
    }
}
