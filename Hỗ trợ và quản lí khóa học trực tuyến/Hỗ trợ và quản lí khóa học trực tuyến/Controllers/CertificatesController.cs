using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize]
    public class CertificatesController : Controller
    {
        private readonly NTULearnContext _context;
        private readonly CertificateService _certificateService;

        public CertificatesController(
            NTULearnContext context,
            CertificateService certificateService)
        {
            _context = context;
            _certificateService = certificateService;
        }

        private int? CurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id : null;
        }

        // =========================
        // CHỨNG CHỈ CỦA TÔI
        // =========================

        [Authorize(Roles = "HocVien")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            // Cấp bù cho khóa đã đủ điều kiện
            // (ví dụ hoàn thành trước khi có chức năng chứng chỉ)
            await _certificateService.IssuePendingForUserAsync(userId.Value);

            var certificates = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.Course)
                    .ThenInclude(c => c!.Instructor)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IssuedAt)
                .ToListAsync();

            return View(certificates);
        }

        // =========================
        // XEM / IN CHỨNG CHỈ
        // =========================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var certificate = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Course)
                    .ThenInclude(c => c!.Instructor)
                .FirstOrDefaultAsync(c => c.CertificateId == id);

            if (certificate?.Course == null)
                return NotFound();

            // Học viên sở hữu, Admin hoặc giảng viên của khóa được xem
            bool allowed =
                certificate.UserId == userId ||
                User.IsInRole("Admin") ||
                (User.IsInRole("GiangVien") &&
                 certificate.Course.InstructorId == userId);

            if (!allowed)
                return Forbid();

            return View(certificate);
        }

        // =========================
        // XÁC MINH CHỨNG CHỈ (CÔNG KHAI)
        // =========================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Verify(string? code)
        {
            code = code?.Trim().ToUpperInvariant();

            ViewBag.Code = code;

            if (string.IsNullOrEmpty(code))
                return View(null);

            var certificate = await _context.Certificates
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Course)
                    .ThenInclude(c => c!.Instructor)
                .FirstOrDefaultAsync(c => c.CertificateCode == code);

            return View(certificate);
        }
    }
}
