#nullable disable

using System.ComponentModel.DataAnnotations;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Linq.Dynamic.Core;

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

    public class Comment : CommentsModel
    {
        public string UserName { get; set; }
        public string AvatarPath { get; set; }
    }
    public class IndexViewModel : PostsModel
    {
        public string Author { get; set; }
        public string PathAvatar { get; set; }
        public string CateName { get; set; }
        public bool isLiked { get; set; }
        public int NumLikes { get; set; }
        public int NumComments { get; set; }
        public List<Comment> CommentsView { get; set; }
    }

    //GET: /{slugPost}
    [HttpGet("/{slugPost}")]
    public async Task<IActionResult> Index(string slugPost)
    {
        if (string.IsNullOrEmpty(slugPost))
        {
            return NotFound("Không tìm thấy bài viết.");
        }
        IndexViewModel model = await _dbContext.Posts.AsNoTracking()
                                            .Where(p => p.Slug == slugPost)
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
                                                Author = p.User.UserName ?? "Vô danh",
                                                PathAvatar = _dbContext.Images.Where(i => i.UserId == p.AuthorId &&
                                                                                            i.UseType == UseType.profile)
                                                                .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
                                                isPinned = p.isPinned,
                                                NumLikes = p.Likes.Count(),
                                                NumComments = p.Comments.Count(),
                                            }).FirstOrDefaultAsync();
        if (model == null)
        {
            return NotFound("Không tìm thấy bài viết.");
        }

        // Comments
        // var allComments = await _dbContext.Comments.AsNoTracking()
        //     .Where(c => c.PostId == model.Id).OrderByDescending(c => c.DateCommented)
        //     .Include(c => c.ChildComments)
        //     .Include(c => c.User)
        //     .ToListAsync();

        // model.CommentsView = BuildCommentTree(allComments);

        var userId = (await _userManager.GetUserAsync(User))?.Id;
        if (userId == null)
        {
            model.isLiked = false;
            return View(model);
        }
        model.isLiked = _dbContext.Likes.AsNoTracking().Where(l => l.UserId == userId && l.PostId == model.Id).FirstOrDefault() != null ? true : false;

        return View(model);
    }

    // GET: /GetAllComments/{id}
    [HttpGet]
    public async Task<IActionResult> GetAllCommentsAsync(int postId)
    {
        var allComments = await _dbContext.Comments.AsNoTracking()
            .Where(c => c.PostId == postId).OrderByDescending(c => c.DateCommented)
            .Include(c => c.ChildComments)
            .Include(c => c.User)
            .ToListAsync();

        var model = BuildCommentTree(allComments);
        return Json(model);
    }
    private List<Comment> BuildCommentTree(List<CommentsModel> allComments)
    {
        var commentDict = allComments.ToDictionary(c => c.Id, c => c);
        var result = new List<Comment>();

        foreach (var comment in allComments.Where(c => c.ParentCommentId == null))
        {
            result.Add(BuildCommentWithReplies(comment, commentDict));
        }

        return result;
    }
    private Comment BuildCommentWithReplies(CommentsModel comment, Dictionary<int, CommentsModel> commentDict)
    {
        var commentView = new Comment
        {
            Id = comment.Id,
            Content = comment.Content,
            DateCommented = comment.DateCommented,
            UserId = comment.UserId,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            UserName = comment.User.UserName ?? "Vô danh",
            AvatarPath = _dbContext.Images
                                    .Where(i => i.UserId == comment.UserId && i.UseType == UseType.profile)
                                    .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
            ChildComments = new List<CommentsModel>()
        };

        var childComments = commentDict.Values.Where(c => c.ParentCommentId == comment.Id).OrderByDescending(c => c.DateCommented).ToList();

        foreach (var childComment in childComments)
        {
            commentView.ChildComments.Add(BuildCommentWithReplies(childComment, commentDict));
        }

        return commentView;
    }

    //POST: /CommentPost/{id, content}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CommentPostAsync(int id, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogError(string.Empty, "Lỗi comment");
            return Json(new { success = false });
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogError(string.Empty, "Không tìm thấy tài khoản");
            return Json(new { success = false });
        }

        var comment = new CommentsModel
        {
            Content = content,
            DateCommented = DateTime.UtcNow,
            PostId = id,
            UserId = user.Id
        };
        await _dbContext.Comments.AddAsync(comment);
        await _dbContext.SaveChangesAsync();

        var model = new Comment
        {
            Id = comment.Id,
            Content = comment.Content,
            DateCommented = comment.DateCommented,
            UserId = comment.UserId,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            UserName = comment.User.UserName ?? "Vô danh",
            AvatarPath = _dbContext.Images
                                    .Where(i => i.UserId == comment.UserId && i.UseType == UseType.profile)
                                    .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
        };
        return PartialView("_CommentPartial", model);
    }

    //POST: /ReplyComment/{content, postId, parentId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyCommentAsync(int postId, int parentId, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogError(string.Empty, "Lỗi comment");
            return Json(new { success = false });
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogError(string.Empty, "Không tìm thấy tài khoản");
            return Json(new { success = false });
        }

        var comment = new CommentsModel
        {
            Content = content,
            DateCommented = DateTime.UtcNow,
            PostId = postId,
            UserId = user.Id,
            ParentCommentId = parentId
        };
        await _dbContext.Comments.AddAsync(comment);
        await _dbContext.SaveChangesAsync();

        var model = new Comment
        {
            Id = comment.Id,
            Content = comment.Content,
            DateCommented = comment.DateCommented,
            UserId = comment.UserId,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            UserName = comment.User.UserName ?? "Vô danh",
            AvatarPath = _dbContext.Images
                                    .Where(i => i.UserId == comment.UserId && i.UseType == UseType.profile)
                                    .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
        };
        return PartialView("_CommentPartial", model);
    }

    //POST: /LikePost/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LikePostAsync(int? id)
    {
        if (id == null)
        {
            _logger.LogError(string.Empty, "Không tìm thấy bài viết");
            return Json(new { success = false });
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogError(string.Empty, "Không tìm thấy tài khoản");
            return Json(new { success = false });
        }
        var like = await _dbContext.Likes.Where(l => l.UserId == user.Id && l.PostId == id).FirstOrDefaultAsync();
        if (like == null)
        {
            like = new LikesModel {
                LikeType = LikeTypes.Post,
                DateLiked = DateTime.UtcNow,
                UserId = user.Id,
                PostId = id,
                User = user
            };
            await _dbContext.Likes.AddAsync(like);
        }
        else
        {
             _dbContext.Likes.Remove(like);
        }
        await _dbContext.SaveChangesAsync();

        var numLikes = _dbContext.Likes.Count(l => l.PostId == id);
        var numLikesFormatted = NumberFormatter.FormatNumber(numLikes);

        return Json(new { success = true, numLikesFormatted });
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
        var allCates = await _dbContext.Categories
                                    .Include(c => c.ParentCate)
                                    .Where(c => c.ParentCate != null)
                                    .Select(c => c.Name).ToListAsync();
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
            return Json(new { success = false });
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
                return Json(new { success = false });
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

        return Json(new { success = true, redirect = Url.Action("Index", new { slugPost = post.Slug }) });
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
        var allCategories = await _dbContext.Categories
                                    .Include(c => c.ParentCate)
                                    .Where(c => c.ParentCate != null)
                                    .Select(c => c.Name).ToListAsync();
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
            return Json(new { success = false });
        }
        var post = await _dbContext.Posts.Where(p => p.Id == model.Id).FirstOrDefaultAsync();
        if (post == null)
        {
            StatusMessage = "Không tìm thấy bài viết.";
            return Json(new { success = false });
        }

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var timeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

        var category = await _dbContext.Categories.Where(c => c.Name == model.CategoryName)
                                                    .Select(c => new { c.Id, c.Slug }).FirstOrDefaultAsync();

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
                return Json(new { success = false });
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
        return Json(new { success = true, redirect = Url.Action("Index", new { slugPost = post.Slug }) });
    }

    //POST: /DeletePost/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePostAsync(int? id)
    {
        if (id == null)
        {
            _logger.LogError(string.Empty, "Không tìm tấy bài viết");
            return Json(new { success = false });
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

        return Json(new { success = true, redirect = Url.Action("Index", "Home") });
    }

    //POST: /PinPost/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PinPostAsync(int? id)
    {
        if (id == null)
        {
            _logger.LogError(string.Empty, "Không tìm thấy bài viết");
            return Json(new { success = false });
        }
        var post = await _dbContext.Posts.Where(p => p.Id == id).FirstOrDefaultAsync();
        if (post == null)
        {
            _logger.LogError(string.Empty, "Bài viết không tồn tại");
            return Json(new { success = false });
        }

        post.isPinned = !post.isPinned;
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, isPinned = post.isPinned });
    }
}