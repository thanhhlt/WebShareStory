#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace App.Controllers;

[Route("home/[action]")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly IThumbnailService _thumbnailService;
    private readonly IUserBlockService _userBlockService;

    public HomeController(
        ILogger<HomeController> logger, 
        AppDbContext dbContext,
        IWebHostEnvironment env,
        IThumbnailService thumbnailService,
        IUserBlockService userBlockService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _env = env;
        _thumbnailService = thumbnailService;
        _userBlockService = userBlockService;
    }
    
    public class Post
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public int NumViews { get; set; }
        public int NumLikes { get; set; }
        public int NumComments { get; set; }
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
        var Posts = _userBlockService.GetFilteredPosts(_dbContext.Posts);

        var dateCreatedLatest = await Posts.MaxAsync(p => p.DateCreated);
        var sevenDaysAgoFromLatest = dateCreatedLatest.AddDays(-7);

        var featuredPosts = await Posts
            .AsNoTracking()
            .Where(p => p.DateCreated >= sevenDaysAgoFromLatest && p.DateCreated <= dateCreatedLatest)
            .OrderByDescending(p => p.NumViews).ThenByDescending(p => p.Likes.Count)
            .Take(3)
            .Select(p => new Post
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Description = p.Description,
                DateCreated = p.DateCreated,
                DateUpdated = p.DateUpdated,
                NumViews = p.NumViews,
                NumLikes = p.Likes.Count,
                NumComments = p.Comments.Count,
                CateName = p.Category.Name,
                Author = p.User.UserName,
                AvatarPath = _dbContext.Images
                    .Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                    .Select(i => i.FilePath)
                    .FirstOrDefault() ?? "/images/no_avt.jpg"
            })
            .ToListAsync();

        if (featuredPosts.Count < 3)
        {
            var additionalPosts = await Posts
                .AsNoTracking()
                .OrderByDescending(p => p.NumViews).ThenByDescending(p => p.Likes.Count)
                .Take(3 - featuredPosts.Count)
                .Select(p => new Post
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = p.Title,
                    Description = p.Description,
                    DateCreated = p.DateCreated,
                    DateUpdated = p.DateUpdated,
                    NumViews = p.NumViews,
                    NumLikes = p.Likes.Count,
                    NumComments = p.Comments.Count,
                    CateName = p.Category.Name,
                    Author = p.User.UserName,
                    AvatarPath = _dbContext.Images
                        .Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                        .Select(i => i.FilePath)
                        .FirstOrDefault() ?? "/images/no_avt.jpg"
                })
                .ToListAsync();

            featuredPosts.AddRange(additionalPosts);
        }

        model.FeaturedPosts = featuredPosts;

        model.LatestPosts = await Posts
            .AsNoTracking()
            .OrderByDescending(p => p.DateCreated)
            .Take(20)
            .Select(p => new Post
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Description = TrimDescription(p.Description, 150),
                DateCreated = p.DateCreated,
                DateUpdated = p.DateUpdated,
                CateName = p.Category.Name,
                Author = p.User.UserName,
                AvatarPath = _dbContext.Images
                    .Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                    .Select(i => i.FilePath)
                    .FirstOrDefault() ?? "/images/no_avt.jpg"
            })
            .ToListAsync();

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
        var post = _dbContext.Posts.Include(p => p.Image).FirstOrDefault(p => p.Id == postId);

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
    
    public async Task<IActionResult> Privacy()
    {
        var slug = await _dbContext.Posts.AsNoTracking()
                                .Where(p => p.Title == "Chính sách bảo mật" && p.Category.Name == "Thông báo chung")
                                .Select(p => p.Slug).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound("Không có Chính sách bảo mật");
        }
        return RedirectToAction("Index", "Post", new {slugPost = slug});
    }

    public async Task<IActionResult> TermsOfUse()
    {
        var slug = await _dbContext.Posts.AsNoTracking()
                                .Where(p => p.Title == "Điều khoản sử dụng" && p.Category.Name == "Thông báo chung")
                                .Select(p => p.Slug).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound("Không có Điều khoản sử dụng");
        }
        return RedirectToAction("Index", "Post", new {slugPost = slug});
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}