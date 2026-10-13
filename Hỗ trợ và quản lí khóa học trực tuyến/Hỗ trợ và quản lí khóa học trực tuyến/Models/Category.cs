using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }
            = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public ICollection<Course> Courses { get; set; }
            = new List<Course>();
    }
}
