using System;
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrolledAt { get; set; }
            = DateTime.Now;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        // Navigation Properties
        public User? User { get; set; }

        public Course? Course { get; set; }
    }
}