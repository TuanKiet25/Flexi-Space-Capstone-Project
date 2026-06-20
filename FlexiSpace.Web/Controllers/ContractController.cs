using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Contract;
using Microsoft.AspNetCore.Mvc;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : MyBaseController
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateContract([FromBody] ContractRequest request)
        {
            var result = await _contractService.CreateContractAsync(request);
            return HandleResult(result);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllContracts([FromQuery] FilterGetAllContract filter)
        {
            var result = await _contractService.GetAllContractsAsync(filter);
            return HandleResult(result);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetContractById(long id)
        {
            var result = await _contractService.GetContractByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateContract(long id, [FromBody] ContractRequest request)
        {
            var result = await _contractService.UpdateContractAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteContract(long id)
        {
            var result = await _contractService.DeleteContractAsync(id);
            return HandleResult(result);
        }
    }
}