namespace FlexiSpace.Application.ViewModels.Requests
{
    public class CreateExternalContractRequest
    {
        public long SpaceId { get; set; }
        public string LesseeId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public bool CanShare { get; set; }
        public bool CanGrantSharePermission { get; set; }
    }
}
