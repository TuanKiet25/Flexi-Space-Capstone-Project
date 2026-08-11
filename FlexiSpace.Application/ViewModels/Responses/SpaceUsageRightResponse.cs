using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class SpaceUsageRightResponse
    {
        public long Id { get; set; }
        public long SpaceId { get; set; }
        public long? ContractId { get; set; }
        public string UserId { get; set; }
        public string GrantedByUserId { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool CanShare { get; set; }
        public bool CanGrantSharePermission { get; set; }
        public SpaceUsageRightType Type { get; set; }
        public bool IsActive { get; set; }
    }
}
