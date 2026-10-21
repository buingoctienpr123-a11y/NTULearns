using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    // Danh sách giao dịch cho Admin
    public class TransactionListViewModel
    {
        public string? Status { get; set; }

        public decimal TotalRevenue { get; set; }

        public int SuccessCount { get; set; }

        public int PendingCount { get; set; }

        public int FailedCount { get; set; }

        public List<Transaction> Transactions { get; set; } = new();
    }
}
