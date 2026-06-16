using FlexiSpace.Application.IServices;

using FlexiSpace.Web.Hubs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlexiSpace.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestChatController : ControllerBase
    {
        // Cầu nối giúp Controller gọi được các hàm của SignalR Hub
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IMessageService _messageService;
        // Tiêm thêm UnitOfWork/Service của bạn để lưu DB
        // private readonly IUnitOfWork _unitOfWork;

        public TestChatController(IHubContext<ChatHub> hubContext, IMessageService messageService)
        {
            _hubContext = hubContext;
            _messageService = messageService;
        }

        /// <summary>
        /// API dùng để test bắn tin nhắn Real-time qua SignalR
        /// </summary>
        [HttpPost("test-send")]
        public async Task<IActionResult> SendTestMessage( string SenderId, string ReceiverId, string ConversationId, string Content)
        {
            try
            {
                // 1. (Tùy chọn) Viết code lưu tin nhắn xuống Database ở đây
                // var chatMsg = new ChatMessage { ... };
                // await _unitOfWork.ChatMessageRepository.AddAsync(chatMsg);
                // await _unitOfWork.SaveChangesAsync();
                await _messageService.SaveMessageAsync(ConversationId, SenderId, Content);
                // 2. Bắn tin nhắn real-time tới đúng người nhận (ReceiverId)
                await _hubContext.Clients.User(ReceiverId).SendAsync("ReceiveMessage", new
                {
                    ConversationId = ConversationId,
                    SenderId = SenderId,
                    Content = Content,
                    SentAt = DateTime.UtcNow
                });

                return Ok(new { Success = true, Message = "Đã lưu DB và kích hoạt luồng SignalR thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }
    }
}
