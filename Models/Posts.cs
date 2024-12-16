using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Utilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace App.Models;

public class PostsModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = "";

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Content { get; set; } = "";

    [MaxLength(50)]
    public string? Hashtag { get; set; }

    [Required]
    [MaxLength(255)]
    [RegularExpression(@"^[a-z0-9-]*$")]
    public required string Slug { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    public bool isPublished { get; set; }

    public bool isChildAllowed { get; set; }

    [ForeignKey(nameof(User))]
    public string? AuthorId { get; set; }

    [ForeignKey(nameof(Category))]
    public int? CategoryId { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual CategoriesModel? Category { get; set; }
    public virtual ICollection<CommentsModel>? Comments { get; set; }
    public virtual ICollection<LikesModel>? Likes { get; set; }
    public virtual ICollection<ImagesModel>? Images { get; set; }

    public void SetSlug ()
    {
        Slug = SlugUtility.GenerateSlug(Title) + '.' + Id;
    }
}