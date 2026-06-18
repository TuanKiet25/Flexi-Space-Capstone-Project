using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Application.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ContractService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<ContractResponse>> CreateContractAsync(ContractRequest request)
        {
            try
            {
                var validationMessage = await ValidateRequestAsync(request);
                if (validationMessage != null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = validationMessage
                    };
                }

                var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == request.SpaceId && !x.IsDeleted);
                var primaryBookingRequest = await _unitOfWork.primaryBookingRequestRepository.GetAsync(x => x.Id == request.PrimaryBookingRequestId && !x.IsDeleted);

                var contract = _mapper.Map<Contract>(request);
                contract.LessorId = space.OwnerId;
                contract.LesseeId = primaryBookingRequest.LesseeId;
                contract.CreatedAt = DateTime.Now;
                contract.Status = ContractStatusEnum.Pending;

                await _unitOfWork.contractRepository.AddAsync(contract);
                await _unitOfWork.SaveChangesAsync();

                var result = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == contract.Id && !x.IsDeleted,
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ContractResponse>(result)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<List<ContractResponse>>> GetAllContractsAsync(FilterGetAllContract filter)
        {
            try
            {
                 var lessorId = filter?.LessorId;
                 var lesseeId = filter?.LesseeId;
                 var spaceId = filter?.SpaceId;
                 var status = filter?.Status;

                 var contracts = await _unitOfWork.contractRepository.GetAllAsync(
                    x => !x.IsDeleted &&
                        (string.IsNullOrWhiteSpace(lessorId) || x.LessorId == lessorId) &&
                        (string.IsNullOrWhiteSpace(lesseeId) || x.LesseeId == lesseeId) &&
                        (spaceId == null || x.SpaceId == spaceId) &&
                        (status == null || (int)x.Status == status),
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                return new ServiceResult<List<ContractResponse>>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<List<ContractResponse>>(contracts)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<List<ContractResponse>>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ContractResponse>> GetContractByIdAsync(long id)
        {
            try
            {
                var contract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                if (contract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ContractResponse>(contract)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ContractResponse>> UpdateContractAsync(long id, ContractRequest request)
        {
            try
            {
                var existingContract = await _unitOfWork.contractRepository.GetAsync(
                    x => x.Id == id && !x.IsDeleted,
                    include: q => q.Include(c => c.Space).Include(c => c.PrimaryBookingRequest));

                if (existingContract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }

                var validationMessage = await ValidateRequestAsync(request);
                if (validationMessage != null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        Message = validationMessage
                    };
                }

                var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == request.SpaceId && !x.IsDeleted);
                var primaryBookingRequest = await _unitOfWork.primaryBookingRequestRepository.GetAsync(x => x.Id == request.PrimaryBookingRequestId && !x.IsDeleted);

                _mapper.Map(request, existingContract);
                existingContract.LessorId = space.OwnerId;
                existingContract.LesseeId = primaryBookingRequest.LesseeId;
                existingContract.UpdatedAt = DateTime.Now;

                await _unitOfWork.contractRepository.UpdateAsync(existingContract);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Data = _mapper.Map<ContractResponse>(existingContract)
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResult<ContractResponse>> DeleteContractAsync(long id)
        {
            try
            {
                var existingContract = await _unitOfWork.contractRepository.GetAsync(x => x.Id == id && !x.IsDeleted);

                if (existingContract == null)
                {
                    return new ServiceResult<ContractResponse>
                    {
                        IsSuccess = false,
                        IsNotFound = true,
                        Message = "Không tìm thấy hợp đồng với Id đã cho."
                    };
                }

                await _unitOfWork.contractRepository.RemoveByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = true,
                    Message = "Xóa hợp đồng thành công."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<ContractResponse>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        private async Task<string?> ValidateRequestAsync(ContractRequest request)
        {
            if (request.Duration <= 0)
            {
                return "Thời hạn hợp đồng phải lớn hơn 0.";
            }

            if (request.Price <= 0)
            {
                return "Giá hợp đồng phải lớn hơn 0.";
            }

            if (request.DepositAmount < 0)
            {
                return "Tiền đặt cọc không được nhỏ hơn 0.";
            }

            if (request.Acreage <= 0)
            {
                return "Diện tích phải lớn hơn 0.";
            }

            if (request.EndDate < request.StartDate)
            {
                return "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
            }

            var space = await _unitOfWork.spaceRepository.GetAsync(x => x.Id == request.SpaceId && !x.IsDeleted);
            if (space == null)
            {
                return "Không tìm thấy không gian với Id đã cho.";
            }

            if (string.IsNullOrWhiteSpace(space.OwnerId))
            {
                return "Không thể xác định chủ sở hữu của không gian.";
            }

            var primaryBookingRequest = await _unitOfWork.primaryBookingRequestRepository.GetAsync(x => x.Id == request.PrimaryBookingRequestId && !x.IsDeleted);
            if (primaryBookingRequest == null)
            {
                return "Không tìm thấy yêu cầu đặt chỗ với Id đã cho.";
            }

            if (string.IsNullOrWhiteSpace(primaryBookingRequest.LesseeId))
            {
                return "Không thể xác định người thuê từ yêu cầu đặt chỗ.";
            }

            if (primaryBookingRequest.SpaceId != request.SpaceId)
            {
                return "Yêu cầu đặt chỗ phải thuộc đúng không gian của hợp đồng.";
            }

            return null;
        }
    }
}