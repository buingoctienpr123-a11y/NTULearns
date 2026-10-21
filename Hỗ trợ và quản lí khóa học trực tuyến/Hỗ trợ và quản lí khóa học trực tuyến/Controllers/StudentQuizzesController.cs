
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "HocVien")]
    public class StudentQuizzesController : Controller
    {
        private readonly NTULearnContext _context;
        private readonly CertificateService _certificateService;

        public StudentQuizzesController(
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

        // Danh sách Quiz của khóa học
        [HttpGet]
        public async Task<IActionResult> Index(int courseId)
        {
            var userId = CurrentUserId();
            if (userId == null) return Challenge();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == courseId &&
                    e.Status == "Active");

            if (enrollment == null)
                return Forbid();

            var course = await _context.Courses
                .FindAsync(courseId);

            if (course == null)
                return NotFound();

            var quizzes = await _context.Quizzes
                .Where(q =>
                    q.CourseId == courseId &&
                    q.IsPublished)
                .OrderBy(q => q.QuizId)
                .ToListAsync();

            ViewBag.Course = course;

            return View(quizzes);
        }

        // Hiển thị bài kiểm tra

        [HttpGet]
        public async Task<IActionResult> Take(int id)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q =>
                    q.QuizId == id && q.IsPublished);

            if (quiz == null)
                return NotFound();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == quiz.CourseId &&
                    e.Status == "Active");

            if (enrollment == null)
                return Forbid();

            if (quiz.DurationMinutes <= 0 || !quiz.Questions.Any())
                return BadRequest("Bài kiểm tra chưa hợp lệ.");

            // Tìm lượt làm bài chưa nộp

            var attempt = await _context.QuizAttempts
                .Where(a =>
                    a.QuizId == quiz.QuizId &&
                    a.EnrollmentId == enrollment.EnrollmentId &&
                    a.SubmittedAt == null)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefaultAsync();

            // Nếu có lượt làm bài đang thực hiện
            if (attempt != null)
            {
                var expiredDeadlineUtc = DateTime.SpecifyKind(
                    attempt.StartedAt,
                    DateTimeKind.Utc
                ).AddMinutes(quiz.DurationMinutes);

                if (DateTime.UtcNow >= expiredDeadlineUtc)
                {
                    // Kết thúc và chấm bài đã hết hạn
                    await FinalizeAttemptAsync(
                        attempt,
                        expiredDeadlineUtc
                    );

                    TempData["Info"] =
                        "Bài kiểm tra đã hết thời gian và được chấm điểm.";

                    return RedirectToAction(
                        nameof(Result),
                        new { id = attempt.QuizAttemptId });
                }
            }
            else
            {
                // Chưa có lượt làm bài -> tạo lượt mới
                attempt = new QuizAttempt
                {
                    QuizId = quiz.QuizId,
                    EnrollmentId = enrollment.EnrollmentId,
                    StartedAt = DateTime.UtcNow
                };

                _context.QuizAttempts.Add(attempt);

                await _context.SaveChangesAsync();
            }

            // Đến đây attempt chắc chắn không null
            var deadlineUtc = DateTime.SpecifyKind(
                attempt.StartedAt,
                DateTimeKind.Utc
            ).AddMinutes(quiz.DurationMinutes);


            var model = new QuizTakeViewModel
            {
                QuizId = quiz.QuizId,
                QuizAttemptId = attempt.QuizAttemptId,
                Title = quiz.Title,
                DurationMinutes = quiz.DurationMinutes,
                DeadlineUtc = deadlineUtc,

                Questions = quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .ThenBy(q => q.QuestionId)
                    .Select(q => new QuizQuestionViewModel
                    {
                        QuestionId = q.QuestionId,
                        Content = q.Content,
                        OrderIndex = q.OrderIndex,

                        Options = q.AnswerOptions
                            .OrderBy(a => a.AnswerOptionId)
                            .Select(a => new QuizOptionViewModel
                            {
                                AnswerOptionId = a.AnswerOptionId,
                                Content = a.Content
                            })
                            .ToList()
                    })
                    .ToList()
            };


            // Lấy các câu trả lời đã được lưu trong SQL Server
            var savedAnswers = await _context.StudentAnswers
                .AsNoTracking()
                .Where(a =>
                    a.QuizAttemptId == attempt.QuizAttemptId)
                .ToDictionaryAsync(
                    a => a.QuestionId,
                    a => a.AnswerOptionId
                );

            // Khôi phục đáp án đã chọn vào ViewModel
            foreach (var question in model.Questions)
            {
                if (savedAnswers.TryGetValue(
                    question.QuestionId,
                    out int selectedOptionId))
                {
                    question.SelectedOptionId = selectedOptionId;
                }
            }


            return View(model);
        }


        // Nộp bài và chấm điểm


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(QuizTakeViewModel model)
        {
            var userId = CurrentUserId();
            if (userId == null)
                return Challenge();

            var attempt = await _context.QuizAttempts
                .Include(a => a.Enrollment)
                .Include(a => a.Quiz)
                .FirstOrDefaultAsync(a =>
                    a.QuizAttemptId == model.QuizAttemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.Enrollment?.UserId != userId ||
                attempt.Enrollment.Status != "Active")
                return Forbid();

            var quiz = attempt.Quiz;

            if (quiz == null ||
                quiz.QuizId != model.QuizId ||
                !quiz.IsPublished)
                return BadRequest("Quiz không hợp lệ.");

            if (attempt.SubmittedAt != null)
                return RedirectToAction(
                    nameof(Result),
                    new { id = attempt.QuizAttemptId });

            var deadlineUtc = DateTime.SpecifyKind(
                attempt.StartedAt,
                DateTimeKind.Utc
            ).AddMinutes(quiz.DurationMinutes);

            var nowUtc = DateTime.UtcNow;

            // Nếu đã hết hạn, chấm từ đáp án Auto Save
            if (nowUtc > deadlineUtc)
            {
                await FinalizeAttemptAsync(
                    attempt, deadlineUtc);
            }
            else
            {
                // Cập nhật các đáp án cuối cùng từ form
                var questions = await _context.Questions
                    .Include(q => q.AnswerOptions)
                    .Where(q => q.QuizId == quiz.QuizId)
                    .ToListAsync();

                var submitted = model.Questions
                    .GroupBy(q => q.QuestionId)
                    .ToList();

                if (submitted.Any(g => g.Count() > 1))
                    return BadRequest(
                        "Dữ liệu câu trả lời không hợp lệ.");

                var selected = submitted.ToDictionary(
                    g => g.Key,
                    g => g.First().SelectedOptionId);

                var validQuestionIds = questions
                    .Select(q => q.QuestionId)
                    .ToHashSet();

                if (selected.Keys.Any(id =>
                    !validQuestionIds.Contains(id)))
                    return BadRequest(
                        "Có câu hỏi không hợp lệ.");

                var savedAnswers = await _context.StudentAnswers
                    .Where(a =>
                        a.QuizAttemptId == attempt.QuizAttemptId)
                    .ToDictionaryAsync(a => a.QuestionId);

                foreach (var question in questions)
                {
                    if (!selected.TryGetValue(
                        question.QuestionId,
                        out int? optionId))
                    {
                        // Không gửi câu trả lời: giữ Auto Save cũ
                        continue;
                    }

                    if (optionId == null)
                        continue;

                    var option = question.AnswerOptions
                        .FirstOrDefault(a =>
                            a.AnswerOptionId == optionId.Value);

                    if (option == null)
                        return BadRequest(
                            "Đáp án không hợp lệ.");

                    if (savedAnswers.TryGetValue(
                        question.QuestionId,
                        out var existing))
                    {
                        existing.AnswerOptionId =
                            option.AnswerOptionId;

                        existing.IsCorrect = option.IsCorrect;
                    }
                    else
                    {
                        _context.StudentAnswers.Add(
                            new StudentAnswer
                            {
                                QuizAttemptId = attempt.QuizAttemptId,
                                QuestionId = question.QuestionId,
                                AnswerOptionId = option.AnswerOptionId,
                                IsCorrect = option.IsCorrect
                            });
                    }
                }

                // Lưu đáp án cuối cùng trước khi chấm
                await _context.SaveChangesAsync();

                await FinalizeAttemptAsync(
                    attempt, DateTime.UtcNow);
            }

            return RedirectToAction(
                nameof(Result),
                new { id = attempt.QuizAttemptId });
        }



        // Xem kết quả sau khi nộp
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Challenge();

            var attempt = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Quiz)
                .Include(a => a.Enrollment)
                .Include(a => a.StudentAnswers)
                .FirstOrDefaultAsync(a =>
                    a.QuizAttemptId == id);

            if (attempt == null)
                return NotFound();

            if (attempt.Enrollment?.UserId != userId)
                return Forbid();

            return View(attempt);
        }


        [HttpGet]
        public async Task<IActionResult> History(int quizId)
        {
            var userId = CurrentUserId();

            if (userId == null)
                return Challenge();

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                return NotFound();

            // Chỉ học viên đã đăng ký khóa học mới được xem
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.CourseId == quiz.CourseId);

            if (enrollment == null)
                return Forbid();

            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a =>
                    a.QuizId == quizId &&
                    a.EnrollmentId == enrollment.EnrollmentId &&
                    a.SubmittedAt != null)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            ViewBag.Quiz = quiz;

            return View(attempts);
        }


        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var userId = CurrentUserId();

            if (userId == null)
                return Challenge();

            var attempt = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Quiz)
                    .ThenInclude(q => q!.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                .Include(a => a.Enrollment)
                .Include(a => a.StudentAnswers)
                .FirstOrDefaultAsync(a =>
                    a.QuizAttemptId == id &&
                    a.SubmittedAt != null);

            if (attempt == null)
                return NotFound();

            if (attempt.Enrollment?.UserId != userId)
                return Forbid();

            return View(attempt);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoSave(
            [FromBody] AutoSaveAnswerViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Dữ liệu không hợp lệ.");

            var userId = CurrentUserId();

            if (userId == null)
                return Unauthorized();

            var attempt = await _context.QuizAttempts
                .Include(a => a.Enrollment)
                .Include(a => a.Quiz)
                .FirstOrDefaultAsync(a =>
                    a.QuizAttemptId == model.QuizAttemptId);

            if (attempt == null)
                return NotFound();

            // Chỉ chủ sở hữu lượt làm bài mới được lưu
            if (attempt.Enrollment?.UserId != userId ||
                attempt.Enrollment.Status != "Active")
                return Forbid();

            if (attempt.Quiz == null ||
                !attempt.Quiz.IsPublished)
                return BadRequest("Quiz không hợp lệ.");

            if (attempt.SubmittedAt != null)
                return Conflict("Lượt làm bài đã kết thúc.");

            // Kiểm tra thời hạn bằng giờ UTC
            var deadline = DateTime.SpecifyKind(
                attempt.StartedAt,
                DateTimeKind.Utc
            ).AddMinutes(attempt.Quiz.DurationMinutes);

            if (DateTime.UtcNow > deadline)
                return Conflict("Đã hết thời gian làm bài.");

            // Kiểm tra đáp án thuộc câu hỏi của Quiz
            var option = await _context.AnswerOptions
                .Include(o => o.Question)
                .FirstOrDefaultAsync(o =>
                    o.AnswerOptionId == model.AnswerOptionId &&
                    o.QuestionId == model.QuestionId &&
                    o.Question != null &&
                    o.Question.QuizId == attempt.QuizId);

            if (option == null)
                return BadRequest("Đáp án không hợp lệ.");

            // Tìm câu trả lời đã lưu
            var existingAnswer = await _context.StudentAnswers
                .FirstOrDefaultAsync(a =>
                    a.QuizAttemptId == attempt.QuizAttemptId &&
                    a.QuestionId == model.QuestionId);

            if (existingAnswer == null)
            {
                // Chưa có thì thêm mới
                _context.StudentAnswers.Add(new StudentAnswer
                {
                    QuizAttemptId = attempt.QuizAttemptId,
                    QuestionId = model.QuestionId,
                    AnswerOptionId = option.AnswerOptionId,
                    IsCorrect = option.IsCorrect
                });
            }
            else
            {
                // Đã có thì cập nhật
                existingAnswer.AnswerOptionId = option.AnswerOptionId;
                existingAnswer.IsCorrect = option.IsCorrect;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã lưu đáp án"
            });
        }


        private async Task FinalizeAttemptAsync(
            QuizAttempt attempt,
            DateTime submittedAtUtc)
        {
            if (attempt.SubmittedAt != null)
                return;

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstAsync(q => q.QuizId == attempt.QuizId);

            var answers = await _context.StudentAnswers
                .Where(a => a.QuizAttemptId == attempt.QuizAttemptId)
                .ToListAsync();

            int totalQuestions = quiz.Questions.Count;

            if (totalQuestions == 0)
            {
                attempt.Score = 0;
                attempt.IsPassed = false;
            }
            else
            {
                int correctCount = 0;

                foreach (var answer in answers)
                {
                    var question = quiz.Questions
                        .FirstOrDefault(q =>
                            q.QuestionId == answer.QuestionId);

                    var option = question?.AnswerOptions
                        .FirstOrDefault(o =>
                            o.AnswerOptionId == answer.AnswerOptionId);

                    bool correct = option?.IsCorrect == true;

                    // Tính lại từ DB, không tin dữ liệu gửi lên
                    answer.IsCorrect = correct;

                    if (correct)
                        correctCount++;
                }

                attempt.Score = Math.Round(
                    correctCount * 10.0 / totalQuestions, 2);

                attempt.IsPassed =
                    attempt.Score >= quiz.PassingScore;
            }

            attempt.SubmittedAt = submittedAtUtc;

            await _context.SaveChangesAsync();

            // Đạt Quiz có thể là điều kiện cuối cùng để nhận chứng chỉ
            if (attempt.IsPassed)
            {
                var enrollment = attempt.Enrollment
                    ?? await _context.Enrollments
                        .FirstAsync(e =>
                            e.EnrollmentId == attempt.EnrollmentId);

                if (await _certificateService.TryIssueAsync(enrollment))
                {
                    TempData["Success"] =
                        "Chúc mừng! Bạn đã hoàn thành khóa học và được cấp chứng chỉ.";
                }
            }
        }

    }
}
