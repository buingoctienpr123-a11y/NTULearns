using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    // Trang xác nhận mua khóa học.
    // Giá chỉ để hiển thị, khi thanh toán luôn lấy lại từ DB.
    public class CheckoutViewModel
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = string.Empty;

        public string? CourseDescription { get; set; }

        public string InstructorName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
