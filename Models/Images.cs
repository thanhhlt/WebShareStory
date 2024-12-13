using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public enum UseType
{
    profile = 1,
    post = 2
}

public class ImagesModel
{
    [Key]
    public int Id { get; set; }

    public string? FileName { get; set; }

    [Required]
    public required string FilePath { get; set; }

    public UseType UseType { get; set; }

    [ForeignKey(nameof(User))]
    public string? UserId { get; set; }

    [ForeignKey(nameof(Post))]
    public int? PostId { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual PostsModel? Post { get; set; }
}