using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public enum LikeTypes
{
    Post,
    Comment
}

public class LikesModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public LikeTypes LikeType { get; set; }
    
    public DateTime DateLiked { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    [ForeignKey(nameof(Post))]
    public int? PostId { get; set; }

    [ForeignKey(nameof(Comment))]
    public int? CommentId { get; set; }

    public required AppUser User { get; set; }
    public PostsModel? Post { get; set; }
    public CommentsModel? Comment { get; set; }
}