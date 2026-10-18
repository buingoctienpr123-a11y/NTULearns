
using System;
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class LessonProgress
    {
        [Key]
        public int LessonProgressId { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        // Navigation Properties
        public Enrollment? Enrollment { get; set; }

        public Lesson? Lesson { get; set; }
    }
}

