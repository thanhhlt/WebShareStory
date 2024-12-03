#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.UserViewModels
{
  public class SetUserPasswordModel
  {
      [Required(ErrorMessage = "{0} không được bỏ trống.")]
      [StringLength(50, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
      [DataType(DataType.Password)]
      [Display(Name = "Mật khẩu mới")]
      public string NewPassword { get; set; }

      [DataType(DataType.Password)]
      [Display(Name = "Xác nhận mật khẩu")]
      [Compare("NewPassword", ErrorMessage = "Sai mật khẩu xác nhận.")]
      public string ConfirmPassword { get; set; }


  }
}