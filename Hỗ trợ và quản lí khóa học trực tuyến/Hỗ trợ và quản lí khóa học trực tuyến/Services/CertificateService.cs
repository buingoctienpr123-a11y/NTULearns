using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services
{
    // Cấp chứng chỉ khi học viên hoàn thành khóa học:
    // - Hoàn thành 100% bài học của khóa (khóa phải có ít nhất 1 bài học)
    // - Đạt tất cả Quiz đã công khai của khóa (nếu có), tức là có ít nhất
    //   một lượt đã nộp với IsPassed = true (Score >= PassingScore,
    //   được chấm trong StudentQuizzesController.FinalizeAttemptAsync)
    public class CertificateService
    {
        // Bỏ các ký tự dễ nhầm (0/O, 1/I)
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly NTULearnContext _context;

        public CertificateService(NTULearnContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEligibleAsync(Enrollment enrollment)
        {
            if (enrollment.Status != "Active")
                return false;

            var lessonIds = _context.Lessons
                .Where(l => l.CourseId == enrollment.CourseId)
                .Select(l => l.LessonId);

            int totalLessons = await lessonIds.CountAsync();

            if (totalLessons == 0)
                return false;

            int completedLessons = await _context.LessonProgresses
                .Where(p =>
                    p.EnrollmentId == enrollment.EnrollmentId &&
                    p.IsCompleted &&
                    lessonIds.Contains(p.LessonId))
                .Select(p => p.LessonId)
                .Distinct()
                .CountAsync();

            if (completedLessons < totalLessons)
                return false;

            // Chỉ tính Quiz học viên làm được:
            // đã công khai và có câu hỏi.
            var quizIds = _context.Quizzes
                .Where(q =>
                    q.CourseId == enrollment.CourseId &&
                    q.IsPublished &&
                    q.Questions.Any())
                .Select(q => q.QuizId);

            int totalQuizzes = await quizIds.CountAsync();

            if (totalQuizzes == 0)
                return true;

            int passedQuizzes = await _context.QuizAttempts
                .Where(a =>
                    a.EnrollmentId == enrollment.EnrollmentId &&
                    a.SubmittedAt != null &&
                    a.IsPassed &&
                    quizIds.Contains(a.QuizId))
                .Select(a => a.QuizId)
                .Distinct()
                .CountAsync();

            return passedQuizzes >= totalQuizzes;
        }

        // Cấp chứng chỉ nếu đủ điều kiện và chưa được cấp.
        // Trả về true nếu vừa cấp chứng chỉ mới.
        public async Task<bool> TryIssueAsync(Enrollment enrollment)
        {
            bool issued = await _context.Certificates
                .AnyAsync(c =>
                    c.UserId == enrollment.UserId &&
                    c.CourseId == enrollment.CourseId);

            if (issued || !await IsEligibleAsync(enrollment))
                return false;

            var certificate = new Certificate
            {
                UserId = enrollment.UserId,
                CourseId = enrollment.CourseId,
                CertificateCode = GenerateCode(),
                IssuedAt = DateTime.Now
            };

            _context.Certificates.Add(certificate);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Unique Index (UserId, CourseId) ngăn cấp trùng
                // khi có hai yêu cầu đồng thời.
                _context.Entry(certificate).State = EntityState.Detached;

                return false;
            }

            return true;
        }

        // Cấp bù chứng chỉ cho các khóa học viên đã đủ điều kiện
        public async Task IssuePendingForUserAsync(int userId)
        {
            var enrollments = await _context.Enrollments
                .Where(e =>
                    e.UserId == userId &&
                    e.Status == "Active" &&
                    !_context.Certificates.Any(c =>
                        c.UserId == userId &&
                        c.CourseId == e.CourseId))
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                await TryIssueAsync(enrollment);
            }
        }

        // Mã chứng chỉ: NTU-năm-8 ký tự ngẫu nhiên
        private static string GenerateCode()
        {
            return "NTU-" +
                DateTime.Now.Year + "-" +
                RandomNumberGenerator.GetString(CodeAlphabet, 8);
        }
    }
}
