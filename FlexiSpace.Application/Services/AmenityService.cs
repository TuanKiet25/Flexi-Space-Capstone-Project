using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class AmenityService : IAmenityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AmenityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<AmenityResponse>>> GetAllAmenitiesBySpaceIdAsync(long spaceId)
        {
            try
            {
                // Kiểm tra xem Space có tồn tại không
                var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == spaceId);
                if (space == null)
                {
                    return new ServiceResult<IEnumerable<AmenityResponse>>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy mặt bằng với ID đã cho."
                    };
                }

                // Lấy tất cả các tiện ích của mặt bằng
                var amenities = await _unitOfWork.amenityRepository.GetAllAsync(
                    filter: x => x.SpaceId == spaceId
                );

                if (amenities == null || amenities.Count == 0)
                {
                    return new ServiceResult<IEnumerable<AmenityResponse>>
                    {
                        IsSuccess = true,
                        Data = new List<AmenityResponse>(),
                        Message = "Không có tiện ích nào cho mặt bằng này."
                    };
                }

                var result = _mapper.Map<IEnumerable<AmenityResponse>>(amenities);

                return new ServiceResult<IEnumerable<AmenityResponse>>
                {
                    IsSuccess = true,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<IEnumerable<AmenityResponse>>
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi lấy danh sách tiện ích: {ex.Message}"
                };
            }
        }
    }
}
