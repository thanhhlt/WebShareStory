using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public class CommentsModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = "";

    public DateTime DateCommented {get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParrentComment))]
    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(Posts))]
    public required int PostId { get; set; }

    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    public CommentsModel? ParrentComment { get; set; }
    public ICollection<CommentsModel>? ChildComments { get; set; }
    public required PostsModel Posts { get; set; }
    public required AppUser User { get; set; }
}