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

    [ForeignKey(nameof(ParentComment))]
    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(Posts))]
    public required int PostId { get; set; }

    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    public virtual CommentsModel? ParentComment { get; set; }
    public virtual ICollection<CommentsModel>? ChildComments { get; set; }
    public virtual PostsModel? Posts { get; set; }
    public virtual AppUser? User { get; set; }
}