using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }

        public int CategoryId { get; set; }

        public int InstructorId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // Lý do Admin từ chối khóa học (khi Status = "Rejected")
        [StringLength(255)]
        public string? RejectionReason { get; set; }

        public Category? Category { get; set; }

        public User? Instructor { get; set; }

        public ICollection<Lesson> Lessons { get; set; }
            = new List<Lesson>();

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<Quiz> Quizzes { get; set; }
            = new List<Quiz>();
    }
}
