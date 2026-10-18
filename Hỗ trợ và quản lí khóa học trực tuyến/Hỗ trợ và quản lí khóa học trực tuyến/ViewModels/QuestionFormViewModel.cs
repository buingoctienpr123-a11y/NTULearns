
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class QuestionFormViewModel
    {
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập câu hỏi")]
        public string Content { get; set; } = "";

        [Range(1, 1000)]
        public int OrderIndex { get; set; } = 1;

        [Required(ErrorMessage = "Nhập đáp án A")]
        public string AnswerA { get; set; } = "";

        [Required(ErrorMessage = "Nhập đáp án B")]
        public string AnswerB { get; set; } = "";

        [Required(ErrorMessage = "Nhập đáp án C")]
        public string AnswerC { get; set; } = "";

        [Required(ErrorMessage = "Nhập đáp án D")]
        public string AnswerD { get; set; } = "";

        [Required]
        [RegularExpression("^[ABCD]$",
            ErrorMessage = "Chọn đáp án A, B, C hoặc D")]
        public string CorrectAnswer { get; set; } = "";

        public int QuestionId { get; set; }
    }
}
