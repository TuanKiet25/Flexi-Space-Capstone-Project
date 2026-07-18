using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Domain.Enum
{
    public enum ReportReasonEnum
    {
        // Có dấu hiệu lừa đảo, chiếm đoạt tài sản
        ScamOrFraud ,

        // Hình ảnh và mô tả không đúng sự thật: Không gian thực tế tồi tàn hơn, nhỏ hơn hoặc khác hoàn toàn so với ảnh chụp
        FakeInformation,
        //Sai mức giá: Giá hiển thị trên web một đằng, nhưng khi nhắn tin lại báo giá khác (phụ thu phí ẩn, bắt ép mua thêm dịch vụ).
        PriceMismatch,
        //Vị trí/Địa chỉ giả mạo: Ghim map sai hoặc đăng địa chỉ không tồn tại.
        FakeAddress,

        // Hình ảnh/Ngôn từ phản cảm: Chứa hình ảnh thô tục, bạo lực hoặc ngôn từ xúc phạm.
        InappropriateContent,
        //Sai mục đích sử dụng: Đăng bán hàng hóa, quảng cáo dịch vụ khác thay vì cho thuê mặt bằng.
        WrongCategory,

        // Khác
        Other
    }
}
