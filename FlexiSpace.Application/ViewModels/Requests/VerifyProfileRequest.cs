using FlexiSpace.Domain.Enum;
using System;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class VerifyProfileRequest
    {
        public string? CitizenIDNumber { get; set; }
        public string? IdentityCardNumber { get; set; }
        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public DateOnly Dob { get; set; }
        public string? PermanentResidence { get; set; }
        public DateOnly DateOfIssue { get; set; }
    }
}
