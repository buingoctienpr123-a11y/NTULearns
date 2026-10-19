
using System.Diagnostics;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NTULearnContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            NTULearnContext context)
        {
            _logger = logger;
            _context = context;
        }

        // TRANG CHỦ
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            keyword = keyword?.Trim() ?? "";

            // Lấy danh sách khóa học
            var coursesQuery = _context.Courses
                .AsNoTracking()
                .Where(c => c.Status == "Published");

            // Tìm kiếm khóa học
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                coursesQuery = coursesQuery
                    .Where(c => c.Title.Contains(keyword));
            }

            // Thống kê trang chủ
            var model = new HomeViewModel
            {
                Keyword = keyword,

                TotalCourses = await _context.Courses
                    .CountAsync(c => c.Status == "Published"),

                TotalStudents = await _context.Users
                    .CountAsync(u =>
                        u.Role != null &&
                        u.Role.RoleName == "HocVien"),

                TotalLessons = await _context.Lessons
                    .CountAsync()
            };

            // Lấy 6 khóa học nổi bật
            model.FeaturedCourses = await coursesQuery
                .Select(c => new FeaturedCourseViewModel
                {
                    CourseId = c.CourseId,

                    Title = c.Title,

                    Description = c.Description ?? "",

                    ThumbnailUrl = c.ThumbnailUrl,

                    Price = c.Price,

                    CategoryName = c.Category != null
                        ? c.Category.CategoryName
                        : "Chưa phân loại",

                    InstructorName = c.Instructor != null
                        ? c.Instructor.FullName
                        : "Chưa có giảng viên",

                    EnrollmentCount = _context.Enrollments
                        .Count(e => e.CourseId == c.CourseId)
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ThenBy(c => c.Title)
                .Take(6)
                .ToListAsync();

            return View(model);
        }

        // TRANG PRIVACY
        public IActionResult Privacy()
        {
            return View();
        }

        // TRANG ERROR
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
