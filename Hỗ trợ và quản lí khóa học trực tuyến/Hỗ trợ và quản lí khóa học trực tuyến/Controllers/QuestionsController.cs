
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    [Authorize(Roles = "Admin,GiangVien")]
    public class QuestionsController : Controller
    {
        private readonly NTULearnContext _context;

        public QuestionsController(NTULearnContext context)
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

        // Danh sách câu hỏi
        [HttpGet]
        public async Task<IActionResult> Index(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            var questions = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            ViewBag.Quiz = quiz;

            return View(questions);
        }

        // Giao diện thêm câu hỏi
        [HttpGet]
        public async Task<IActionResult> Create(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            if (quiz.IsPublished)
            {
                TempData["Error"] =
                    "Không thể thêm câu hỏi vào Quiz đã công bố.";

                return RedirectToAction(
                    nameof(Index), new { quizId });
            }

            int nextOrder = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Select(q => (int?)q.OrderIndex)
                .MaxAsync() ?? 0;

            return View(new QuestionFormViewModel
            {
                QuizId = quizId,
                OrderIndex = nextOrder + 1
            });
        }

        // Lưu câu hỏi và 4 đáp án
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            QuestionFormViewModel model)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

            if (quiz == null)
                return NotFound();

            if (quiz.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            if (quiz.IsPublished)
                return BadRequest(
                    "Quiz đã công bố không thể thêm câu hỏi.");

            if (!ModelState.IsValid)
                return View(model);

            var question = new Question
            {
                QuizId = model.QuizId,
                Content = model.Content.Trim(),
                OrderIndex = model.OrderIndex
            };

            question.AnswerOptions = new List<AnswerOption>
            {
                new AnswerOption
                {
                    Content = model.AnswerA.Trim(),
                    IsCorrect = model.CorrectAnswer == "A"
                },
                new AnswerOption
                {
                    Content = model.AnswerB.Trim(),
                    IsCorrect = model.CorrectAnswer == "B"
                },
                new AnswerOption
                {
                    Content = model.AnswerC.Trim(),
                    IsCorrect = model.CorrectAnswer == "C"
                },
                new AnswerOption
                {
                    Content = model.AnswerD.Trim(),
                    IsCorrect = model.CorrectAnswer == "D"
                }
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thêm câu hỏi thành công!";

            return RedirectToAction(
                nameof(Index),
                new { quizId = model.QuizId });
        }

        // Xóa câu hỏi chưa có lịch sử trả lời
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(q => q!.Course)
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            var quiz = question.Quiz;

            if (quiz?.Course == null ||
                !CanManageCourse(quiz.Course))
                return Forbid();

            if (quiz.IsPublished)
            {
                TempData["Error"] =
                    "Không thể xóa câu hỏi của Quiz đã công bố.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }

            bool hasAnswers = await _context.StudentAnswers
                .AnyAsync(a => a.QuestionId == id);

            if (hasAnswers)
            {
                TempData["Error"] =
                    "Câu hỏi đã có lịch sử làm bài, không thể xóa.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }

            _context.AnswerOptions.RemoveRange(
                question.AnswerOptions);

            _context.Questions.Remove(question);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã xóa câu hỏi.";

            return RedirectToAction(
                nameof(Index),
                new { quizId = question.QuizId });
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.Quiz)
                    .ThenInclude(q => q!.Course)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            if (question.Quiz?.Course == null ||
                !CanManageCourse(question.Quiz.Course))
                return Forbid();

            if (question.Quiz.IsPublished)
            {
                TempData["Error"] =
                    "Không thể sửa câu hỏi của Quiz đã công bố.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }

            var options = question.AnswerOptions
                .OrderBy(a => a.AnswerOptionId)
                .ToList();

            if (options.Count != 4)
                return BadRequest("Câu hỏi phải có đúng 4 đáp án.");

            var correctIndex = options.FindIndex(a => a.IsCorrect);

            if (correctIndex < 0)
                return BadRequest("Câu hỏi chưa có đáp án đúng.");

            var model = new QuestionFormViewModel
            {
                QuestionId = question.QuestionId,
                QuizId = question.QuizId,
                Content = question.Content,
                OrderIndex = question.OrderIndex,
                AnswerA = options[0].Content,
                AnswerB = options[1].Content,
                AnswerC = options[2].Content,
                AnswerD = options[3].Content,
                CorrectAnswer = "ABCD"[correctIndex].ToString()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            QuestionFormViewModel model)
        {
            if (id != model.QuestionId)
                return BadRequest();

            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.Quiz)
                    .ThenInclude(q => q!.Course)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            if (question.Quiz?.Course == null ||
                !CanManageCourse(question.Quiz.Course))
                return Forbid();

            if (question.Quiz.IsPublished)
                return BadRequest("Quiz đã công bố không thể chỉnh sửa.");

            var options = question.AnswerOptions
                .OrderBy(a => a.AnswerOptionId)
                .ToList();

            if (options.Count != 4)
                return BadRequest("Câu hỏi phải có đúng 4 đáp án.");

            if (!ModelState.IsValid)
            {
                model.QuizId = question.QuizId;
                return View(model);
            }

            question.Content = model.Content.Trim();
            question.OrderIndex = model.OrderIndex;

            string[] contents =
            {
                model.AnswerA.Trim(),
                model.AnswerB.Trim(),
                model.AnswerC.Trim(),
                model.AnswerD.Trim()
            };

            for (int i = 0; i < 4; i++)
            {
                options[i].Content = contents[i];

                options[i].IsCorrect =
                    model.CorrectAnswer == "ABCD"[i].ToString();
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật câu hỏi thành công!";

            return RedirectToAction(
                nameof(Index),
                new { quizId = question.QuizId });
        }

    }
}
