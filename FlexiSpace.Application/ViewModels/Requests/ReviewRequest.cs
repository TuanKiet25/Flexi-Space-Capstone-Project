using System.ComponentModel.DataAnnotations;

namespace FlexiSpace.Application.ViewModels.Requests
{
    public class ReviewRequest
    {
        [Required]
        public long BookingRequestId { get; set; }

        public string? TargetUserId { get; set; }

        public long? SpaceId { get; set; }

        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        public string Description { get; set; }
    }
}
