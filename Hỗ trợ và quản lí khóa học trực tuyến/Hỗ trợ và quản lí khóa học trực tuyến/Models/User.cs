using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public Role Role { get; set; } = null!;

        public ICollection<Course> InstructorCourses { get; set; }
           = new List<Course>();

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

    }
}
