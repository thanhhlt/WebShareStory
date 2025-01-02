#nullable disable

using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

public class MainContentController : Controller
{
    private readonly AppDbContext _dbContext;
    public MainContentController(
        AppDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public class Post
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string DateCreated { get; set; }
        public string AuthorId { get; set; }
        public string Author { get; set; }
        public string AvatarPath { get; set; }

    }
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public Post LatestPost { get; set; }
        public int ToTalPosts { get; set; }
        public List<Category> ChildCategories { get; set; } = new List<Category>();
    }
    public class IndexViewModel
    {
        public List<Category> Categories { get; set; }
    }

    [HttpGet("/stories")]
    public async Task<IActionResult> Index()
    {
        var categories = await _dbContext.Categories
                                    .Where(c => c.ParentCateId == null)
                                    .Include(c => c.ChildCates)
                                    .ThenInclude(cc => cc.Posts)
                                    .ThenInclude(p => p.User)
                                    .Select(c => new Category()
                                    {
                                        Id = c.Id,
                                        Name = c.Name,
                                        Slug = c.Slug,
                                        ChildCategories = c.ChildCates.Select(cc => new Category()
                                        {
                                            Id = cc.Id,
                                            Name = cc.Name,
                                            Slug = cc.Slug,
                                            ToTalPosts = cc.Posts.Count(),
                                            LatestPost = cc.Posts.OrderByDescending(p => p.DateCreated).Take(1).Select(p => new Post()
                                            {
                                                Id = p.Id,
                                                Slug = p.Slug,
                                                Title = p.Title,
                                                DateCreated = p.DateCreated.ToString("dd/MM/yyyy"),
                                                AuthorId = p.AuthorId,
                                                Author = p.User.UserName,
                                                AvatarPath = _dbContext.Images.Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                                                                            .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg"
                                            }).FirstOrDefault()
                                        }).ToList()
                                    }).ToListAsync();

        var totalPosts = await _dbContext.Posts.CountAsync();

        IndexViewModel model = new IndexViewModel()
        {
            Categories = categories,
        };

        return View(model);
    }
}

