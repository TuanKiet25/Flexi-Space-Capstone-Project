using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Enum
{
    public enum PrimaryBookingRequestStatusEnum
    {
        Pending ,      // Chờ xử lý
        Negotiating ,  // Đang nhắn tin đàm phán/thương lượng giá
        Approved ,     // Đã chấp thuận (Chuẩn bị ký hợp đồng)
        Rejected,     // Bị từ chối
        Canceled      // Khách chủ động hủy yêu cầu
    }
}
