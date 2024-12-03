#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.ManageViewModels
{
  public class EditExtraProfileModel
  {
      [Display(Name = "Tên tài khoản")]
      [StringLength(100, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.", MinimumLength = 3)]
      public string UserName { get; set; }

      [Display(Name = "Email")]
      [EmailAddress(ErrorMessage = "{0} sai định dạng.")]
      public string UserEmail { get; set; }

      [Display(Name = "Số điện thoại")]
      [Phone(ErrorMessage = "{0} sai định dạng.")]
      public string PhoneNumber { get; set; }

      [Display(Name = "Ngày sinh")]
      public DateTime? BirthDate { get; set; }
  }
}