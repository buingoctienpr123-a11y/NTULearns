
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class QuizAttempt
    {
        [Key]
        public int QuizAttemptId { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? SubmittedAt { get; set; }

        public double? Score { get; set; }

        public bool IsPassed { get; set; } = false;

        public Quiz? Quiz { get; set; }

        public Enrollment? Enrollment { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
            = new List<StudentAnswer>();
    }
}
