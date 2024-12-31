#nullable disable

using System.ComponentModel.DataAnnotations;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace App.Controllers;

[Route("/[action]")]
public class PostController : Controller
{
    private readonly ILogger<PostController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public PostController(
        ILogger<PostController> logger,
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IWebHostEnvironment environment
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _environment = environment;
    }

    [TempData]
    public string StatusMessage { get; set; }

    public IActionResult GetStatusMessage()
    {
        return PartialView("_StatusMessage");
    }

    public class IndexViewModel : PostsModel
    {
        public string Author { get; set; }
        public string PathAvatar { get; set; }
        public string CateName { get; set; }
    }

    //GET: /{slugCate}/{slugPost}
    [HttpGet("/{slugPost}")]
    public async Task<IActionResult> Index(string slugPost)
    {
        if (string.IsNullOrEmpty(slugPost))
        {
            return NotFound("Không tìm thấy bài viết.");
        }
        IndexViewModel model = await _dbContext.Posts.Where(p => p.Slug == slugPost)
                                                    .Include(p => p.User)
                                                    .Include(p => p.Category)
                                                    .Include(p => p.Image)
                                                    .Select(p => new IndexViewModel
                                                    {
                                                        Id = p.Id,
                                                        AuthorId = p.AuthorId,
                                                        CateName = p.Category.Name,
                                                        Title = p.Title,
                                                        DateCreated = p.DateCreated,
                                                        DateUpdated = p.DateUpdated,
                                                        Hashtag = p.Hashtag,
                                                        Content = p.Content,
                                                        Slug = slugPost,
                                                        User = p.User
                                                    }).FirstOrDefaultAsync();
        if (model == null)
        {
            return NotFound("Không tìm thấy bài viết.");
        }
        if (model.User == null)
        {
            model.Author = "Vô danh";
            model.PathAvatar = "/images/no_avt.jpg";
        }
        else
        {
            model.AuthorId = model.User.Id;
            model.Author = model.User.UserName;
            model.PathAvatar = await _dbContext.Images.Where(i => i.UserId == model.User.Id && 
                                                            i.UseType == UseType.profile)
                                                        .Select(i => i.FilePath).FirstOrDefaultAsync() ?? "/images/no_avt.jpg";
        }
        return View(model);
    }

    public class EditCreateModel
    {
        public int Id { get; set; }

        [Display(Name = "Tên bài viết")]
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [MaxLength(255, ErrorMessage = "{0} dài không quá {1} ký tự.")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }
        
        [Display(Name = "Nội dung")]
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        public string Content { get; set; }

        [Display(Name = "HashTags")]
        [MaxLength(50, ErrorMessage = "{0} dài không quá {1} ký tự.")]
        public string HashTags { get; set; }

        public string CategoryName { get; set; }

        public SelectList AllCategories { get; set; }

        public string PathThumbnail { get; set; }

        [Display(Name = "Ảnh thumbnail minh hoạ")]
        public IFormFile ImageThumbnail { get; set; }
    }

    //GET: /CreatePost
    [HttpGet]
    public async Task<IActionResult> CreatePost()
    {
        var allCates = await _dbContext.Categories.Select(c => c.Name).ToListAsync();
        EditCreateModel model = new EditCreateModel()
        {
            AllCategories = new SelectList(allCates),
        };
        return View(model);
    }

    //POST: /CreaePost
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePostAsync(EditCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            StatusMessage = $"Error Tạo bài viết thất bại.<br/> {string.Join("<br/>", errorMessages)}";
            return Json(new {success = false});
        }

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var timeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        PostsModel post = new PostsModel()
        {
            AuthorId = (await _userManager.GetUserAsync(User)).Id,
            Title = model.Title,
            Description = model.Description,
            Content = model.Content,
            Hashtag = model.HashTags,
            CategoryId = await _dbContext.Categories.Where(c => c.Name == model.CategoryName)
                                                    .Select(c => c.Id).FirstOrDefaultAsync(),
            DateCreated = timeInVietnam,
            DateUpdated = timeInVietnam,
            Slug = "",
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        post.SetSlug();

        if (model.ImageThumbnail != null && model.ImageThumbnail.Length > 0)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var fileExtension = Path.GetExtension(model.ImageThumbnail.FileName).ToLower();

            if (!extensions.Contains(fileExtension))
            {
                StatusMessage = "Error Chỉ cho phép ảnh .jpg, .jpeg, .png, .webp, .gif";
                return Json(new{success = false});
            }

            string directoryPath = Path.Combine(_environment.ContentRootPath, "Images/Posts");
            var filePath = Path.Combine(directoryPath, post.Id + fileExtension);

            var existingFiles = Directory.GetFiles(directoryPath)
                                        .Where(f => Path.GetFileNameWithoutExtension(f) == post.Id.ToString())
                                        .ToList();
            foreach (var file in existingFiles)
            {
                System.IO.File.Delete(file);
            }

            using (var image = await Image.LoadAsync(model.ImageThumbnail.OpenReadStream()))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(350, 210)
                }));

                await image.SaveAsync(filePath);
            }

            filePath = "/imgs/Posts/" + post.Id + fileExtension;

            var img = await _dbContext.Images.Where(i => i.PostId == post.Id && i.UseType == UseType.post).FirstOrDefaultAsync();
            if (img != null)
            {
                img.FileName = model.ImageThumbnail.FileName;
                img.FilePath = filePath;
            }
            else
            {
                var image = new ImagesModel()
                {
                    FileName = model.ImageThumbnail.FileName,
                    FilePath = filePath,
                    UseType = UseType.post,
                    PostId = post.Id
                };
                _dbContext.Images.Add(image);
            }
        }

        await _dbContext.SaveChangesAsync();

        return Json(new{success = true, redirect = Url.Action("Index", new { slugPost = post.Slug})});
    }

    //GET: /EditPost/{id}
    [HttpGet]
    public async Task<IActionResult> EditPostAsync(int id)
    {
        EditCreateModel model = await _dbContext.Posts.Where(p => p.Id == id)
                                                    .Include(p => p.Category)
                                                    .Include(p => p.Image)
                                                    .Select(p => new EditCreateModel
                                                    {
                                                        Id = id,
                                                        Title = p.Title,
                                                        Description = p.Description,
                                                        Content = p.Content,
                                                        HashTags = p.Hashtag,
                                                        CategoryName = p.Category.Name,
                                                        PathThumbnail = p.Image.FilePath ?? null
                                                    }).FirstOrDefaultAsync();
        if (model == null)
        {
            return NotFound("Không tìm thấy bài viết.");
        }
        var allCategories = await _dbContext.Categories.Select(c => c.Name).ToListAsync();
        model.AllCategories = new SelectList(allCategories);

        return View(model);
    }

    //POST: /EditPost/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPostAsync(EditCreateModel model, bool thumbnailDeleted)
    {
        if (!ModelState.IsValid)
        {
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            StatusMessage = $"Error Chỉnh sửa bài viết thất bại.<br/> {string.Join("<br/>", errorMessages)}";
            return Json(new {success = false});
        }
        var post = await _dbContext.Posts.Where(p => p.Id == model.Id).FirstOrDefaultAsync();
        if (post == null)
        {
            StatusMessage = "Không tìm thấy bài viết.";
            return Json(new{success = false});
        }

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var timeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

        var category = await _dbContext.Categories.Where(c => c.Name == model.CategoryName)
                                                    .Select(c => new {c.Id, c.Slug}).FirstOrDefaultAsync();
        
        post.CategoryId = category.Id;
        post.Title = model.Title;
        post.Description = model.Description;
        post.Content = model.Content;
        post.Hashtag = model.HashTags;
        post.DateUpdated = timeInVietnam;
        post.SetSlug();

        var img = await _dbContext.Images.Where(i => i.PostId == post.Id && i.UseType == UseType.post).FirstOrDefaultAsync();
        if (thumbnailDeleted)
        {
            if (img != null)
            {
                if (img.FilePath.StartsWith("/imgs/"))
                {
                    var filePath = Path.Combine(_environment.ContentRootPath, "Images/" + img.FilePath.Substring(6));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _dbContext.Images.Remove(img);
                img = null;
                await _dbContext.SaveChangesAsync();
            }
        }

        if (model.ImageThumbnail != null && model.ImageThumbnail.Length > 0)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var fileExtension = Path.GetExtension(model.ImageThumbnail.FileName).ToLower();

            if (!extensions.Contains(fileExtension))
            {
                StatusMessage = "Error Chỉ cho phép ảnh .jpg, .jpeg, .png, .webp, .gif";
                return Json(new{success = false});
            }

            string directoryPath = Path.Combine(_environment.ContentRootPath, "Images/Posts");
            var filePath = Path.Combine(directoryPath, post.Id + fileExtension);

            var existingFiles = Directory.GetFiles(directoryPath)
                                        .Where(f => Path.GetFileNameWithoutExtension(f) == post.Id.ToString())
                                        .ToList();
            foreach (var file in existingFiles)
            {
                System.IO.File.Delete(file);
            }

            using (var image = await Image.LoadAsync(model.ImageThumbnail.OpenReadStream()))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(350, 210)
                }));

                await image.SaveAsync(filePath);
            }

            filePath = "/imgs/Posts/" + post.Id + fileExtension;

            if (img != null)
            {
                img.FileName = model.ImageThumbnail.FileName;
                img.FilePath = filePath;
            }
            else
            {
                var image = new ImagesModel()
                {
                    FileName = model.ImageThumbnail.FileName,
                    FilePath = filePath,
                    UseType = UseType.post,
                    PostId = post.Id
                };
                _dbContext.Images.Add(image);
            }
        }

        await _dbContext.SaveChangesAsync();
        return Json(new{success = true, redirect = Url.Action("Index", new { slugPost = post.Slug})});
    }

    //POST: /DeletePost/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePostAsync(int? id)
    {
        if (id == null)
        {
            _logger.LogError(string.Empty, "Không tìm tấy bài viết");
            return Json(new{success = false});
        }
        var post = await _dbContext.Posts.Where(p => p.Id == id)
                                        .Include(p => p.Image).FirstOrDefaultAsync();
        if (post == null)
        {
            _logger.LogError(string.Empty, "Bài viết không tồn tại");
            return Json(new { success = false });
        }

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();

        if (post.Image != null)
        {
            if (post.Image.FilePath.StartsWith("/imgs/"))
            {
                var filePath = Path.Combine(_environment.ContentRootPath, "Images/" + post.Image.FilePath.Substring(6));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            _dbContext.Images.Remove(post.Image);
        }

        return Json(new{success = true, redirect = Url.Action("Index", "Home")});
    }
}