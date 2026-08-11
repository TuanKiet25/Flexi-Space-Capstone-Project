using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;

namespace FlexiSpace.Application.IServices
{
    public interface IExternalContractService
    {
        Task<ServiceResult<MessageResponse>> CreateAndShareAsync(CreateExternalContractRequest request);
        Task<ServiceResult<MessageResponse>> ConfirmAsync(long contractId);
    }
}
