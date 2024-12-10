using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Models;

public enum Gender
{
        Male = 1,
        Female = 2,
        Unspecified = 3
}

public class AppUser : IdentityUser
{
        [Required()]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; } = DateTime.UtcNow;

        public Gender? Gender { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public string? Introduction { get; set; }

        [Required]
        public bool isActivate { get; set; } = false;

        [Required]
        [DataType(DataType.Date)]
        public DateTime AccountCreationDate { get; set; } = DateTime.UtcNow;

        public DateTimeOffset? PostLockEnd { get; set; }

        public DateTimeOffset? CommentLockEnd { get; set; }

        public virtual ICollection<PostsModel>? Posts { get; set; }
        public virtual ICollection<CommentsModel>? Comments { get; set; }
        public virtual ICollection<SupportRequestsModel>? SupportRequests { get; set; }
        public virtual ICollection<LoggedBrowsersModel>? LoggedBrowsers { get; set; }
}