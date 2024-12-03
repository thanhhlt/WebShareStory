#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.Models.RoleViewModels
{
  public class EditRoleModel
    {
        [Display(Name = "Tên của role")]
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
        public string Name { get; set; }
        public List<IdentityRoleClaim<string>> Claims { get; set; }

        public IdentityRole role { get; set; }
    }
}
