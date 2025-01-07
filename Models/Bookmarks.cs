using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public class BookmarksModel
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    [ForeignKey(nameof(Post))]
    public required int PostId { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public virtual AppUser? User { get; set; }
    public virtual PostsModel? Post { get; set; }
}