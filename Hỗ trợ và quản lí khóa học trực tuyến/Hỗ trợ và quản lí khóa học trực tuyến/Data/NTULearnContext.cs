using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Microsoft.EntityFrameworkCore;


namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data
{
    public class NTULearnContext : DbContext
    {
        public NTULearnContext(
            DbContextOptions<NTULearnContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Lesson> Lessons { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<AnswerOption> AnswerOptions { get; set; }

        public DbSet<QuizAttempt> QuizAttempts { get; set; }

        public DbSet<StudentAnswer> StudentAnswers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Category)
                .WithMany(c => c.Courses)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(u => u.InstructorCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            // Tài khoản đã có trước khi thêm chức năng duyệt
            // được coi là đã duyệt.
            modelBuilder.Entity<User>()
                .Property(u => u.ApprovalStatus)
                .HasDefaultValue("Approved");

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    RoleId = 1,
                    RoleName = "Admin"
                },
                new Role
                {
                    RoleId = 2,
                    RoleName = "GiangVien"
                },
                new Role
                {
                    RoleId = 3,
                    RoleName = "HocVien"
                }
            );

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.UserId, e.CourseId })
                .IsUnique();


            modelBuilder.Entity<LessonProgress>()
                .HasOne(lp => lp.Enrollment)
                .WithMany(e => e.LessonProgresses)
                .HasForeignKey(lp => lp.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonProgress>()
                .HasOne(lp => lp.Lesson)
                .WithMany(l => l.LessonProgresses)
                .HasForeignKey(lp => lp.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mỗi bài học chỉ có một bản ghi tiến độ
            // trong cùng một lượt đăng ký.
            modelBuilder.Entity<LessonProgress>()
                .HasIndex(lp => new
                {
                    lp.EnrollmentId,
                    lp.LessonId
                })
                .IsUnique();


            // Quiz thuộc một Course
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Question thuộc một Quiz
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Restrict);

            // AnswerOption thuộc một Question
            modelBuilder.Entity<AnswerOption>()
                .HasOne(a => a.Question)
                .WithMany(q => q.AnswerOptions)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // QuizAttempt thuộc một Quiz
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(a => a.Quiz)
                .WithMany(q => q.QuizAttempts)
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Restrict);

            // QuizAttempt thuộc một Enrollment
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(a => a.Enrollment)
                .WithMany(e => e.QuizAttempts)
                .HasForeignKey(a => a.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentAnswer thuộc một QuizAttempt
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(a => a.QuizAttempt)
                .WithMany(at => at.StudentAnswers)
                .HasForeignKey(a => a.QuizAttemptId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentAnswer thuộc một Question
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentAnswer chọn một AnswerOption
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(a => a.AnswerOption)
                .WithMany(o => o.StudentAnswers)
                .HasForeignKey(a => a.AnswerOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mỗi lượt làm bài chỉ có một câu trả lời
            // cho mỗi câu hỏi.
            modelBuilder.Entity<StudentAnswer>()
                .HasIndex(a => new
                {
                    a.QuizAttemptId,
                    a.QuestionId
                })
                .IsUnique();

        }
    }
}

