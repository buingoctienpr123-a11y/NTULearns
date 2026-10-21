
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "HocVien")]
    public class LearningController : Controller
    {
        private readonly NTULearnContext _context;

        public LearningController(NTULearnContext context)
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

        // Danh sách bài học của khóa đã đăng ký
        [HttpGet]
        public async Task<IActionResult> Index(int courseId)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == courseId &&
                    e.Status == "Active");

            if (enrollment == null)
                return Forbid();

            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            var completedIds = await _context.LessonProgresses
                .Where(p =>
                    p.EnrollmentId == enrollment.EnrollmentId &&
                    p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync();

            var completedSet = completedIds.ToHashSet();

            int completedCount = lessons.Count(
                l => completedSet.Contains(l.LessonId));

            double percent = lessons.Count == 0
                ? 0
                : completedCount * 100.0 / lessons.Count;

            ViewBag.Course = enrollment.Course;
            ViewBag.CompletedIds = completedSet;
            ViewBag.ProgressPercent = percent;

            return View(lessons);
        }

        // Xem nội dung bài học
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Attachments)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
                return NotFound();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == lesson.CourseId &&
                    e.Status == "Active");

            if (enrollment == null)
                return Forbid();

            bool isCompleted = await _context.LessonProgresses
                .AnyAsync(p =>
                    p.EnrollmentId == enrollment.EnrollmentId &&
                    p.LessonId == lesson.LessonId &&
                    p.IsCompleted);

            ViewBag.IsCompleted = isCompleted;

            return View(lesson);
        }

        // Đánh dấu hoàn thành bài học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int lessonId)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                return NotFound();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == lesson.CourseId &&
                    e.Status == "Active");

            if (enrollment == null)
                return Forbid();

            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(p =>
                    p.EnrollmentId == enrollment.EnrollmentId &&
                    p.LessonId == lessonId);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    LessonId = lessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.Now
                };

                _context.LessonProgresses.Add(progress);
            }
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã hoàn thành bài học!";

            return RedirectToAction(
                nameof(Index),
                new { courseId = lesson.CourseId });
        }
    }
}
