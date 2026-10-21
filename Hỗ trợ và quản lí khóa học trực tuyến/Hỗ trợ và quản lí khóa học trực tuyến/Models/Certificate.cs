using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    // Chứng chỉ hoàn thành khóa học.
    // Mỗi học viên chỉ có một chứng chỉ cho mỗi khóa học.
    public class Certificate
    {
        public int CertificateId { get; set; }

        public int UserId { get; set; }

        public int CourseId { get; set; }

        [Required]
        [StringLength(30)]
        public string CertificateCode { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }

        public Course? Course { get; set; }
    }
}
