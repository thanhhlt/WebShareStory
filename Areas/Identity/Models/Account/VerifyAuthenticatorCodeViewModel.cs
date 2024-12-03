// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class VerifyAuthenticatorCodeViewModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [Display(Name = "Nhập mã khôi phục tài khoản.")]
        public string Code { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Nhớ thông tin trình duyệt này?")]
        public bool RememberBrowser { get; set; }

        [Display(Name = "Nhớ thông tin đăng nhập?")]
        public bool RememberMe { get; set; }
    }
}
