
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class StudentAnswer
    {
        [Key]
        public int StudentAnswerId { get; set; }

        [Required]
        public int QuizAttemptId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public int AnswerOptionId { get; set; }

        public bool IsCorrect { get; set; } = false;

        public QuizAttempt? QuizAttempt { get; set; }

        public Question? Question { get; set; }

        public AnswerOption? AnswerOption { get; set; }
    }
}
