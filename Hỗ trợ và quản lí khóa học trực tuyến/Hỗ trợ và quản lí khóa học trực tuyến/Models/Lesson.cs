using System.ComponentModel.DataAnnotations;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class Lesson
    {
        public int LessonId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? VideoUrl { get; set; }

        public int OrderIndex { get; set; }

        public int CourseId { get; set; }

        public Course? Course { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<LessonProgress> LessonProgresses { get; set; }
            = new List<LessonProgress>();
    }
}