
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class Quiz
    {
        [Key]
        public int QuizId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int DurationMinutes { get; set; } = 30;

        public double PassingScore { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsPublished { get; set; } = false;

        // Navigation Properties
        public Course? Course { get; set; }

        public ICollection<Question> Questions { get; set; }
            = new List<Question>();

        public ICollection<QuizAttempt> QuizAttempts { get; set; }
            = new List<QuizAttempt>();
    }
}
