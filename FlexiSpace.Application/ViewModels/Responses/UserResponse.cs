using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.ViewModels.Responses
{
    public class UserResponse
    {
        required
        public string UserId { get; set; } 
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public RoleEnum Role { get; set; }
        public UserStatus UserStatus { get; set; }
        public string? ProfileFullName { get; set; }
        public string? ProfileAvatarUrl { get; set; }
        public string? ProfileBio { get; set; }
        public string? ProfileSocialLink { get; set; }
        public Gender ProfileGender { get; set; }
    }
}
