using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    // Mua khóa học trả phí bằng thanh toán giả lập
    // (không tích hợp cổng thanh toán thật).
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly NTULearnContext _context;

        public TransactionsController(NTULearnContext context)
        {
            _context = context;
        }

        private int? CurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id : null;
        }

        // =========================
        // XÁC NHẬN MUA KHÓA HỌC - GET
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpGet]
        public async Task<IActionResult> Checkout(int courseId)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            var check = await CheckCanBuyAsync(course, userId.Value);
            if (check != null)
                return check;

            return View(ToCheckoutModel(course!, PaymentMethods.BankTransfer));
        }

        // =========================
        // XÁC NHẬN MUA KHÓA HỌC - POST
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int courseId, string? paymentMethod)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            var check = await CheckCanBuyAsync(course, userId.Value);
            if (check != null)
                return check;

            if (string.IsNullOrEmpty(paymentMethod) ||
                !PaymentMethods.All.ContainsKey(paymentMethod))
            {
                ModelState.AddModelError(
                    nameof(CheckoutViewModel.PaymentMethod),
                    "Phương thức thanh toán không hợp lệ.");

                return View(ToCheckoutModel(course!, paymentMethod ?? ""));
            }

            // Dùng lại giao dịch đang chờ thanh toán (nếu có)
            // để tránh tạo nhiều giao dịch cho cùng một khóa học.
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.CourseId == courseId &&
                    t.Status == "Pending");

            if (transaction == null)
            {
                transaction = new Transaction
                {
                    UserId = userId.Value,
                    CourseId = courseId,
                    TransactionCode = GenerateTransactionCode(),
                    CreatedAt = DateTime.Now,
                    Status = "Pending"
                };

                _context.Transactions.Add(transaction);
            }

            // Giá luôn lấy từ DB, không nhận từ client
            transaction.Amount = course!.Price;
            transaction.PaymentMethod = paymentMethod;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Pay),
                new { code = transaction.TransactionCode });
        }

        // =========================
        // TRANG THANH TOÁN GIẢ LẬP
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpGet]
        public async Task<IActionResult> Pay(string code)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var transaction = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t =>
                    t.TransactionCode == code &&
                    t.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (transaction.Status != "Pending")
            {
                TempData["Error"] = "Giao dịch này đã được xử lý.";
                return RedirectToAction(nameof(History));
            }

            return View(transaction);
        }

        // =========================
        // XÁC NHẬN ĐÃ THANH TOÁN (GIẢ LẬP THÀNH CÔNG)
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string code)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var transaction = await _context.Transactions
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t =>
                    t.TransactionCode == code &&
                    t.UserId == userId);

            if (transaction?.Course == null)
                return NotFound();

            if (transaction.Status != "Pending")
            {
                TempData["Error"] = "Giao dịch này đã được xử lý.";
                return RedirectToAction(nameof(History));
            }

            var course = transaction.Course;

            string? failReason = null;

            if (course.Status != "Published")
            {
                failReason = "Khóa học hiện không còn mở bán.";
            }
            else if (course.Price != transaction.Amount)
            {
                failReason =
                    "Giá khóa học đã thay đổi. Vui lòng thực hiện lại giao dịch.";
            }
            else if (await _context.Enrollments.AnyAsync(e =>
                         e.UserId == userId &&
                         e.CourseId == course.CourseId))
            {
                failReason = "Bạn đã sở hữu khóa học này.";
            }

            if (failReason != null)
            {
                transaction.Status = "Failed";
                await _context.SaveChangesAsync();

                TempData["Error"] = "Thanh toán thất bại: " + failReason;
                return RedirectToAction(nameof(History));
            }

            // Cập nhật giao dịch và ghi danh trong cùng một lần lưu
            // để hai thao tác cùng thành công hoặc cùng thất bại.
            transaction.Status = "Success";
            transaction.PaidAt = DateTime.Now;

            _context.Enrollments.Add(new Enrollment
            {
                UserId = userId.Value,
                CourseId = course.CourseId,
                EnrolledAt = DateTime.Now,
                Status = "Active"
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Unique Index (UserId, CourseId) ngăn ghi danh trùng
                // khi có hai yêu cầu thanh toán đồng thời.
                _context.ChangeTracker.Clear();

                var current = await _context.Transactions
                    .FirstAsync(t =>
                        t.TransactionId == transaction.TransactionId);

                bool enrolled = await _context.Enrollments
                    .AnyAsync(e =>
                        e.UserId == userId &&
                        e.CourseId == course.CourseId);

                if (!enrolled)
                    throw;

                if (current.Status == "Pending")
                {
                    current.Status = "Failed";
                    await _context.SaveChangesAsync();
                }

                TempData["Error"] = "Bạn đã sở hữu khóa học này.";
                return RedirectToAction(
                    "MyCourses", "Enrollments");
            }

            TempData["Success"] =
                "Thanh toán thành công! Bạn đã được ghi danh vào khóa học.";

            return RedirectToAction("MyCourses", "Enrollments");
        }

        // =========================
        // HỦY THANH TOÁN (GIẢ LẬP THẤT BẠI)
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string code)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                    t.TransactionCode == code &&
                    t.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (transaction.Status == "Pending")
            {
                transaction.Status = "Failed";
                await _context.SaveChangesAsync();

                TempData["Error"] = "Giao dịch đã bị hủy.";
            }
            else
            {
                TempData["Error"] = "Giao dịch này đã được xử lý.";
            }

            return RedirectToAction(nameof(History));
        }

        // =========================
        // LỊCH SỬ GIAO DỊCH CỦA HỌC VIÊN
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Course)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.TransactionId)
                .ToListAsync();

            return View(transactions);
        }

        // =========================
        // DANH SÁCH GIAO DỊCH (ADMIN)
        // =========================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index(string? status)
        {
            var all = _context.Transactions.AsNoTracking();

            var model = new TransactionListViewModel
            {
                Status = status,

                TotalRevenue = await all
                    .Where(t => t.Status == "Success")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0,

                SuccessCount = await all.CountAsync(t => t.Status == "Success"),
                PendingCount = await all.CountAsync(t => t.Status == "Pending"),
                FailedCount = await all.CountAsync(t => t.Status == "Failed")
            };

            var query = all
                .Include(t => t.User)
                .Include(t => t.Course)
                .AsQueryable();

            if (status == "Pending" || status == "Success" || status == "Failed")
                query = query.Where(t => t.Status == status);
            else
                model.Status = null;

            model.Transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.TransactionId)
                .ToListAsync();

            return View(model);
        }

        // =========================
        // HÀM HỖ TRỢ
        // =========================

        // Trả về kết quả chặn nếu học viên không được mua khóa học
        private async Task<IActionResult?> CheckCanBuyAsync(
            Course? course, int userId)
        {
            // Chỉ được mua khóa học đã được Admin duyệt
            if (course == null || course.Status != "Published")
                return NotFound();

            if (course.Price <= 0)
            {
                TempData["Error"] =
                    "Khóa học miễn phí, bạn có thể đăng ký trực tiếp.";

                return RedirectToAction(
                    "Details", "Courses", new { id = course.CourseId });
            }

            bool enrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == course.CourseId);

            if (enrolled)
            {
                TempData["Error"] = "Bạn đã sở hữu khóa học này.";

                return RedirectToAction("MyCourses", "Enrollments");
            }

            return null;
        }

        private static CheckoutViewModel ToCheckoutModel(
            Course course, string paymentMethod)
        {
            return new CheckoutViewModel
            {
                CourseId = course.CourseId,
                CourseTitle = course.Title,
                CourseDescription = course.Description,
                InstructorName = course.Instructor?.FullName ?? "Chưa cập nhật",
                Price = course.Price,
                PaymentMethod = paymentMethod
            };
        }

        // Mã giao dịch: NTU + thời gian + chuỗi ngẫu nhiên
        private static string GenerateTransactionCode()
        {
            return "NTU" +
                DateTime.Now.ToString("yyyyMMddHHmmss") +
                RandomNumberGenerator.GetHexString(6);
        }
    }
}
