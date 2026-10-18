
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "Admin,GiangVien")]
    public class QuizzesController : Controller
    {
        private readonly NTULearnContext _context;

        public QuizzesController(NTULearnContext context)
        {
            _context = context;
        }

        private bool CanManageCourse(Course course)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(userId, out int id)
                && course.InstructorId == id;
        }

        // Danh sách Quiz theo khóa học
        [HttpGet]
        public async Task<IActionResult> Index(int courseId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
                return NotFound();

            if (!CanManageCourse(course))
                return Forbid();

            var quizzes = await _context.Quizzes
                .Where(q => q.CourseId == courseId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            ViewBag.Course = course;

            return View(quizzes);
        }

        // Giao diện tạo Quiz
        [HttpGet]
        public async Task<IActionResult> Create(int courseId)
        {
            var course = await _context.Courses
                .FindAsync(courseId);

            if (course == null)
                return NotFound();

            if (!CanManageCourse(course))
                return Forbid();

            ViewBag.Course = course;

            return View(new Quiz
            {
                CourseId = courseId,
                DurationMinutes = 30,
                PassingScore = 5
            });
        }

        // Lưu Quiz mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CourseId,Title,Description,DurationMinutes,PassingScore")]
            Quiz quiz)
        {
            var course = await _context.Courses
                .FindAsync(quiz.CourseId);

            if (course == null)
                return NotFound();

            if (!CanManageCourse(course))
                return Forbid();

            if (quiz.DurationMinutes <= 0)
                ModelState.AddModelError(
                    "DurationMinutes",
                    "Thời gian phải lớn hơn 0.");

            if (quiz.PassingScore < 0 || quiz.PassingScore > 10)
                ModelState.AddModelError(
                    "PassingScore",
                    "Điểm đạt phải từ 0 đến 10.");

            if (!ModelState.IsValid)
            {
                ViewBag.Course = course;
                return View(quiz);
            }

            quiz.CreatedAt = DateTime.Now;
            quiz.IsPublished = false;

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Tạo bài kiểm tra thành công!";

            return RedirectToAction(
                nameof(Index),
                new { courseId = quiz.CourseId });
        }

        // Giao diện chỉnh sửa Quiz
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            return View(quiz);
        }

        // Lưu chỉnh sửa Quiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("QuizId,Title,Description,DurationMinutes,PassingScore")]
            Quiz input)
        {
            if (id != input.QuizId)
                return BadRequest();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            if (input.DurationMinutes <= 0)
                ModelState.AddModelError(
                    "DurationMinutes",
                    "Thời gian phải lớn hơn 0.");

            if (input.PassingScore < 0 || input.PassingScore > 10)
                ModelState.AddModelError(
                    "PassingScore",
                    "Điểm đạt phải từ 0 đến 10.");

            // Chỉ kiểm tra các trường được phép chỉnh sửa
            ModelState.Remove(nameof(Quiz.CourseId));

            if (!ModelState.IsValid)
                return View(quiz);

            quiz.Title = input.Title;
            quiz.Description = input.Description;
            quiz.DurationMinutes = input.DurationMinutes;
            quiz.PassingScore = input.PassingScore;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật bài kiểm tra thành công!";

            return RedirectToAction(
                nameof(Index),
                new { courseId = quiz.CourseId });
        }

        // Xóa Quiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            bool hasQuestions = await _context.Questions
                .AnyAsync(q => q.QuizId == id);

            bool hasAttempts = await _context.QuizAttempts
                .AnyAsync(a => a.QuizId == id);

            if (hasQuestions || hasAttempts)
            {
                TempData["Error"] =
                    "Không thể xóa Quiz đã có câu hỏi hoặc lịch sử làm bài.";

                return RedirectToAction(
                    nameof(Index),
                    new { courseId = quiz.CourseId });
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã xóa bài kiểm tra.";

            return RedirectToAction(
                nameof(Index),
                new { courseId = quiz.CourseId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            if (quiz.IsPublished)
            {
                TempData["Error"] =
                    "Bài kiểm tra đã được công bố.";

                return RedirectToAction(
                    nameof(Index),
                    new { courseId = quiz.CourseId });
            }

            if (!quiz.Questions.Any())
            {
                TempData["Error"] =
                    "Cần có ít nhất một câu hỏi trước khi công bố.";

                return RedirectToAction(
                    nameof(Index),
                    new { courseId = quiz.CourseId });
            }

            bool invalidQuestions = quiz.Questions.Any(q =>
                q.AnswerOptions.Count != 4 ||
                q.AnswerOptions.Count(a => a.IsCorrect) != 1);

            if (invalidQuestions)
            {
                TempData["Error"] =
                    "Mỗi câu hỏi phải có đúng 4 đáp án và 1 đáp án đúng.";

                return RedirectToAction(
                    nameof(Index),
                    new { courseId = quiz.CourseId });
            }

            quiz.IsPublished = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Công bố bài kiểm tra thành công!";

            return RedirectToAction(
                nameof(Index),
                new { courseId = quiz.CourseId });
        }

    }
}
