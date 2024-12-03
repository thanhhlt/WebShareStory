// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.ManageViewModels
{
    public class AddPhoneNumberViewModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }
}
