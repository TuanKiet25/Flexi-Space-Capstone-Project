namespace FlexiSpace.Application.ViewModels.Requests
{
    public record ChangePasswordRequest(string Email, string CurrentPassword, string NewPassword);
}
