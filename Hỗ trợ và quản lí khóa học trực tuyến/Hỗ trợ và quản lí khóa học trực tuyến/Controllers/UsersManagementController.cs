
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersManagementController : Controller
    {
        private readonly NTULearnContext _context;

        public UsersManagementController(
            NTULearnContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? keyword,
            int? roleId,
            bool? isActive,
            int page = 1)
        {
            const int pageSize = 10;

            page = Math.Max(1, page);

            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm theo tên hoặc email
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(u =>
                    u.FullName.Contains(keyword) ||
                    u.Email.Contains(keyword));
            }

            // Lọc theo vai trò
            if (roleId.HasValue)
            {
                query = query.Where(u =>
                    u.RoleId == roleId.Value);
            }

            // Lọc theo trạng thái
            if (isActive.HasValue)
            {
                query = query.Where(u =>
                    u.IsActive == isActive.Value);
            }

            int totalUsers = await query.CountAsync();

            int totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    totalUsers / (double)pageSize));

            page = Math.Min(page, totalPages);

            var users = await query
                .OrderBy(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserManagementViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleId = u.RoleId,

                    RoleName = u.Role != null
                        ? u.Role.RoleName
                        : "Chưa có vai trò",

                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .Select(r => new UserRoleOptionViewModel
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                })
                .ToListAsync();

            var model = new UserManagementIndexViewModel
            {
                Keyword = keyword ?? "",
                RoleId = roleId,
                IsActive = isActive,

                Page = page,
                TotalPages = totalPages,
                TotalUsers = totalUsers,

                Users = users,
                Roles = roles
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Lấy ID của Admin đang đăng nhập
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserId, out int adminId))
            {
                return Challenge();
            }

            // Không cho phép Admin tự khóa tài khoản
            if (user.UserId == adminId && user.IsActive)
            {
                TempData["Error"] =
                    "Bạn không thể tự khóa tài khoản đang đăng nhập.";

                return RedirectToAction(nameof(Index));
            }

            // Đảo trạng thái tài khoản
            user.IsActive = !user.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive
                ? "Mở khóa tài khoản thành công."
                : "Khóa tài khoản thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();

            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r =>
                    r.RoleName == "Admin" ||
                    r.RoleName == "GiangVien" ||
                    r.RoleName == "HocVien")
                .OrderBy(r => r.RoleId)
                .Select(r => new UserRoleOptionViewModel
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                })
                .ToListAsync();

            var model = new ChangeRoleViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                CurrentRoleName = user.Role?.RoleName
                    ?? "Chưa xác định",
                NewRoleId = user.RoleId,
                Roles = roles
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            ChangeRoleViewModel model)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == model.UserId);

            if (user == null)
                return NotFound();

            var newRole = model.NewRoleId.HasValue
                ? await _context.Roles
                    .FirstOrDefaultAsync(r =>
                        r.RoleId == model.NewRoleId.Value &&
                        (r.RoleName == "Admin" ||
                         r.RoleName == "GiangVien" ||
                         r.RoleName == "HocVien"))
                : null;

            if (newRole == null)
            {
                ModelState.AddModelError(
                    nameof(model.NewRoleId),
                    "Vai trò được chọn không hợp lệ.");
            }

            var currentUserIdString =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(currentUserIdString, out int adminId))
                return Challenge();

            // Không cho Admin tự thay đổi vai trò
            if (user.UserId == adminId &&
                newRole != null &&
                user.RoleId != newRole.RoleId)
            {
                ModelState.AddModelError(
                    nameof(model.NewRoleId),
                    "Bạn không thể tự thay đổi vai trò của mình.");
            }

            // Giảng viên đang quản lý khóa học
            // không được chuyển sang Học viên
            if (user.Role?.RoleName == "GiangVien" &&
                newRole?.RoleName == "HocVien")
            {
                bool hasCourses = await _context.Courses
                    .AnyAsync(c => c.InstructorId == user.UserId);

                if (hasCourses)
                {
                    ModelState.AddModelError(
                        nameof(model.NewRoleId),
                        "Giảng viên đang phụ trách khóa học. " +
                        "Hãy chuyển giao khóa học trước khi đổi vai trò.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.FullName = user.FullName;
                model.Email = user.Email;
                model.CurrentRoleName =
                    user.Role?.RoleName ?? "Chưa xác định";

                model.Roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r =>
                        r.RoleName == "Admin" ||
                        r.RoleName == "GiangVien" ||
                        r.RoleName == "HocVien")
                    .OrderBy(r => r.RoleId)
                    .Select(r => new UserRoleOptionViewModel
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName
                    })
                    .ToListAsync();

                return View(model);
            }

            if (user.RoleId == newRole!.RoleId)
            {
                TempData["Success"] =
                    "Tài khoản đã có vai trò này.";

                return RedirectToAction(nameof(Index));
            }

            user.RoleId = newRole.RoleId;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thay đổi vai trò tài khoản thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // 1. Lấy thông tin người dùng
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();

            var model = new UserDetailsViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "Chưa xác định",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            // 2. Lấy các khóa học đã đăng ký
            model.Enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == id)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new UserEnrollmentDetailViewModel
                {
                    CourseId = e.CourseId,

                    CourseTitle = e.Course != null
                        ? e.Course.Title
                        : "Không xác định",

                    EnrolledAt = e.EnrolledAt,
                    Status = e.Status,

                    TotalLessons = _context.Lessons
                        .Count(l => l.CourseId == e.CourseId),

                    CompletedLessons = _context.LessonProgresses
                        .Count(p =>
                            p.EnrollmentId == e.EnrollmentId &&
                            p.IsCompleted)
                })
                .ToListAsync();

            model.TotalEnrollments = model.Enrollments.Count;

            model.TotalCompletedLessons =
                model.Enrollments.Sum(e => e.CompletedLessons);

            // 3. Lấy lịch sử làm Quiz
            model.QuizHistory = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => a.Enrollment.UserId == id)
                .OrderByDescending(a => a.StartedAt)
                .Select(a => new UserQuizHistoryViewModel
                {
                    QuizAttemptId = a.QuizAttemptId,

                    QuizTitle = a.Quiz.Title,

                    CourseTitle = a.Enrollment.Course != null
                        ? a.Enrollment.Course.Title
                        : "Không xác định",

                    StartedAt = a.StartedAt,
                    SubmittedAt = a.SubmittedAt,
                    Score = a.Score,
                    IsPassed = a.IsPassed
                })
                .ToListAsync();

            model.TotalQuizAttempts =
                model.QuizHistory.Count(a => a.SubmittedAt != null);

            return View(model);
        }

    }
}
