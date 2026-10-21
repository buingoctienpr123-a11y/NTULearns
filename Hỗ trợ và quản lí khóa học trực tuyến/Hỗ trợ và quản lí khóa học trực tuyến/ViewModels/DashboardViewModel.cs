
using System.Collections.Generic;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCourses { get; set; }

        public int TotalStudents { get; set; }

        public int TotalInstructors { get; set; }

        public int TotalEnrollments { get; set; }

        public int TotalLessons { get; set; }

        public int TotalQuizzes { get; set; }

        public int TotalQuizAttempts { get; set; }

        public double AverageQuizScore { get; set; }

        public int PassedQuizAttempts { get; set; }

        public int FailedQuizAttempts { get; set; }

        // Tổng tiền các giao dịch thành công
        public decimal TotalRevenue { get; set; }

        public int SuccessfulTransactions { get; set; }

        public List<TopCourseItem> TopCourses { get; set; } = new();

        public List<RecentEnrollmentItem> RecentEnrollments { get; set; } = new();

        public List<MonthlyEnrollmentItem> MonthlyEnrollments
        { get; set; } = new();
    }

    public class MonthlyEnrollmentItem
    {
        public int Month { get; set; }

        public int Total { get; set; }
    }


    public class TopCourseItem
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = "";

        public int EnrollmentCount { get; set; }
    }

    public class RecentEnrollmentItem
    {
        public int EnrollmentId { get; set; }

        public string StudentName { get; set; } = "";

        public string CourseTitle { get; set; } = "";

        public DateTime EnrolledAt { get; set; }
    }

}
