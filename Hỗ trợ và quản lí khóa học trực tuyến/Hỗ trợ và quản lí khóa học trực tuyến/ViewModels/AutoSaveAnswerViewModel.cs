
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class AutoSaveAnswerViewModel
    {
        [Range(1, int.MaxValue)]
        public int QuizAttemptId { get; set; }

        [Range(1, int.MaxValue)]
        public int QuestionId { get; set; }

        [Range(1, int.MaxValue)]
        public int AnswerOptionId { get; set; }
    }
}
