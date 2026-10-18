
using System;
using System.Collections.Generic;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class QuizStatisticsViewModel
    {
        public int QuizId { get; set; }

        public string QuizTitle { get; set; } = "";

        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = "";

        public int TotalAttempts { get; set; }

        public int TotalStudents { get; set; }

        public double AverageScore { get; set; }

        public double HighestScore { get; set; }

        public double LowestScore { get; set; }

        public int PassedAttempts { get; set; }

        public int FailedAttempts { get; set; }

        public double PassRate { get; set; }

        public List<QuizAttemptStatisticsItem> Attempts
        { get; set; } = new();
    }

    public class QuizAttemptStatisticsItem
    {
        public int QuizAttemptId { get; set; }

        public string StudentName { get; set; } = "";

        public DateTime? SubmittedAt { get; set; }

        public double Score { get; set; }

        public bool IsPassed { get; set; }
    }
}
