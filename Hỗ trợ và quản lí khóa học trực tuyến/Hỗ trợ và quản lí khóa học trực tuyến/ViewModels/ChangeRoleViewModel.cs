
using System.ComponentModel.DataAnnotations;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels
{
    public class ChangeRoleViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        public string CurrentRoleName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public int? NewRoleId { get; set; }

        public List<UserRoleOptionViewModel> Roles
        { get; set; } = new();
    }
}
