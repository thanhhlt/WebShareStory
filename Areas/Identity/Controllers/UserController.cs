// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using App.Areas.Identity.Models.UserViewModels;
using App.ExtendMethods;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Identity.Controllers
{

    // [Authorize(Roles = RoleName.Administrator)]
    [Area("Identity")]
    [Route("/manage-user/[action]")]
    public class UserController : Controller
    {
        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _dbContext;

        private readonly UserManager<AppUser> _userManager;
        private readonly IDeleteUserService _deleteUser;

        public UserController(
            ILogger<RoleController> logger, 
            RoleManager<IdentityRole> roleManager, 
            AppDbContext dbContext, 
            UserManager<AppUser> userManager,
            IDeleteUserService deleteUser)
        {
            _logger = logger;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _userManager = userManager;
            _deleteUser = deleteUser;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        // GET: /manageUser
        [HttpGet("/manage-user")]
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, [Bind("SearchString")] UserListModel model)
        {
            model.currentPage = currentPage;

            var qr = _userManager.Users.AsQueryable();
            if (!string.IsNullOrEmpty(model.SearchString))
            {
                var qrSearch = qr;
                DateTime searchDate;
                bool isDate = DateTime.TryParseExact(
                    model.SearchString,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out searchDate);

                if (isDate)
                {
                    var startOfDay = searchDate.Date;
                    var endOfDay = searchDate.Date.AddDays(1);

                    qrSearch = qrSearch.Where(u => u.AccountCreationDate >= startOfDay && u.AccountCreationDate < endOfDay);
                }
                else
                {
                    qrSearch = qrSearch.Where(u => u.Id.Contains(model.SearchString) ||
                                        u.UserName.Contains(model.SearchString) ||
                                        u.Email.Contains(model.SearchString));
                }
                if (!qrSearch.Any())
                {
                    model.MessageSearchResult = "Không tìm thấy tài khoản nào.";
                }
                else
                {
                    qr = qrSearch;
                }
            }
            qr = qr.OrderBy(u => u.UserName);

            model.totalUsers = await qr.CountAsync();
            model.countPages = (int)Math.Ceiling((double)model.totalUsers / model.ITEMS_PER_PAGE);

            if (model.currentPage < 1)
                model.currentPage = 1;
            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            var qrView = qr.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                        .Take(model.ITEMS_PER_PAGE)
                        .Select(u => new UserIndex()
                        {
                            Id = u.Id,
                            UserName = u.UserName,
                            Email = u.Email,
                            AccountCreated = u.AccountCreationDate
                        });

            model.users = await qrView.ToListAsync();

            return View(model);
        }

        //GET: /manageUser/ManageUser
        [HttpGet("/manage-user/{id}")]
        public async Task<IActionResult> ManageUserAsync(string id)
        {
            if (id == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }
            var user = await _userManager.Users.Where(u => u.Id == id)
                            .Include(u => u.Posts)
                            .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }

            //Get user info
            var userInfo = new UserInfoModel()
            {
                Id = id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Gender = user.Gender == Gender.Male ? "Nam"
                        : user.Gender == Gender.Female ? "Nữ"
                        : user.Gender == Gender.Unspecified ? "Không xác định"
                        : "",
                BirthDate = user.BirthDate,
                Address = user.Address,
                isActivate = user.isActivate,
                AccountCreated = user.AccountCreationDate,
                AccountLockEnd = user.LockoutEnd,
                PostLockEnd = user.PostLockEnd,
                CommentLockEnd = user.CommentLockEnd,
                AvatarPath = await _dbContext.Images.Where(i => i.UserId == user.Id && i.UseType == UseType.profile)
                                                .Select(i => i.FilePath).FirstOrDefaultAsync() ?? "/images/no_avt.jpg"
            };

            //Get post info
            var posts = new List<PostInfoModel>();
            foreach (var post in user.Posts)
            {
                var postInfoModel = new PostInfoModel()
                {
                    Id = post.Id,
                    Title = post.Title,
                    DateCreated = post.DateCreated,
                    DateUpdated = post.DateUpdated,
                    Category = await (from c in _dbContext.Categories
                                    join p in _dbContext.Posts on c.Id equals p.CategoryId
                                    where p.Id == post.Id
                                    select c.Name).FirstOrDefaultAsync(),
                    Slug = post.Slug
                };
                posts.Add(postInfoModel);
            }
            
            //Get role info
            var roles = from ur in _dbContext.UserRoles
                        join r in _dbContext.Roles on ur.RoleId equals r.Id
                        where ur.UserId == user.Id
                        select r;
            var userRoleNames = await roles.Select(r => r.Name).ToArrayAsync();
            var allRoleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            // Get Claim
            var claims = await (from rc in _dbContext.RoleClaims
                        join r in roles on rc.RoleId equals r.Id
                        select new RoleClaimModel
                        {
                            RoleName = r.Name,
                            ClaimType = rc.ClaimType,
                            ClaimValue = rc.ClaimValue
                        }).ToListAsync();

            var model = new ManageUserModel()
            {
                UserId = id,
                UserInfo = userInfo,
                Posts = posts,
                UserRoleNames = userRoleNames,
                AllRoleNames = new SelectList(allRoleNames),
                Claims = claims
            };
            return View(model);
        }

        //POST: /manageUser/UpdateRoleUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoleUserAsync([Bind("UserId", "UserRoleNames")]ManageUserModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }

            //Remove old role
            var userRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == model.UserId).ToListAsync();
            if (userRoles.Any())
            {
                _dbContext.UserRoles.RemoveRange(userRoles);
                await _dbContext.SaveChangesAsync();
            }

            // Add Role
            if (model.UserRoleNames != null)
            {
                var roles = await _dbContext.Roles.Where(r => model.UserRoleNames.Contains(r.Name)).ToListAsync();
                var newUserRoles = roles.Select(r => new IdentityUserRole<string>
                {
                    UserId = model.UserId,
                    RoleId = r.Id
                }).ToList();
                await _dbContext.UserRoles.AddRangeAsync(newUserRoles);
                await _dbContext.SaveChangesAsync();
            }
            var rolesView = from ur in _dbContext.UserRoles
                            join r in _dbContext.Roles on ur.RoleId equals r.Id
                            where ur.UserId == model.UserId
                            select r;
            List<RoleClaimModel> claims = await (from rc in _dbContext.RoleClaims
                                                join r in rolesView on rc.RoleId equals r.Id
                                                select new RoleClaimModel
                                                {
                                                    RoleName = r.Name,
                                                    ClaimType = rc.ClaimType,
                                                    ClaimValue = rc.ClaimValue
                                                }).ToListAsync();
            
            StatusMessage = "Đã cập nhật role cho tài khoản.";
            return PartialView("_RoleClaimUserTable", claims);
        }

        //POST: /manageUser/LockAccountOptions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccountOptionsAsync([Bind("UserId, UserInfo")]ManageUserModel model)
        { 
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }

            user.LockoutEnd = model.UserInfo.AccountLockEnd ?? null;
            user.PostLockEnd = model.UserInfo.PostLockEnd ?? null;
            user.CommentLockEnd = model.UserInfo.CommentLockEnd ?? null;

            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var timeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            if (model.UserInfo.AccountLockEnd != null && model.UserInfo.AccountLockEnd > new DateTimeOffset(timeInVietnam))
            {
                user.AccessFailedCount = 0;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Cập nhật thông tin khoá tài khoản thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new {success = false});
            }

            if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            UserInfoModel userInfo = new UserInfoModel()
            {
                AccountLockEnd = user.LockoutEnd,
                PostLockEnd = user.PostLockEnd,
                CommentLockEnd = user.CommentLockEnd
            };

            StatusMessage = "Đã cập nhật thông tin khoá tài khoản";
            return PartialView("_LockInfo", userInfo);
        }

        //POST: /manageUser/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordAsync(string userId)
        {
            _logger.LogError(string.Empty, userId);
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, userId);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Đặt lại mật khẩu thất bại.";
                return RedirectToAction(nameof(Index));
            }

            StatusMessage = "Đã đặt lại mật khẩu.";
            return Json(new{success = true});
        }

        //POST: /manageUser/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }

            var result = await _deleteUser.DeleteUserAsync(user.Id);
            if (!result)
            {
                StatusMessage = "Error Xoá tài khoản thất bại.";
                return Json(new{success = false});
            }

            StatusMessage = "Đã xoá tài khoản.";
            return Json(new{success = true, redirect = Url.Action("Index")});
        }
    }
}
