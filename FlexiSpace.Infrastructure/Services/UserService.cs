using AutoMapper;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Enum;
using FlexiSpace.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<IEnumerable<UserResponse>>> GetAllUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.userRepository.GetAllAsync(filter: x => !x.IsDeleted, include: q => q.Include(x => x.Profile));
                var mapped = _mapper.Map<IEnumerable<UserResponse>>(users);
                return new ServiceResult<IEnumerable<UserResponse>> { IsSuccess = true, Data = mapped };
            }
            catch (System.Exception ex)
            {
                return new ServiceResult<IEnumerable<UserResponse>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<UserResponse>> GetUserByUserIdAsync(string userId)
        {
            try
            {
                var user = await _unitOfWork.userRepository.GetAsync(x => x.UserId == userId && !x.IsDeleted , include: q => q.Include(x => x.Profile));
                if (user == null) return new ServiceResult<UserResponse> { IsSuccess = false, IsNotFound = true, Message = "User not found." };
                var mapped = _mapper.Map<UserResponse>(user);
                return new ServiceResult<UserResponse> { IsSuccess = true, Data = mapped };
            }
            catch (System.Exception ex)
            {
                return new ServiceResult<UserResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<UserResponse>> ChangeUserStatusAsync(string userId, UserStatus status)
        {
            try
            {
                var user = await _unitOfWork.userRepository.GetAsync(x => x.UserId == userId && !x.IsDeleted, include: q => q.Include(x => x.Profile));
                if (user == null) return new ServiceResult<UserResponse> { IsSuccess = false, IsNotFound = true, Message = "User not found." };
                user.UserStatus = status;
                await _unitOfWork.userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                var mapped = _mapper.Map<UserResponse>(user);
                return new ServiceResult<UserResponse> { IsSuccess = true, Data = mapped };
            }
            catch (System.Exception ex)
            {
                return new ServiceResult<UserResponse> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResult<string>> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _unitOfWork.userRepository.GetAsync(x => x.UserId == userId && !x.IsDeleted, include: q => q.Include(x=> x.Profile));
                if (user == null) return new ServiceResult<string> { IsSuccess = false, IsNotFound = true, Message = "User not found." };
                user.IsDeleted = true;
                await _unitOfWork.userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                return new ServiceResult<string> { IsSuccess = true, Data = "Deleted" };
            }
            catch (System.Exception ex)
            {
                return new ServiceResult<string> { IsSuccess = false, Message = ex.Message };
            }
        }
    }
}
