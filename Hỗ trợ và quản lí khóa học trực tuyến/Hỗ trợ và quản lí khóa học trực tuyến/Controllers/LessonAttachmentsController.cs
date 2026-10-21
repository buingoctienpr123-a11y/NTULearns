using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize]
    public class LessonAttachmentsController : Controller
    {
        private readonly NTULearnContext _context;
        private readonly LessonFileStorage _storage;

        public LessonAttachmentsController(
            NTULearnContext context,
            LessonFileStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        // =========================
        // TẢI TÀI LIỆU LÊN
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(LessonFileStorage.MaxFileSize + 1024 * 1024)]
        [RequestFormLimits(
            MultipartBodyLengthLimit = LessonFileStorage.MaxFileSize + 1024 * 1024)]
        public async Task<IActionResult> Upload(int lessonId, IFormFile? file)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null || lesson.Course == null)
                return NotFound();

            if (!CanManageCourse(lesson.Course))
                return Forbid();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file tài liệu.";
                return RedirectToLesson(lessonId);
            }

            if (file.Length > LessonFileStorage.MaxFileSize)
            {
                TempData["Error"] = "File vượt quá dung lượng tối đa 20MB.";
                return RedirectToLesson(lessonId);
            }

            var originalName = Path.GetFileName(file.FileName);

            if (!LessonFileStorage.IsAllowedExtension(originalName))
            {
                TempData["Error"] =
                    "Định dạng không được hỗ trợ. Chỉ chấp nhận: pdf, doc, docx, ppt, pptx, xls, xlsx, zip, txt.";
                return RedirectToLesson(lessonId);
            }

            if (originalName.Length > 255)
            {
                TempData["Error"] = "Tên file quá dài (tối đa 255 ký tự).";
                return RedirectToLesson(lessonId);
            }

            var storedName = await _storage.SaveAsync(file);

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(originalName, out var contentType))
                contentType = "application/octet-stream";

            _context.LessonAttachments.Add(new LessonAttachment
            {
                LessonId = lessonId,
                OriginalFileName = originalName,
                StoredFileName = storedName,
                ContentType = contentType,
                FileSize = file.Length,
                UploadedAt = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Không để lại file rác nếu lưu DB thất bại
                _storage.Delete(storedName);
                throw;
            }

            TempData["Success"] = "Tải tài liệu lên thành công!";

            return RedirectToLesson(lessonId);
        }

        // =========================
        // XÓA TÀI LIỆU
        // =========================

        [Authorize(Roles = "Admin,GiangVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var attachment = await _context.LessonAttachments
                .Include(a => a.Lesson)
                    .ThenInclude(l => l!.Course)
                .FirstOrDefaultAsync(a => a.LessonAttachmentId == id);

            if (attachment?.Lesson?.Course == null)
                return NotFound();

            if (!CanManageCourse(attachment.Lesson.Course))
                return Forbid();

            int lessonId = attachment.LessonId;

            _context.LessonAttachments.Remove(attachment);

            await _context.SaveChangesAsync();

            _storage.Delete(attachment.StoredFileName);

            TempData["Success"] = "Đã xóa tài liệu.";

            return RedirectToLesson(lessonId);
        }

        // =========================
        // TẢI TÀI LIỆU VỀ
        // =========================

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var attachment = await _context.LessonAttachments
                .AsNoTracking()
                .Include(a => a.Lesson)
                    .ThenInclude(l => l!.Course)
                .FirstOrDefaultAsync(a => a.LessonAttachmentId == id);

            if (attachment?.Lesson?.Course == null)
                return NotFound();

            // Admin / giảng viên của khóa được tải để kiểm tra.
            // Học viên phải ghi danh (Active) vào khóa học.
            bool allowed = CanManageCourse(attachment.Lesson.Course);

            if (!allowed && User.IsInRole("HocVien"))
            {
                var userId = CurrentUserId();

                allowed = userId != null &&
                    await _context.Enrollments.AnyAsync(e =>
                        e.UserId == userId &&
                        e.CourseId == attachment.Lesson.CourseId &&
                        e.Status == "Active");
            }

            if (!allowed)
                return Forbid();

            var path = _storage.GetPath(attachment.StoredFileName);

            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(
                path,
                attachment.ContentType,
                attachment.OriginalFileName);
        }

        // =========================
        // HÀM HỖ TRỢ
        // =========================

        private IActionResult RedirectToLesson(int lessonId)
        {
            return RedirectToAction(
                "Details", "Lessons", new { id = lessonId });
        }

        private int? CurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id : null;
        }

        private bool CanManageCourse(Course course)
        {
            if (User.IsInRole("Admin"))
                return true;

            if (!User.IsInRole("GiangVien"))
                return false;

            return course.InstructorId == CurrentUserId();
        }
    }
}
