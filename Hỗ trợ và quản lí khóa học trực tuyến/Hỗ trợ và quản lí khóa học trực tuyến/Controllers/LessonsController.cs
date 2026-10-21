using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    public class LessonsController : Controller
    {
        private readonly NTULearnContext _context;
        private readonly LessonFileStorage _storage;

        public LessonsController(
            NTULearnContext context,
            LessonFileStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        // =========================
        // DANH SÁCH BÀI HỌC
        // =========================

        [AllowAnonymous]
        public async Task<IActionResult> Index(int courseId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound();
            }

            var lessons = await _context.Lessons
                .Include(l => l.Attachments)
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            ViewBag.Course = course;

            return View(lessons);
        }


        // =========================
        // XEM CHI TIẾT BÀI HỌC
        // =========================

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Attachments)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
            {
                return NotFound();
            }

            ViewBag.CanManage = lesson.Course != null &&
                User.Identity?.IsAuthenticated == true &&
                (User.IsInRole("Admin") || User.IsInRole("GiangVien")) &&
                CanManageCourse(lesson.Course);

            return View(lesson);
        }


        // =========================
        // TẠO BÀI HỌC - GET
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> Create(int courseId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound();
            }

            if (!CanManageCourse(course))
            {
                return Forbid();
            }

            ViewBag.Course = course;

            return View();
        }


        // =========================
        // TẠO BÀI HỌC - POST
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int courseId,
            [Bind("Title,Content,VideoUrl,OrderIndex")]
            Lesson lesson)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound();
            }

            if (!CanManageCourse(course))
            {
                return Forbid();
            }

            lesson.CourseId = courseId;
            lesson.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                ViewBag.Course = course;
                return View(lesson);
            }

            _context.Lessons.Add(lesson);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { courseId });
        }


        // =========================
        // SỬA BÀI HỌC - GET
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
            {
                return NotFound();
            }

            if (!CanManageCourse(lesson.Course))
            {
                return Forbid();
            }

            return View(lesson);
        }


        // =========================
        // SỬA BÀI HỌC - POST
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("LessonId,Title,Content,VideoUrl,OrderIndex")]
            Lesson lesson)
        {
            if (id != lesson.LessonId)
            {
                return NotFound();
            }

            var existingLesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (existingLesson == null)
            {
                return NotFound();
            }

            if (!CanManageCourse(existingLesson.Course))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                lesson.Course = existingLesson.Course;
                return View(lesson);
            }

            existingLesson.Title = lesson.Title;
            existingLesson.Content = lesson.Content;
            existingLesson.VideoUrl = lesson.VideoUrl;
            existingLesson.OrderIndex = lesson.OrderIndex;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { courseId = existingLesson.CourseId });
        }


        // =========================
        // XÓA BÀI HỌC - GET
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
            {
                return NotFound();
            }

            if (!CanManageCourse(lesson.Course))
            {
                return Forbid();
            }

            return View(lesson);
        }


        // =========================
        // XÓA BÀI HỌC - POST
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Attachments)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
            {
                return NotFound();
            }

            if (!CanManageCourse(lesson.Course))
            {
                return Forbid();
            }

            int courseId = lesson.CourseId;

            var storedFiles = lesson.Attachments
                .Select(a => a.StoredFileName)
                .ToList();

            _context.Lessons.Remove(lesson);

            await _context.SaveChangesAsync();

            // Tài liệu trong DB bị xóa theo (Cascade),
            // xóa luôn file vật lý.
            foreach (var storedFile in storedFiles)
            {
                _storage.Delete(storedFile);
            }

            return RedirectToAction(
                nameof(Index),
                new { courseId });
        }


        // =========================
        // KIỂM TRA QUYỀN
        // =========================

        private bool CanManageCourse(Course course)
        {
            // Admin được quản lý tất cả khóa học
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            // Lấy UserId của người đang đăng nhập
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int instructorId))
            {
                return false;
            }

            // Giảng viên chỉ quản lý khóa học của mình
            return course.InstructorId == instructorId;
        }
    }
}