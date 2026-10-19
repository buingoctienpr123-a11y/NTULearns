
namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class UserManagementViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        public int RoleId { get; set; }

        public string RoleName { get; set; } = "";

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class UserManagementIndexViewModel
    {
        public string Keyword { get; set; } = "";

        public int? RoleId { get; set; }

        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalUsers { get; set; }

        public List<UserManagementViewModel> Users
        { get; set; } = new();

        public List<UserRoleOptionViewModel> Roles
        { get; set; } = new();
    }

    public class UserRoleOptionViewModel
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = "";
    }
}
