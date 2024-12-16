#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Post.Models.Category;

public class CreateModel
{
    public int Id { get; set; }

    [Display(Name = "Tên danh mục")]
    [Required(ErrorMessage = "{0} không được bỏ trống.")]
    [MaxLength(100, ErrorMessage = "{0} không dài quá {1} ký tự")]
    public string Name { get; set; }

    [Display(Name="Mô tả")]
    public string Description { get; set; }
    public string ParentCate { get; set; } = null;
    public SelectList AllCates { get; set; }
}