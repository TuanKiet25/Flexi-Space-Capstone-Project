using FlexiSpace.Application.ViewModels.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Validators
{

        public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
        {
            public RegisterRequestValidator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email không được để trống.")
                    .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                    .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.")
                    .Matches("[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ in hoa.")
                    .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.");

                RuleFor(x => x.UserName)
                    .NotEmpty().WithMessage("Tên người dùng không được để trống.")
                    .MaximumLength(50).WithMessage("Tên không được vượt quá 50 ký tự.");

                RuleFor(x => x.PhoneNumber)
                    .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                    .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$").WithMessage("Số điện thoại Việt Nam không hợp lệ.");

                RuleFor(x => x.TurnstileToken)
                    .NotEmpty().WithMessage("Vui lòng xác thực CAPTCHA.");
            }
        }
    
}
