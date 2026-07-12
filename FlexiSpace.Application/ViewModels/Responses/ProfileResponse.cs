using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ProfileResponse
    {
        required
        public string UserId { get; set; }
        public string? CitizenIDNumber { get; set; }
        public string? IdentityCardNumber { get; set; }
        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public DateOnly Dob { get; set; }
        public string? PermanentResidence { get; set; }
        public DateOnly DateOfIssue { get; set; }
        public bool IsVerified { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? SocialLink { get; set; }
    }
}
