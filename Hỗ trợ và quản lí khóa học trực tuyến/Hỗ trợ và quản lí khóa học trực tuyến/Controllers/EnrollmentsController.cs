
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "HocVien")]
    public class EnrollmentsController : Controller
    {
        private readonly NTULearnContext _context;

        public EnrollmentsController(NTULearnContext context)
        {
            _context = context;
        }

        // Đăng ký khóa học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Challenge();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            // Chỉ được đăng ký khóa học đã được Admin duyệt
            if (course == null || course.Status != "Published")
                return NotFound();

            // Khóa học trả phí phải thanh toán trước,
            // Enrollment được tạo khi giao dịch thành công.
            if (course.Price > 0)
            {
                return RedirectToAction(
                    "Checkout", "Transactions", new { courseId });
            }

            bool exists = await _context.Enrollments
                .AnyAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == courseId);

            if (exists)
            {
                TempData["Error"] =
                    "Bạn đã đăng ký khóa học này.";

                return RedirectToAction(nameof(MyCourses));
            }

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = courseId,
                EnrolledAt = DateTime.Now,
                Status = "Active"
            };

            _context.Enrollments.Add(enrollment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Unique Index ngăn đăng ký trùng
                // khi có hai yêu cầu đồng thời.
                bool registered = await _context.Enrollments
                    .AsNoTracking()
                    .AnyAsync(e =>
                        e.UserId == userId &&
                        e.CourseId == courseId);

                if (!registered)
                    throw;

                TempData["Error"] =
                    "Bạn đã đăng ký khóa học này.";

                return RedirectToAction(nameof(MyCourses));
            }

            TempData["Success"] =
                "Đăng ký khóa học thành công!";

            return RedirectToAction(nameof(MyCourses));
        }

        // Danh sách khóa học đã đăng ký

        [HttpGet]
        public async Task<IActionResult> MyCourses()
        {
            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Challenge();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();

            // Lấy số lượng bài học theo từng khóa học
            var lessonCounts = await _context.Lessons
                .GroupBy(l => l.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    Total = g.Count()
                })
                .ToDictionaryAsync(x => x.CourseId, x => x.Total);

            // Lấy số bài học đã hoàn thành của từng Enrollment
            var completedCounts = await _context.LessonProgresses
                .Where(p =>
                    p.IsCompleted &&
                    p.Enrollment != null &&
                    p.Lesson != null &&
                    p.Enrollment.CourseId == p.Lesson.CourseId)
                .GroupBy(p => p.EnrollmentId)
                .Select(g => new
                {
                    EnrollmentId = g.Key,
                    Completed = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.EnrollmentId,
                    x => x.Completed);

            // Tính phần trăm tiến độ
            var progressPercent = new Dictionary<int, double>();

            foreach (var enrollment in enrollments)
            {
                int total = lessonCounts.GetValueOrDefault(
                    enrollment.CourseId, 0);

                int completed = completedCounts.GetValueOrDefault(
                    enrollment.EnrollmentId, 0);

                double percent = total == 0
                    ? 0
                    : Math.Min(100, completed * 100.0 / total);

                progressPercent[enrollment.EnrollmentId] = percent;
            }

            ViewBag.ProgressPercent = progressPercent;

            return View(enrollments);
        }

    }
}
