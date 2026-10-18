
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "Admin,GiangVien")]
    public class QuizStatisticsController : Controller
    {
        private readonly NTULearnContext _context;

        public QuizStatisticsController(
            NTULearnContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index(int quizId)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q =>
                    q.QuizId == quizId);

            if (quiz == null)
                return NotFound();

            // Giảng viên chỉ được xem Quiz
            // thuộc khóa học mình phụ trách.
            if (User.IsInRole("GiangVien") &&
                !User.IsInRole("Admin"))
            {
                var userIdString = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(
                    userIdString, out int userId))
                {
                    return Challenge();
                }

                if (quiz.Course?.InstructorId != userId)
                    return Forbid();
            }

            // Chỉ thống kê các lượt đã nộp
            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e!.User)
                .Where(a =>
                    a.QuizId == quizId &&
                    a.SubmittedAt != null)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            int totalAttempts = attempts.Count;

            int totalStudents = attempts
                .Where(a => a.Enrollment != null)
                .Select(a => a.Enrollment!.UserId)
                .Distinct()
                .Count();

            int passedAttempts = attempts
                .Count(a => a.IsPassed);

            int failedAttempts =
                totalAttempts - passedAttempts;

            // Score là nullable trong QuizAttempt
            double averageScore = totalAttempts == 0
                ? 0
                : attempts.Average(a => a.Score ?? 0);

            double highestScore = totalAttempts == 0
                ? 0
                : attempts.Max(a => a.Score ?? 0);

            double lowestScore = totalAttempts == 0
                ? 0
                : attempts.Min(a => a.Score ?? 0);

            double passRate = totalAttempts == 0
                ? 0
                : passedAttempts * 100.0 / totalAttempts;

            var model = new QuizStatisticsViewModel
            {
                QuizId = quiz.QuizId,
                QuizTitle = quiz.Title,
                CourseId = quiz.CourseId,
                CourseTitle = quiz.Course?.Title ?? "",

                TotalAttempts = totalAttempts,
                TotalStudents = totalStudents,

                AverageScore = Math.Round(
                    averageScore, 2),

                HighestScore = highestScore,
                LowestScore = lowestScore,

                PassedAttempts = passedAttempts,
                FailedAttempts = failedAttempts,

                PassRate = Math.Round(passRate, 2),

                Attempts = attempts.Select(a =>
                    new QuizAttemptStatisticsItem
                    {
                        QuizAttemptId = a.QuizAttemptId,

                        StudentName =
                            a.Enrollment?.User?.FullName
                            ?? "Không xác định",

                        SubmittedAt = a.SubmittedAt,

                        Score = a.Score ?? 0,

                        IsPassed = a.IsPassed
                    }).ToList()
            };

            return View(model);
        }
    }
}
