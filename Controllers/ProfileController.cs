#nullable disable

using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using App.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

[Authorize]
[Route("/profile/[action]")]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IThumbnailService _thumbnailService;
    private readonly IWebHostEnvironment _env;
    private readonly IUserBlockService _userBlockService;

    public ProfileController(
        ILogger<ProfileController> logger,
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IThumbnailService thumbnailService,
        IWebHostEnvironment env,
        IUserBlockService userBlockService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _thumbnailService = thumbnailService;
        _env = env;
        _userBlockService = userBlockService;
    }

    public class PostListModel
    {
        public int totalPosts { get; set; }
        public int countPages { get; set; }

        public int ITEMS_PER_PAGE { get; set; } = 10;

        public int currentPage { get; set; }
        // public List<PostsModel>? Posts { get; set; }
    }
    public class IndexViewModel : AppUser
    {
        public bool isLogin { get; set; }
        public bool isOwnProfile { get; set; }
        public string sGender { get; set; }
        public RelationshipStatus? Relationship { get; set; }
        public string AvatarPath { get; set; }
        public string UserId { get; set; }
        public string OtherUserId { get; set; }
        public PostListModel PostList { get; set; } = new PostListModel();
    }

    [HttpGet("/profile/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        if (_userBlockService.IsBlockedUser(id))
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        var model = new IndexViewModel()
        {
            OtherUserId = user.Id,
            UserName = user.UserName,
            Introduction = user.Introduction,
            isActivate = user.isActivate,
            sGender = user.Gender == Gender.Male ? "Nam"
                    : user.Gender == Gender.Female ? "Nữ"
                    : user.Gender == Gender.Unspecified ? "Không xác định"
                    : "",
            BirthDate = user.BirthDate,
            Address = user.Address,
            AvatarPath = _dbContext.Images?.Where(i => i.UserId == user.Id && i.UseType == UseType.profile)
                                            .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
        };

        var userPosts = await _dbContext.Posts.AsNoTracking()
                                .Where(p => p.AuthorId == user.Id)
                                .Select(p => new PostsModel{
                                    Id = p.Id,
                                    Title = p.Title,
                                    Description = TrimDescription(p.Description, 150),
                                    Slug = p.Slug,
                                    DateCreated = p.DateCreated,
                                    DateUpdated = p.DateUpdated
                                }).ToListAsync();
        if (userPosts != null && userPosts.Any())
        {
            model.PostList.currentPage = currentPage;
            model.PostList.totalPosts = userPosts.Count();
            model.PostList.countPages = (int)Math.Ceiling((double)model.PostList.totalPosts / model.PostList.ITEMS_PER_PAGE);

            if (model.PostList.currentPage < 1)
                model.PostList.currentPage = 1;
            if (model.PostList.currentPage > model.PostList.countPages)
                model.PostList.currentPage = model.PostList.countPages;

            model.Posts = userPosts.OrderByDescending(p => p.DateCreated)
                                    .Skip((model.PostList.currentPage - 1) * model.PostList.ITEMS_PER_PAGE)
                                    .Take(model.PostList.ITEMS_PER_PAGE).ToList();
        }

        var userLogin = await _userManager.GetUserAsync(User);
        if (userLogin == null)
        {
            model.isLogin = false;
            model.isOwnProfile = false;
            return View(model);
        }
        model.isLogin = true;
        if (user.Id == userLogin.Id)
        {
            model.isOwnProfile = true;
            return View(model);
        }
        model.UserId = userLogin.Id;
        var userRelation = _dbContext.UserRelations?.Where(ur => ur.UserId == userLogin.Id && ur.OtherUserId == user.Id)
                                                    .Select(ur => new{ur.UserId, ur.Status}).FirstOrDefault();
        if (userRelation == null)
        {
            model.Relationship = RelationshipStatus.None;
            return View(model);
        }
        model.Relationship = userRelation.Status;
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
            return description.Substring(0, maxLength).TrimEnd() + "...";
        }
        return description;
    }

    //GET: /profile/Thumbnail
    [HttpGet]
    [AllowAnonymous]
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
        var generatedImageBytes = _thumbnailService.GenerateThumbnail(title, 250, 150);
        return File(generatedImageBytes, "image/png");
    }

    //POST: /profile/UpdateRelation
    [HttpPost]
    public async Task<IActionResult> UpdateRelationAsync(string otheruserId, RelationshipStatus status)
    {
        var userId = (await _userManager.GetUserAsync(User)).Id;
        if (userId == null || otheruserId == null)
        {
            return Json(new{success = false});
        }
        if (_dbContext.UserRelations == null)
        {
            return Json(new{success = false});
        }
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var timeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        if (status == RelationshipStatus.Follow || status == RelationshipStatus.Block)
        {
            var relationOthertoUser = await _dbContext.UserRelations.Where(ur => ur.UserId == otheruserId && ur.OtherUserId == userId)
                                                                    .FirstOrDefaultAsync();
            if (status == RelationshipStatus.Follow && relationOthertoUser != null && relationOthertoUser.Status == RelationshipStatus.Block)
            {
                return Json(new{success = false});
            }
            if (status == RelationshipStatus.Block && relationOthertoUser != null && relationOthertoUser.Status == RelationshipStatus.Follow)
            {
                relationOthertoUser.Status = RelationshipStatus.None;
                relationOthertoUser.DateCreated = new DateTimeOffset(timeInVietnam);
            }
        }
    
        var relation = await _dbContext.UserRelations.Where(ur => ur.UserId == userId && ur.OtherUserId == otheruserId).FirstOrDefaultAsync();
        if (relation != null)
        {  
            relation.Status = status;
            relation.DateCreated = new DateTimeOffset(timeInVietnam);
        }
        else
        {
            var userRelation = new UserRelationModel()
            {
                UserId = userId,
                OtherUserId = otheruserId,
                Status = status,
                DateCreated = new DateTimeOffset(timeInVietnam),
            };
            _dbContext.Add(userRelation);
        }
        await _dbContext.SaveChangesAsync();

        IndexViewModel model = new IndexViewModel()
        {
            Relationship = status,
            UserId = userId,
            OtherUserId = otheruserId
        };
        return PartialView("_RelationUser", model);
    }

    public class UserRelation
    {
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public string AvatarPath { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
    }

    //GET: /profile/ListUserFollow
    [HttpGet]
    public async Task<IActionResult> ListUserFollowAsync()
    {
        var userId = (await _userManager.GetUserAsync(User)).Id;
        if (userId == null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        return View(await GetListUserRelations(userId, RelationshipStatus.Follow));
    }

    public class ListUserId
    {
        public List<string> userIds { get; set; }
    }
    //POST: profile/UnfollowUsers
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnfollowUsersAsync([FromBody] ListUserId Ids)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new{success = false});
        }
        if (Ids.userIds == null)
        {
            return Json(new{success = false});
        }

        var userRelations = await _dbContext.UserRelations.Where(ur => ur.UserId == user.Id && 
                                                            Ids.userIds.Contains(ur.OtherUserId) &&
                                                            ur.Status == RelationshipStatus.Follow)
                                                    .ToListAsync();
        if (!userRelations.Any())
        {
            return Json(new{success = false});
        }

        foreach (var relation in userRelations)
        {
            relation.Status = RelationshipStatus.None;
        }
        await _dbContext.SaveChangesAsync();
        return PartialView("_ListUserFollow", await GetListUserRelations(user.Id, RelationshipStatus.Follow));
    }

    //GET: /profile/ListUserBlock
    [HttpGet]
    public async Task<IActionResult> ListUserBlockAsync()
    {
        var userId = (await _userManager.GetUserAsync(User)).Id;
        if (userId == null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        return View(await GetListUserRelations(userId, RelationshipStatus.Block));
    }

    //POST: profile/UnblockUsers
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUsersAsync([FromBody] ListUserId Ids)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new{success = false});
        }
        if (Ids.userIds == null)
        {
            return Json(new{success = false});
        }

        var userRelations = await _dbContext.UserRelations.Where(ur => ur.UserId == user.Id && 
                                                            Ids.userIds.Contains(ur.OtherUserId) &&
                                                            ur.Status == RelationshipStatus.Block)
                                                    .ToListAsync();
        if (!userRelations.Any())
        {
            return Json(new{success = false});
        }

        foreach (var relation in userRelations)
        {
            relation.Status = RelationshipStatus.None;
        }
        await _dbContext.SaveChangesAsync();
        return PartialView("_ListUserBlock", await GetListUserRelations(user.Id, RelationshipStatus.Block));
    }

    private async Task<List<UserRelation>> GetListUserRelations(string id, RelationshipStatus status)
    {
        var qr = from u in _dbContext.Users
                join ur in _dbContext.UserRelations on u.Id equals ur.OtherUserId
                join i in _dbContext.Images on ur.OtherUserId equals i.UserId into imagesGroup
                from i in imagesGroup.DefaultIfEmpty()
                where ur.Status == status && ur.UserId == id
                select new UserRelation
                {
                    UserId = ur.OtherUserId,
                    UserName = u.UserName,
                    AvatarPath = i != null ? i.FilePath : "/images/no_avt.jpg",
                    DateCreated = ur.DateCreated
                };
        List<UserRelation> listUserRelations = await qr.ToListAsync();
        return listUserRelations;
    }

    public class PostBookmark
    {
        public int PostId { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DateCreatedPost { get; set; }
        public string DateUpdatedPost { get; set; }
        public string DateCreated { get; set; }
    }
    //GET: /profile/ListPostBookmark
    [HttpGet]
    public async Task<IActionResult> ListPostBookmark(string id)
    {
        var userId = (await _userManager.GetUserAsync(User)).Id;
        if (userId == null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        return View(await GetListPostBookmark(userId));
    }

    public class ListPostId
    {
        public List<int> postIds { get; set; }
    }
    //POST: profile/UnBookmarkPosts
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnBookmarkPostsAsync([FromBody] ListPostId Ids)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new{success = false});
        }
        if (Ids.postIds == null)
        {
            return Json(new{success = false});
        }

        var postBookmarks = await _dbContext.Bookmarks
                                    .Where(b => b.UserId == user.Id && 
                                                Ids.postIds.Contains(b.PostId)).ToListAsync();

        if (!postBookmarks.Any())
        {
            return Json(new{success = false});
        }

        _dbContext.Bookmarks.RemoveRange(postBookmarks);
        await _dbContext.SaveChangesAsync();
        return PartialView("_ListPostBookmark", await GetListPostBookmark(user.Id));
    }

    private async Task<List<PostBookmark>> GetListPostBookmark(string id)
    {
        var postBookmarks = await _dbContext.Bookmarks.AsNoTracking()
                                        .Where(b => b.UserId == id)
                                        .Select(b => new PostBookmark
                                        {
                                            PostId = b.PostId,
                                            Title = b.Post.Title,
                                            Description = TrimDescription(b.Post.Description ?? RemoveImagesAndTags(b.Post.Content ?? ""), 250),
                                            Slug = b.Post.Slug,
                                            DateCreatedPost = b.Post.DateCreated.ToString("dd/MM/yyyy"),
                                            DateUpdatedPost = b.Post.DateUpdated.ToString("dd/MM/yyyy"),
                                            DateCreated = b.DateCreated.ToString("dd/MM/yyyy")
                                        }).ToListAsync();
        return postBookmarks;
    }
}