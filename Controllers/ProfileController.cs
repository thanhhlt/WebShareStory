#nullable disable

using System.Linq.Dynamic.Core;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

[Route("/profile/[action]")]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public ProfileController(
        ILogger<ProfileController> logger,
        AppDbContext dbContext,
        UserManager<AppUser> userManager
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
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
        var user = await _dbContext.Users.Where(u => u.Id == id).Include(u => u.Posts)
                                        .Select(u => new{u.Id, u.UserName, u.Introduction, u.isActivate, u.Gender, u.BirthDate, u.Address, u.Posts})
                                        .FirstOrDefaultAsync();
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

        if (user.Posts != null && user.Posts.Any())
        {
            model.PostList.currentPage = currentPage;
            model.PostList.totalPosts = user.Posts.Count();
            model.PostList.countPages = (int)Math.Ceiling((double)model.PostList.totalPosts / model.PostList.ITEMS_PER_PAGE);

            if (model.PostList.currentPage < 1)
                model.PostList.currentPage = 1;
            if (model.PostList.currentPage > model.PostList.countPages)
                model.PostList.currentPage = model.PostList.countPages;

            model.Posts = user.Posts.OrderByDescending(p => p.DateCreated)
                                    .Skip((model.PostList.currentPage - 1) * model.PostList.ITEMS_PER_PAGE)
                                    .Take(model.PostList.ITEMS_PER_PAGE)
                                    .Select(p => new PostsModel{
                                        Id = p.Id,
                                        Title = p.Title,
                                        Content = p.Content,
                                        DateCreated = p.DateCreated,
                                        DateUpdated = p.DateUpdated
                                    }).ToList();
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

    //POST: /profile/UpdateRelation
    [HttpPost]
    public async Task<IActionResult> UpdateRelationAsync(string userId, string otheruserId, RelationshipStatus status)
    {
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
    public async Task<IActionResult> ListUserFollowAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound("Không tìm thấy tài khoản.");
        }
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        return View(await GetListUserRelations(id, RelationshipStatus.Follow));
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
    public async Task<IActionResult> ListUserBlockAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound("Không tìm thấy tài khoản.");
        }
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        return View(await GetListUserRelations(id, RelationshipStatus.Block));
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
}