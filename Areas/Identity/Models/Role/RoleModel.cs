// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.Models.RoleViewModels
{
    public class RoleModel : IdentityRole
    {
        public string[] Claims { get; set; }
    }
}
