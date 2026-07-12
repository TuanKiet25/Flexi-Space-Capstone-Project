using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Entities
{
    public class ContractVerification
    {
        public long ContractId { get; set; }
        public bool IsLessorAgreed { get; set; } = false;
#nullable enable
        public DateTime? LessorSignedAt { get; set; }
        public string? LessorIpAddress { get; set; }
        public string? LessorSignatureData { get; set; } // Vết xác thực (ví dụ: "Verified via Email OTP")

        public bool IsLesseeAgreed { get; set; } = false;
        public DateTime? LesseeSignedAt { get; set; }
        public string? LesseeIpAddress { get; set; }
        public string? LesseeSignatureData { get; set; }
        
        public ContractVerificationStatus VerificationStatus { get; set; }
        public virtual Contract? Contract { get; set; } 
    }
}
