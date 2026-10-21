using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    // Admin duyệt tài khoản Giảng viên và duyệt khóa học
    [Authorize(Roles = "Admin")]
    public class ApprovalsController : Controller
    {
        private readonly NTULearnContext _context;

        public ApprovalsController(NTULearnContext context)
        {
            _context = context;
        }

        // GET: /Approvals/Instructors
        public async Task<IActionResult> Instructors()
        {
            var instructors = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "GiangVien" &&
                            u.ApprovalStatus == "Pending")
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            return View(instructors);
        }

        // POST: /Approvals/ReviewInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewInstructor(int id, bool approve)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id &&
                                          u.Role.RoleName == "GiangVien");

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return RedirectToAction(nameof(Instructors));
            }

            if (user.ApprovalStatus != "Pending")
            {
                TempData["Error"] = $"Tài khoản {user.FullName} không còn ở trạng thái chờ duyệt.";
                return RedirectToAction(nameof(Instructors));
            }

            user.ApprovalStatus = approve ? "Approved" : "Rejected";
            await _context.SaveChangesAsync();

            TempData["Success"] = approve
                ? $"Đã duyệt giảng viên {user.FullName}."
                : $"Đã từ chối giảng viên {user.FullName}.";

            return RedirectToAction(nameof(Instructors));
        }

        // GET: /Approvals/Courses
        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return View(courses);
        }

        // POST: /Approvals/ReviewCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewCourse(int id, bool approve, string? reason)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return RedirectToAction(nameof(Courses));
            }

            if (course.Status != "Pending")
            {
                TempData["Error"] = $"Khóa học {course.Title} không còn ở trạng thái chờ duyệt.";
                return RedirectToAction(nameof(Courses));
            }

            if (approve)
            {
                course.Status = "Published";
                course.RejectionReason = null;
            }
            else
            {
                reason = reason?.Trim();

                if (string.IsNullOrEmpty(reason))
                {
                    TempData["Error"] = "Vui lòng nhập lý do khi từ chối khóa học.";
                    return RedirectToAction(nameof(Courses));
                }

                course.Status = "Rejected";
                course.RejectionReason = reason.Length > 255 ? reason[..255] : reason;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = approve
                ? $"Đã duyệt khóa học {course.Title}."
                : $"Đã từ chối khóa học {course.Title}.";

            return RedirectToAction(nameof(Courses));
        }
    }
}
