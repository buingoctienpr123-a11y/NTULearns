
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public Quiz? Quiz { get; set; }

        public ICollection<AnswerOption> AnswerOptions { get; set; }
            = new List<AnswerOption>();

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
            = new List<StudentAnswer>();
    }
}
