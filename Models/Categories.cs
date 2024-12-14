using App.Utilities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public class CategoriesModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Required]
    [MaxLength(255)]
    [RegularExpression(@"^[a-z0-9-]*$")]
    public required string Slug { get; set; }

    [ForeignKey(nameof(ParrentCate))]
    public int? ParrentCateId { get; set; }

    public CategoriesModel? ParrentCate { get; set; }
    public ICollection<CategoriesModel>? ChildCates { get; set; }
    public ICollection<PostsModel>? Posts { get; set; }

    public void SetName ()
    {
        Slug = SlugUtility.GenerateSlug(Name);
    }
}