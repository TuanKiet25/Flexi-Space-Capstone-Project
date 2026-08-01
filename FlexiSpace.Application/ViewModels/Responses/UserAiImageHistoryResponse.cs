namespace FlexiSpace.Application.ViewModels.Responses
{
    public class UserAiImageHistoryResponse
    {
        public long Id { get; set; }
        public string Prompt { get; set; } = null!;
        public string ResultImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
