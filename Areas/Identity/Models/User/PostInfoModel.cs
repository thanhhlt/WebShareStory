#nullable disable

namespace App.Areas.Identity.Models.UserViewModels
{
    public class PostInfoModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string Category { get; set; }
        public string Slug { get; set; }
    }
}