using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace App.Models;

public class AppUser: IdentityUser 
{
        [Required()]       
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        public bool? isActivate { get; set; }

        public virtual ICollection<PostsModel>? Posts { get; set; }
        public virtual ICollection<CommentsModel>? Comments { get; set; }
        public virtual ICollection<SupportRequestsModel>? SupportRequests { get; set; }
}