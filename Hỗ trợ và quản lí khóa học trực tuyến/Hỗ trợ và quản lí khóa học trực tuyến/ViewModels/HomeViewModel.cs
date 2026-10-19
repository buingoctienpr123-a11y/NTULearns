
namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class HomeViewModel
    {
        public string Keyword { get; set; } = "";

        public int TotalCourses { get; set; }

        public int TotalStudents { get; set; }

        public int TotalLessons { get; set; }

        public List<FeaturedCourseViewModel> FeaturedCourses
        { get; set; } = new();
    }

    public class FeaturedCourseViewModel
    {
        public int CourseId { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string? ThumbnailUrl { get; set; }

        public decimal Price { get; set; }

        public string CategoryName { get; set; } = "";

        public string InstructorName { get; set; } = "";

        public int EnrollmentCount { get; set; }
    }
}
