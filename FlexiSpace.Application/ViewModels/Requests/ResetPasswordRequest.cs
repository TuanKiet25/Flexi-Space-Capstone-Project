namespace FlexiSpace.Application.ViewModels.Requests
{
    public record ResetPasswordRequest(string Email, string OtpCode, string NewPassword);
}
