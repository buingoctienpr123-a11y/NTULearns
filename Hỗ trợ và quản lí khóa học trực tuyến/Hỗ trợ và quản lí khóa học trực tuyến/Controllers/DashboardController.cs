
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "Admin,GiangVien")]
    public class DashboardController : Controller
    {
        private readonly NTULearnContext _context;

        public DashboardController(NTULearnContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            var userIdString = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Challenge();

            bool isAdmin = User.IsInRole("Admin");

            // Admin xem toàn bộ khóa học.
            // Giảng viên chỉ xem khóa học của mình.
            var courses = _context.Courses
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                courses = courses.Where(c =>
                    c.InstructorId == userId);
            }

            var courseIds = courses.Select(c => c.CourseId);

            model.TotalCourses =
                await courses.CountAsync();

            model.TotalStudents = await _context.Enrollments
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            model.TotalInstructors = isAdmin
                ? await _context.Users
                    .AsNoTracking()
                    .CountAsync(u => u.Role != null &&
                                     u.Role.RoleName == "GiangVien")
                : 1;

            var enrollments = _context.Enrollments
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId));

            model.TotalEnrollments =
                await enrollments.CountAsync();

            model.TotalLessons = await _context.Lessons
                .AsNoTracking()
                .CountAsync(l =>
                    courseIds.Contains(l.CourseId));

            var quizzes = _context.Quizzes
                .AsNoTracking()
                .Where(q =>
                    courseIds.Contains(q.CourseId));

            model.TotalQuizzes =
                await quizzes.CountAsync();

            var quizIds = quizzes.Select(q => q.QuizId);

            var attempts = _context.QuizAttempts
                .AsNoTracking()
                .Where(a =>
                    quizIds.Contains(a.QuizId) &&
                    a.SubmittedAt != null);

            model.TotalQuizAttempts =
                await attempts.CountAsync();

            model.AverageQuizScore =
                Math.Round(
                    await attempts.AverageAsync(
                        a => (double?)a.Score) ?? 0,
                    2);

            var currentYear = DateTime.Now.Year;

            model.MonthlyEnrollments = await enrollments
                .Where(e => e.EnrolledAt.Year == currentYear)
                .GroupBy(e => e.EnrolledAt.Month)
                .Select(g => new MonthlyEnrollmentItem
                {
                    Month = g.Key,
                    Total = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            model.PassedQuizAttempts = await attempts
                .CountAsync(a => a.IsPassed);

            model.FailedQuizAttempts =
                model.TotalQuizAttempts - model.PassedQuizAttempts;

            model.TopCourses = await courses
                .Select(c => new TopCourseItem
                {
                    CourseId = c.CourseId,
                    CourseTitle = c.Title,

                    EnrollmentCount = _context.Enrollments
                        .Count(e => e.CourseId == c.CourseId)
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ThenBy(c => c.CourseTitle)
                .Take(5)
                .ToListAsync();

            model.RecentEnrollments = await enrollments
                .OrderByDescending(e => e.EnrolledAt)
                .ThenByDescending(e => e.EnrollmentId)
                .Take(5)
                .Select(e => new RecentEnrollmentItem
                {
                    EnrollmentId = e.EnrollmentId,

                    StudentName = e.User != null
                        ? e.User.FullName
                        : "Không xác định",

                    CourseTitle = e.Course != null
                        ? e.Course.Title
                        : "Không xác định",

                    EnrolledAt = e.EnrolledAt
                })
                .ToListAsync();
            


            return View(model);
        }
    }
}
