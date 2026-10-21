using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models
{
    // Tài liệu đính kèm của bài học.
    // File được lưu ngoài wwwroot với tên ngẫu nhiên,
    // tên gốc chỉ lưu trong cơ sở dữ liệu.
    public class LessonAttachment
    {
        public int LessonAttachmentId { get; set; }

        public int LessonId { get; set; }

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public Lesson? Lesson { get; set; }
    }
}
