using BCrypt.Net;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IDistributedCache _cache;
        public AuthService(IUnitOfWork unitOfWork, IEmailService emailService, IJwtProvider jwtProvider, ITurnstileService turnstileService, IDistributedCache cache)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _jwtProvider = jwtProvider;
            _turnstileService = turnstileService;
            _cache = cache;
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
                if(account.UserStatus == Domain.Enum.UserStatus.Banned) throw new Exception("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.");
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
                    Data = new AuthResponse(accessToken, $"Đăng nhập thành công. ID: {account.UserId} Role: {account.Role}") 
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
                    UserName = request.UserName,
                    Name = request.Name,
                    CreatedAt = DateTime.UtcNow,
                    PhoneNumber = request.PhoneNumber,
                    // Hash mật khẩu bằng BCrypt hoặc PBKDF2 bảo mật
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsActive = false,
                    Role = Domain.Enum.RoleEnum.USER, // Hoặc gán Enum Role của hệ thống
                    Profile = new UserProfile()
                };
                
                await _unitOfWork.userRepository.AddAsync(newAccount);
                await _unitOfWork.SaveChangesAsync();

                // 4. Sinh mã OTP ngẫu nhiên (6 chữ số) và thiết lập hết hạn sau 5 phút
                string otpCode = new Random().Next(100000, 999999).ToString();
                string normalizedEmail = request.Email.Trim().ToLowerInvariant();
                string redisKey = $"OTP:Register:{normalizedEmail}";
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };

                // Lưu mã OTP vào Redis
                await _cache.SetStringAsync(redisKey, otpCode, cacheOptions);
                // 5. Gửi mail chứa mã OTP cho người dùng
                await _emailService.ResendOtpEmailAsync(request.Email, otpCode);
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
                string normalizedEmail = request.Email.Trim().ToLowerInvariant();
                string redisKey = $"OTP:Register:{normalizedEmail}";
                string? savedOtp = await _cache.GetStringAsync(redisKey);

                // 1. Kiểm tra mã OTP trong Redis xem có khớp và còn hạn không
                if (string.IsNullOrEmpty(savedOtp) || savedOtp != request.OtpCode)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Mã OTP không chính xác hoặc đã hết hạn.",
                    };
                }

                // 2. Xóa OTP khỏi Redis ngay sau khi xác thực để tránh replay attack
                await _cache.RemoveAsync(redisKey);

                // 3. Cập nhật trạng thái tài khoản thành Đã xác thực
                var account = await _unitOfWork.userRepository.GetAsync(u => u.Email.ToLower() == normalizedEmail);
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

        public async Task<ServiceResult<AuthResponse>> SendPasswordOtpAsync(ForgotPasswordRequest request)
        {
            try
            {
                string normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var account = await _unitOfWork.userRepository.GetAsync(u => u.Email.ToLower() == normalizedEmail);
                if (account == null)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Email không tồn tại trong hệ thống."
                    };
                }

                string otpCode = new Random().Next(100000, 999999).ToString();
                string redisKey = $"OTP:Password:{normalizedEmail}";
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };

                await _cache.SetStringAsync(redisKey, otpCode, cacheOptions);
                await _emailService.ResendOtpEmailAsync(request.Email, otpCode);

                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = true,
                    Message = "Mã OTP đã được gửi đến email để thiết lập lại mật khẩu."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = $"Gửi OTP thất bại: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<AuthResponse>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                string normalizedEmail = request.Email.Trim().ToLowerInvariant();
                string redisKey = $"OTP:Password:{normalizedEmail}";
                string? savedOtp = await _cache.GetStringAsync(redisKey);

                if (string.IsNullOrEmpty(savedOtp) || savedOtp != request.OtpCode)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Mã OTP không chính xác hoặc đã hết hạn."
                    };
                }

                await _cache.RemoveAsync(redisKey);

                var account = await _unitOfWork.userRepository.GetAsync(u => u.Email.ToLower() == normalizedEmail);
                if (account == null)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Tài khoản không tồn tại."
                    };
                }

                account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = true,
                    Message = "Đặt lại mật khẩu thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = $"Đặt lại mật khẩu thất bại: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<AuthResponse>> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                string normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var account = await _unitOfWork.userRepository.GetAsync(u => u.Email.ToLower() == normalizedEmail);
                if (account == null)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Tài khoản không tồn tại."
                    };
                }

                bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.Password);
                if (!isCurrentPasswordValid)
                {
                    return new ServiceResult<AuthResponse>
                    {
                        IsSuccess = false,
                        Message = "Mật khẩu hiện tại không chính xác."
                    };
                }

                account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = true,
                    Message = "Đổi mật khẩu thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<AuthResponse>
                {
                    IsSuccess = false,
                    Message = $"Đổi mật khẩu thất bại: {ex.Message}"
                };
            }
        }
    }
}
