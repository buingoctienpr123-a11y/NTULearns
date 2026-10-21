using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    // Giao dịch mua khóa học trả phí (thanh toán giả lập).
    public class Transaction
    {
        public int TransactionId { get; set; }

        public int UserId { get; set; }

        public int CourseId { get; set; }

        // Số tiền lấy từ giá khóa học trong DB tại thời điểm tạo giao dịch
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // BankTransfer (Chuyển khoản) / EWallet (Ví điện tử)
        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; } = string.Empty;

        // Pending / Success / Failed
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(50)]
        public string TransactionCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PaidAt { get; set; }

        public User? User { get; set; }

        public Course? Course { get; set; }
    }

    // Các phương thức thanh toán giả lập được hỗ trợ
    public static class PaymentMethods
    {
        public const string BankTransfer = "BankTransfer";

        public const string EWallet = "EWallet";

        public static readonly IReadOnlyDictionary<string, string> All =
            new Dictionary<string, string>
            {
                [BankTransfer] = "Chuyển khoản ngân hàng",
                [EWallet] = "Ví điện tử"
            };

        public static string DisplayName(string method)
        {
            return All.TryGetValue(method, out var name)
                ? name : method;
        }
    }
}
