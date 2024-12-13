#nullable disable

namespace App.Areas.Identity.Models.UserViewModels
{
    public class UserInfoModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string Address { get; set; }
        public bool isActivate { get; set; }
        public DateTime AccountCreated { get; set; }
        public DateTimeOffset? AccountLockEnd { get; set; }
        public DateTimeOffset? PostLockEnd { get; set; }
        public DateTimeOffset? CommentLockEnd { get; set; }
        public string AvatarPath { get; set; }
    }
}