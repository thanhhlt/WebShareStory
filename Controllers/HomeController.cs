#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;

namespace App.Controllers;

[Route("home/[action]")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IThumbnailService _thumbnailService;

    public HomeController(
        ILogger<HomeController> logger, 
        AppDbContext context,
        IWebHostEnvironment env,
        IThumbnailService thumbnailService
        )
    {
        _logger = logger;
        _context = context;
        _env = env;
        _thumbnailService = thumbnailService;
    }
    
    public class Post
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public int NumLikes { get; set; }
        public string CateName { get; set; }
        public string Author { get; set; }
        public string AvatarPath { get; set; }
    }
    public class IndexViewModel
    {
        public List<Post> FeaturedPosts { get; set; }
        public List<Post> LatestPosts { get; set; }
    }

    //GET: /
    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel();

        var dateCreatedLatest = _context.Posts.Max(p => p.DateCreated);
        var sevenDaysAgoFromLatest = dateCreatedLatest.AddDays(-7);
        var featuredPosts = await _context.Posts.Where(p => p.DateCreated >= sevenDaysAgoFromLatest && p.DateCreated <= dateCreatedLatest)
                                        .Include(p => p.Likes)
                                        .Include(p => p.Category)
                                        .Include(p => p.User)
                                        .OrderByDescending(p => p.Likes.Count).Take(3).ToListAsync();
        if (featuredPosts == null || featuredPosts.Count < 3)
        {
            featuredPosts = await _context.Posts.Include(p => p.Likes)
                                                .Include(p => p.Category)
                                                .Include(p => p.User)
                                                .OrderByDescending(p => p.Likes.Count).Take(3).ToListAsync();
        }
        model.FeaturedPosts = featuredPosts.Select(p => new Post
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = p.Title,
            Description = TrimDescription(p.Description ?? RemoveImagesAndTags(p.Content ?? ""), 250),
            DateCreated = p.DateCreated,
            DateUpdated = p.DateUpdated,
            NumLikes = p.Likes.Count,
            CateName = p.Category.Name,
            Author = p.User.UserName,
            AvatarPath = _context.Images.Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                                        .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg"
        }).ToList();

        var latestPosts = _context.Posts.Include(p => p.Category)
                                            .Include(p => p.User)
                                            .OrderByDescending(p => p.DateCreated).Take(20)
                                            .Select(p => new Post
                                            {
                                                Id = p.Id,
                                                Slug = p.Slug,
                                                Title = p.Title,
                                                Description = TrimDescription(p.Description ?? RemoveImagesAndTags(p.Content ?? ""), 250),
                                                DateCreated = p.DateCreated,
                                                DateUpdated = p.DateUpdated,
                                                CateName = p.Category.Name,
                                                Author = p.User.UserName,
                                                AvatarPath = _context.Images.Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                                                                            .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg"
                                            }).ToList();
        model.LatestPosts = latestPosts;
        return View(model);
    }
    private static string RemoveImagesAndTags(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var imgNodes = doc.DocumentNode.SelectNodes("//img");
        if (imgNodes != null)
        {
            foreach (var img in imgNodes)
            {
                img.Remove();
            }
        }

        return doc.DocumentNode.InnerText.Trim();
    }
    private static string TrimDescription(string description, int maxLength)
    {
        if (description.Length > maxLength)
        {
            var d = description.Substring(0, maxLength) + '…';
            return d;
        }
        return description;
    }
    //GET: /home/Thumbnail
    [HttpGet]
    public IActionResult Thumbnail(int postId)
    {
        var post = _context.Posts.Include(p => p.Image).FirstOrDefault(p => p.Id == postId);

        if (post?.Image != null)
        {
            var imagePath = post.Image.FilePath;
            if (!string.IsNullOrEmpty(imagePath) && imagePath.StartsWith("/imgs/"))
            {
                imagePath = Path.Combine(_env.ContentRootPath, "Images/" + imagePath.Substring(6));
                if (System.IO.File.Exists(imagePath))
                {
                    var imageBytes = System.IO.File.ReadAllBytes(imagePath);
                    return File(imageBytes, "image/png");
                }
            }
        }

        var title = post?.Title ?? "Title";
        var generatedImageBytes = _thumbnailService.GenerateThumbnail(title, 350, 210);
        return File(generatedImageBytes, "image/png");
    }
    
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
