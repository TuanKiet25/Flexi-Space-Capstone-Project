using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResult<IEnumerable<ReviewResponse>>> GetAllAsync()
        {
            try
            {
                var reviews = await _unitOfWork.reviewRepository.GetAllAsync(
                    filter: r => !r.IsDeleted,
                    include: q => q.Include(r => r.Reviewer).ThenInclude(u => u.Profile)
                                   .Include(r => r.TargetUser).ThenInclude(u => u.Profile)
                                   .Include(r => r.PrimaryBookingRequest).ThenInclude(p => p.Space)
                );

                var response = _mapper.Map<IEnumerable<ReviewResponse>>(reviews);
                return new ServiceResult<IEnumerable<ReviewResponse>>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<ReviewResponse>>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi lấy danh sách đánh giá: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<ReviewResponse>>> GetByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new ServiceResult<IEnumerable<ReviewResponse>>
                    {
                        IsSuccess = false,
                        Message = "UserId không được để trống."
                    };
                }

                // Lấy đánh giá về user này (TargetUserId == userId)
                var reviews = await _unitOfWork.reviewRepository.GetAllAsync(
                    filter: r => r.TargetUserId == userId && !r.IsDeleted,
                    include: q => q.Include(r => r.Reviewer).ThenInclude(u => u.Profile)
                                   .Include(r => r.TargetUser).ThenInclude(u => u.Profile)
                                   .Include(r => r.PrimaryBookingRequest).ThenInclude(p => p.Space)
                );

                var response = _mapper.Map<IEnumerable<ReviewResponse>>(reviews);
                return new ServiceResult<IEnumerable<ReviewResponse>>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<ReviewResponse>>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi lấy danh sách đánh giá của người dùng: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<IEnumerable<ReviewResponse>>> GetBySpaceIdAsync(long spaceId)
        {
            try
            {
                // Lấy đánh giá về space này (PrimaryBookingRequest.SpaceId == spaceId)
                var reviews = await _unitOfWork.reviewRepository.GetAllAsync(
                    filter: r => r.PrimaryBookingRequest.SpaceId == spaceId && !r.IsDeleted,
                    include: q => q.Include(r => r.Reviewer).ThenInclude(u => u.Profile)
                                   .Include(r => r.TargetUser).ThenInclude(u => u.Profile)
                                   .Include(r => r.PrimaryBookingRequest).ThenInclude(p => p.Space)
                );

                var response = _mapper.Map<IEnumerable<ReviewResponse>>(reviews);
                return new ServiceResult<IEnumerable<ReviewResponse>>
                {
                    IsSuccess = true,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<ReviewResponse>>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi lấy danh sách đánh giá của mặt bằng: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<ReviewResponse>> CreateAsync(ReviewRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn chưa đăng nhập."
                    };
                }

                if (request.SpaceId == null && string.IsNullOrEmpty(request.TargetUserId))
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn phải cung cấp SpaceId hoặc TargetUserId để đánh giá."
                    };
                }

                if (request.SpaceId != null && !string.IsNullOrEmpty(request.TargetUserId))
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn chỉ được đánh giá mặt bằng hoặc người dùng trong một đánh giá."
                    };
                }

                // Kiểm tra xem yêu cầu đặt chỗ có tồn tại không
                var bookingRequest = await _unitOfWork.primaryBookingRequestRepository.GetAsync(x => x.Id == request.BookingRequestId);
                if (bookingRequest == null)
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Yêu cầu đặt chỗ không tồn tại."
                    };
                }

                // Kiểm tra xem đã có hợp đồng Active/Expired cho yêu cầu đặt chỗ này chưa (từng thuê, làm việc với)
                var contract = await _unitOfWork.contractRepository.GetAsync(c => 
                    c.PrimaryBookingRequestId == request.BookingRequestId && 
                    (c.Status == ContractStatusEnum.Active || c.Status == ContractStatusEnum.Expired));
                
                if (contract == null)
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn chỉ có thể đánh giá khi đã có hợp đồng thuê ở trạng thái Active hoặc Expired."
                    };
                }

                // Kiểm tra xem người đánh giá có phải là một bên trong hợp đồng không
                if (currentUserId != contract.LesseeId && currentUserId != contract.LessorId)
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Bạn không có quyền đánh giá giao dịch này."
                    };
                }

                // Kiểm tra xem đã đánh giá giao dịch này chưa (mỗi booking chỉ có tối đa 1 review)
                var existingReview = await _unitOfWork.reviewRepository.GetAsync(r => r.BookingRequestId == request.BookingRequestId);
                if (existingReview != null)
                {
                    return new ServiceResult<ReviewResponse>
                    {
                        IsSuccess = false,
                        Message = "Yêu cầu đặt chỗ này đã được đánh giá."
                    };
                }

                var review = new Review
                {
                    BookingRequestId = request.BookingRequestId,
                    ReviewerId = currentUserId,
                    Rating = request.Rating,
                    Description = request.Description,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsActive = true,
                    CreatedBy = currentUserId
                };

                // Đánh giá mặt bằng
                if (request.SpaceId.HasValue)
                {
                    if (currentUserId != contract.LesseeId)
                    {
                        return new ServiceResult<ReviewResponse>
                        {
                            IsSuccess = false,
                            Message = "Chỉ người thuê (Lessee) mới được đánh giá mặt bằng."
                        };
                    }

                    if (request.SpaceId.Value != contract.SpaceId)
                    {
                        return new ServiceResult<ReviewResponse>
                        {
                            IsSuccess = false,
                            Message = "Mặt bằng được đánh giá không khớp với hợp đồng."
                        };
                    }

                    review.Name = "Đánh giá mặt bằng";
                    review.TargetUserId = null;
                }
                // Đánh giá người dùng khác
                else
                {
                    string expectedTargetId = (currentUserId == contract.LesseeId) ? contract.LessorId : contract.LesseeId;
                    if (request.TargetUserId != expectedTargetId)
                    {
                        return new ServiceResult<ReviewResponse>
                        {
                            IsSuccess = false,
                            Message = "Người dùng được đánh giá không khớp với đối tác trong hợp đồng."
                        };
                    }

                    if (request.TargetUserId == currentUserId)
                    {
                        return new ServiceResult<ReviewResponse>
                        {
                            IsSuccess = false,
                            Message = "Bạn không thể tự đánh giá chính mình."
                        };
                    }

                    review.Name = "Đánh giá người dùng";
                    review.TargetUserId = request.TargetUserId;
                }

                await _unitOfWork.reviewRepository.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                // Load lại review cùng các navigation properties
                var savedReview = await _unitOfWork.reviewRepository.GetAsync(
                    r => r.Id == review.Id,
                    include: q => q.Include(r => r.Reviewer).ThenInclude(u => u.Profile)
                                   .Include(r => r.TargetUser).ThenInclude(u => u.Profile)
                                   .Include(r => r.PrimaryBookingRequest).ThenInclude(p => p.Space)
                );

                var response = _mapper.Map<ReviewResponse>(savedReview);
                return new ServiceResult<ReviewResponse>
                {
                    IsSuccess = true,
                    Data = response,
                    Message = "Tạo đánh giá thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ReviewResponse>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi tạo đánh giá: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(long id)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        Message = "Bạn chưa đăng nhập."
                    };
                }

                var review = await _unitOfWork.reviewRepository.GetAsync(r => r.Id == id && !r.IsDeleted);
                if (review == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy đánh giá hoặc đánh giá đã bị xóa."
                    };
                }

                if (review.ReviewerId != currentUserId)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        Message = "Bạn chỉ được xóa bài đánh giá của chính mình."
                    };
                }

                review.IsDeleted = true;
                review.UpdatedAt = DateTime.Now;
                review.UpdatedBy = currentUserId;

                await _unitOfWork.reviewRepository.UpdateAsync(review);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "Xóa đánh giá thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi xóa đánh giá: {ex.Message}"
                };
            }
        }
    }
}
