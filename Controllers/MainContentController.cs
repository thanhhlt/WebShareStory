#nullable disable

using App.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

[Route("maincontent/[action]")]
public class MainContentController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly IThumbnailService _thumbnailService;
    private readonly IUserBlockService _userBlockService;
    public MainContentController(
        AppDbContext dbContext,
        IWebHostEnvironment env,
        IThumbnailService thumbnailService,
        IUserBlockService userBlockService)
    {
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
        public string DateCreated { get; set; }
        public string DateUpdated { get; set; }
        public int NumLikes { get; set; }
        public int NumViews { get; set; }
        public bool IsPinned { get; set; }
        public string AuthorId { get; set; }
        public string Author { get; set; }
        public string AvatarPath { get; set; }
    }
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public Post LatestPost { get; set; }
        public int ToTalPosts { get; set; }
        public List<Category> ChildCategories { get; set; } = new List<Category>();
        public Category ParentCategory { get; set; }
    }
    public class IndexViewModel
    {
        public List<Category> Categories { get; set; }
    }

    //GET: /stories
    [HttpGet("/stories")]
    public async Task<IActionResult> Index()
    {
        var filteredPosts = _userBlockService.GetFilteredPosts(_dbContext.Posts);
        var categories = await _dbContext.Categories
                                    .AsNoTracking()
                                    .Where(c => c.ParentCateId == null)
                                    .Include(c => c.ChildCates)
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
                                            ToTalPosts = filteredPosts.Count(p => p.CategoryId == cc.Id),
                                            LatestPost = filteredPosts.Where(p => p.CategoryId == cc.Id).AsNoTracking()
                                                .OrderByDescending(p => p.DateCreated).Take(1).Select(p => new Post()
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

        // var totalPosts = await _dbContext.Posts.CountAsync();

        IndexViewModel model = new IndexViewModel()
        {
            Categories = categories,
        };

        return View(model);
    }

    public class CategoryPostsViewModel
    {
        public Category Category { get; set; }
        public List<Post> PostsPinned { get; set; }
        public List<Post> Posts { get; set; }

        //Pagging
        public int totalPosts { get; set; }
        public int countPages { get; set; }

        public int ITEMS_PER_PAGE { get; set; } = 20;

        public int currentPage { get; set; }
    }

    //GET: /stories/{cateSlug}
    [HttpGet("/stories/{slug}")]
    public async Task<IActionResult> CategoryPosts(string slug, [FromQuery(Name = "p")] int currentPage, string sortOption = "DateCreated", string sortBy = "desc")
    {
        if (slug == null)
        {
            return NotFound("Không tìm thấy danh mục");
        }

        var filteredPosts = _userBlockService.GetFilteredPosts(_dbContext.Posts);

        var model = new CategoryPostsViewModel();

        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Slug == slug)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Slug,
                ParentCategory = new Category
                {
                    Id = c.ParentCateId ?? 0,
                    Name = c.ParentCate.Name,
                },
                Posts = filteredPosts.Where(p => p.CategoryId == c.Id)
                    .AsNoTracking().Select(p => new
                    {
                        p.Id,
                        p.Slug,
                        p.Title,
                        Description = TrimDescription(p.Description, 150),
                        DateCreated = p.DateCreated,
                        DateUpdated = p.DateUpdated,
                        NumViews = p.NumViews,
                        NumLikes = p.Likes.Count(),
                        NumComments = p.Comments.Count(),
                        p.isPinned,
                        AuthorId = p.AuthorId,
                        Author = p.User.UserName,
                        AvatarPath = _dbContext.Images
                            .Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                            .Select(i => i.FilePath)
                            .FirstOrDefault() ?? "/images/no_avt.jpg"
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound("Không tìm thấy danh mục");
        }

        var postsPinned = category.Posts
            .AsReadOnly()
            .Where(p => p.isPinned)
            .OrderByDescending(p => p.DateCreated)
            .Select(p => new Post
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Description = p.Description,
                DateCreated = p.DateCreated.ToString("dd/MM/yyyy"),
                DateUpdated = p.DateUpdated.ToString("dd/MM/yyyy"),
                NumLikes = p.NumLikes,
                IsPinned = p.isPinned,
                AuthorId = p.AuthorId,
                Author = p.Author,
                AvatarPath = p.AvatarPath
            })
            .ToList();

        var posts = category.Posts
            .Where(p => !p.isPinned)
            .AsQueryable();

        posts = (sortOption, sortBy) switch
        {
            ("DateCreated", "asc") => posts.OrderBy(p => p.DateCreated),
            ("DateCreated", "desc") => posts.OrderByDescending(p => p.DateCreated),
            ("DateUpdated", "asc") => posts.OrderBy(p => p.DateUpdated),
            ("DateUpdated", "desc") => posts.OrderByDescending(p => p.DateUpdated),
            ("NumViews", "asc") => posts.OrderBy(p => p.NumViews),
            ("NumViews", "desc") => posts.OrderByDescending(p => p.NumViews),
            ("NumLikes", "asc") => posts.OrderBy(p => p.NumLikes),
            ("NumLikes", "desc") => posts.OrderByDescending(p => p.NumLikes),
            ("NumComments", "asc") => posts.OrderBy(p => p.NumComments),
            ("NumComments", "desc") => posts.OrderByDescending(p => p.NumComments),
            _ => posts.OrderByDescending(p => p.DateCreated)
        };

        // Pagination
        if (posts.Any())
        {
            model.currentPage = Math.Max(currentPage, 1);
            model.totalPosts = posts.Count();
            model.countPages = (int)Math.Ceiling((double)model.totalPosts / model.ITEMS_PER_PAGE);

            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            posts = posts.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                         .Take(model.ITEMS_PER_PAGE);
        }

        model.Category = new Category
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Slug = category.Slug,
            ParentCategory = category.ParentCategory ?? new Category()
        };

        model.PostsPinned = postsPinned;
        model.Posts = posts.Select(p => new Post
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = p.Title,
            Description = p.Description,
            DateCreated = p.DateCreated.ToString("dd/MM/yyyy"),
            DateUpdated = p.DateUpdated.ToString("dd/MM/yyyy"),
            NumLikes = p.NumLikes,
            NumViews = p.NumViews,
            IsPinned = p.isPinned,
            AuthorId = p.AuthorId,
            Author = p.Author,
            AvatarPath = p.AvatarPath
        }).ToList();

        ViewBag.SortOption = sortOption;
        ViewBag.SortBy = sortBy;

        return View(model);
    }

    // GET: /stories/LatestPosts
    [HttpGet("/stories/LatestPosts")]
    public async Task<IActionResult> LatestPosts([FromQuery(Name = "p")] int currentPage)
    {
        var model = new CategoryPostsViewModel();

        var Posts = _userBlockService.GetFilteredPosts(_dbContext.Posts);

        var posts = Posts
            .AsNoTracking()
            .OrderByDescending(p => p.DateCreated).Take(50)
            .Select(p => new Post
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Description = TrimDescription(p.Description, 150),
                DateCreated = p.DateCreated.ToString("dd/MM/yyyy"),
                NumLikes = p.Likes.Count(),
                NumViews = p.NumViews,
                AuthorId = p.AuthorId,
                Author = p.User.UserName,
                AvatarPath = _dbContext.Images
                    .Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                    .Select(i => i.FilePath)
                    .FirstOrDefault() ?? "/images/no_avt.jpg"
            });

        // Pagination
        if (posts.Any())
        {
            model.currentPage = Math.Max(currentPage, 1);
            model.totalPosts = posts.Count();
            model.countPages = (int)Math.Ceiling((double)model.totalPosts / model.ITEMS_PER_PAGE);

            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            posts = posts.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                         .Take(model.ITEMS_PER_PAGE);
        }

        model.Posts = await posts.ToListAsync();

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
    //GET: /maincontent/Thumbnail
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
        var generatedImageBytes = _thumbnailService.GenerateThumbnail(title, 100, 60);
        return File(generatedImageBytes, "image/png");
    }

    //GET: /maincontent/SearchPosts
    [HttpGet]
    public async Task<IActionResult> SearchPosts([FromQuery(Name = "p")] int currentPage, string query, string searchCategory)
    {
        ViewBag.Query = query;
        ViewBag.SearchCategory = searchCategory;

        var model = new CategoryPostsViewModel();
        var posts = _userBlockService.GetFilteredPosts(_dbContext.Posts);

        if (string.IsNullOrWhiteSpace(query))
        {
            return View("SearchResults", model);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            switch (searchCategory)
            {
                case "title":
                    posts = posts.Where(p => p.Title.Contains(query));
                    break;
                case "description":
                    posts = posts.Where(p => p.Description.Contains(query));
                    break;
                case "content":
                    posts = posts.Where(p => p.Content.Contains(query));
                    break;
                case "author":
                    posts = posts.Where(p => p.User.UserName.Contains(query));
                    break;
                default:
                    posts = posts.Where(p => p.Title.Contains(query) || p.Content.Contains(query));
                    break;
            }
        }

        if (posts.Any())
        {
            model.currentPage = Math.Max(currentPage, 1);
            model.totalPosts = posts.Count();
            model.countPages = (int)Math.Ceiling((double)model.totalPosts / model.ITEMS_PER_PAGE);

            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            posts = posts.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                         .Take(model.ITEMS_PER_PAGE);
        }

        model.Posts = await posts.Select(p => new Post
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = p.Title,
            Description = p.Description,
            DateCreated = p.DateCreated.ToString("dd/MM/yyyy"),
            NumLikes = p.Likes.Count(),
            NumViews = p.NumViews,
            AuthorId = p.AuthorId,
            Author = p.User.UserName,
            AvatarPath = _dbContext.Images
                .Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                .Select(i => i.FilePath)
                .FirstOrDefault() ?? "/images/no_avt.jpg"
        }).ToListAsync();
        return View("SearchResults", model);
    }
}
