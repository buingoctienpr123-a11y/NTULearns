
using System.Collections.Generic;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class QuizTakeViewModel
    {
        public int QuizId { get; set; }

        public string Title { get; set; } = "";

        public int DurationMinutes { get; set; }

        public List<QuizQuestionViewModel> Questions { get; set; }
            = new();

        public int QuizAttemptId { get; set; }

        public DateTime DeadlineUtc { get; set; }
    }

    public class QuizQuestionViewModel
    {
        public int QuestionId { get; set; }

        public string Content { get; set; } = "";

        public int OrderIndex { get; set; }

        public int? SelectedOptionId { get; set; }

        public List<QuizOptionViewModel> Options { get; set; }
            = new();
    }

    public class QuizOptionViewModel
    {
        public int AnswerOptionId { get; set; }

        public string Content { get; set; } = "";
    }
}
