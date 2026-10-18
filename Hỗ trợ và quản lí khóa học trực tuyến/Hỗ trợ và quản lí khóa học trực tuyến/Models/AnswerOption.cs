
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class AnswerOption
    {
        [Key]
        public int AnswerOptionId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public Question? Question { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
            = new List<StudentAnswer>();
    }
}
