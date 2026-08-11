namespace FlexiSpace.Application.ViewModels.Requests
{
    public class GrantSpaceUsageRightRequest
    {
        public long SpaceId { get; set; }
        public string UserId { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool CanShare { get; set; }
        public bool CanGrantSharePermission { get; set; }
    }

    public class UpdateSpaceUsageRightPermissionRequest
    {
        public bool CanShare { get; set; }
        public bool CanGrantSharePermission { get; set; }
    }
}
