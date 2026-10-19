
namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class UserDetailsViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        public string RoleName { get; set; } = "";

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public int TotalEnrollments { get; set; }

        public int TotalCompletedLessons { get; set; }

        public int TotalQuizAttempts { get; set; }

        public List<UserEnrollmentDetailViewModel>
            Enrollments
        { get; set; } = new();

        public List<UserQuizHistoryViewModel>
            QuizHistory
        { get; set; } = new();
    }

    public class UserEnrollmentDetailViewModel
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = "";

        public DateTime EnrolledAt { get; set; }

        public string Status { get; set; } = "";

        public int TotalLessons { get; set; }

        public int CompletedLessons { get; set; }

        public double ProgressPercent =>
            TotalLessons == 0
                ? 0
                : Math.Round(
                    CompletedLessons * 100.0 / TotalLessons,
                    1);
    }

    public class UserQuizHistoryViewModel
    {
        public int QuizAttemptId { get; set; }

        public string QuizTitle { get; set; } = "";

        public string CourseTitle { get; set; } = "";

        public DateTime StartedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public double? Score { get; set; }

        public bool IsPassed { get; set; }
    }
}
