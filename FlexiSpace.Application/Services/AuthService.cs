using BCrypt.Net;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IJwtProvider _jwtProvider;
        private readonly ITurnstileService _turnstileService;
        public AuthService(IUnitOfWork unitOfWork, IEmailService emailService, IJwtProvider jwtProvider, ITurnstileService turnstileService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _jwtProvider = jwtProvider;
            _turnstileService = turnstileService;
        }
        public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // 1. Chặn đứng Bot brute-force mật khẩu bằng Turnstile
                bool isHuman = await _turnstileService.VerifyTokenAsync(request.TurnstileToken);
                if (!isHuman) throw new Exception("Xác thực CAPTCHA thất bại.");

                // 2. Tìm tài khoản theo Email
                var account = await _unitOfWork.userRepository.GetAsync(u => u.Email == request.Email);
                if (account == null) throw new Exception("Tài khoản hoặc mật khẩu không chính xác.");

                // 3. Kiểm tra tính hợp lệ của mật khẩu
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, account.Password);
                if (!isPasswordValid) throw new Exception("Tài khoản hoặc mật khẩu không chính xác.");

                // 4. Kiểm tra tài khoản đã qua bước kích hoạt OTP chưa
                if (!account.IsActive)
                    throw new Exception("Tài khoản của bạn chưa được xác thực OTP qua Email.");

                // 5. Tài khoản hợp lệ -> Tiến hành cấp mã JWT Token
                string accessToken = _jwtProvider.GenerateToken(account);
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = true,
                    Message = "Đăng nhập thành công.",
                    Data = new AuthResponse(accessToken, $"Đăng nhập thành công. {account.UserId + account.Role}") 
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = $"Đăng nhập thất bại: {ex.Message}",
                };
            }
        }

        public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // 1. Chặn Bot bằng Turnstile trước tiên
                bool isHuman = await _turnstileService.VerifyTokenAsync(request.TurnstileToken);
                if (!isHuman) throw new Exception("Xác thực CAPTCHA thất bại. Phát hiện hành vi Robot!");

                // 2. Kiểm tra tài khoản đã tồn tại chưa
                var emailExists = await _unitOfWork.userRepository.GetAsync(u => u.Email == request.Email && u.IsActive == true);
                if (emailExists != null) throw new Exception("Email này đã được sử dụng hệ thống.");

                // 3. Khởi tạo Entity mới (Trạng thái mặc định: Chưa kích hoạt)
                var newAccount = new User
                {
                    Email = request.Email,
                    Name = request.Name,
                    Dob = request.Dob,
                    CreatedAt = DateTime.UtcNow,
                    PhoneNumber = request.PhoneNumber,
                    // Hash mật khẩu bằng BCrypt hoặc PBKDF2 bảo mật
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsActive = false,
                    Role = Domain.Enum.RoleEnum.USER // Hoặc gán Enum Role của hệ thống
                };
                if(newAccount.Dob > DateTime.UtcNow) throw new Exception("Ngày sinh không thể ở tương lai");
                
                await _unitOfWork.userRepository.AddAsync(newAccount);

                // 4. Sinh mã OTP ngẫu nhiên (6 chữ số) và thiết lập hết hạn sau 5 phút
                string otpCode = new Random().Next(100000, 999999).ToString();
                DateTime expiryTime = DateTime.UtcNow.AddMinutes(5);

                // Lưu mã OTP vào DB 
                var userOtp = new UserOTP
                {
                    User = newAccount,
                    Email = request.Email,
                    OtpCode = otpCode,
                    ExpiryTime = expiryTime
                };
                await _unitOfWork.userOTPRepository.AddAsync(userOtp);
                await _unitOfWork.SaveChangesAsync();
                // 5. Gửi mail chứa mã OTP cho người dùng
                await _emailService.SendOtpEmailAsync(request.Email, otpCode);
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = true,
                    Message = "Đăng ký thành công! Vui lòng kiểm tra email để nhận mã OTP kích hoạt tài khoản.",
                };
            }
            catch(Exception ex)
            {
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = $"Đăng ký thất bại: {ex.InnerException?.Message ?? ex.Message}",
                };
            }
        }

        public async Task<ServiceResult<AuthResponse>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            try
            {
                // 1. Kiểm tra mã OTP trong DB xem có khớp và còn hạn không
                var checkValidOtp = await _unitOfWork.userOTPRepository.GetAsync(u => u.Email == request.Email && u.OtpCode == request.OtpCode);
                if (checkValidOtp == null)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Mã OTP không chính xác hoặc đã hết hạn.",
                    };
                }
                // 2. Cập nhật trạng thái tài khoản thành Đã xác thực
                var account = await _unitOfWork.userRepository.GetAsync(u => u.Email == request.Email);
                if (account != null)
                {
                    account.IsActive = true;
                    await _unitOfWork.SaveChangesAsync();
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = true,
                        Message = "Xác thực OTP thành công! Tài khoản đã được kích hoạt.",
                        Data = new AuthResponse(_jwtProvider.GenerateToken(account), $"Xác thực OTP thành công! Tài khoản đã được kích hoạt. Id : {account.UserId}")
                    };
                }

                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = "Xác thực OTP thất bại! Tài khoản không tồn tại",
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = $"Xác thực OTP thất bại: {ex.Message}",
                };
            }
        }
    }
}
