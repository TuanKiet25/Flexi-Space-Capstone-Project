using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface IContractService
    {
        Task<ServiceResult<ContractResponse>> CreateContractAsync(ContractRequest request);
        Task<ServiceResult<List<ContractResponse>>> GetAllContractsAsync(FilterGetAllContract filter);
        Task<ServiceResult<ContractResponse>> GetContractByIdAsync(long id);
        Task<ServiceResult<ContractResponse>> UpdateContractAsync(long id, ContractRequest request);
        Task<ServiceResult<ContractResponse>> DeleteContractAsync(long id);
        Task<ServiceResult<MessageResponse>> ShareContractAsync(long contractId);
        Task<ServiceResult<string>> GetContractSnapshotByIdAsync(long id);
        Task<ServiceResult<bool>> SendContractOtpAsync(long contractId);
        Task<ServiceResult<MessageResponse>> ContractValidateOtpAsync(long contractId, string inputOtp);
    }
}