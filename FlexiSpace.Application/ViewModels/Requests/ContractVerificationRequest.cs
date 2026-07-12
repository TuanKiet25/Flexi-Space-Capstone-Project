using FlexiSpace.Domain.Enum;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ContractVerificationRequest
    {
        public string ContractId { get; set; } = string.Empty;
        public bool IsLessorAgreed { get; set; }
        public bool IsLesseeAgreed { get; set; }
        public string? LessorSignatureData { get; set; }
        public string? LesseeSignatureData { get; set; }
        public ContractVerificationStatus VerificationStatus { get; set; }
    }
}

