#nullable disable

using System.Globalization;
using App.Areas.Post.Models.Post;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Post.Controllers;

[Area("Post")]
[Route("/[action]")]
[Authorize(Policy = "CanManagePost")]
public class PostController : Controller
{
    private readonly ILogger<PostController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public PostController(
        ILogger<PostController> logger,
        AppDbContext dbContext,
        IWebHostEnvironment environment
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _environment = environment;
    }

    [TempData]
    public string StatusMessage { get; set; }

    public IActionResult GetStatusMessage()
    {
        return PartialView("_StatusMessage");
    }

    //GET: /admin/post
    [HttpGet("/admin/post")]
    public async Task<IActionResult> ManagePostAsync ([FromQuery(Name = "p")] int currentPage, [Bind("SearchString")] ManagePostModel model)
    {
        var qr = from p in _dbContext.Posts
                    join u in _dbContext.Users on p.AuthorId equals u.Id into usersGroup
                    from u in usersGroup.DefaultIfEmpty()
                    join c in _dbContext.Categories on p.CategoryId equals c.Id into catesGroup
                    from c in catesGroup.DefaultIfEmpty()
                    select new PostView
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Author = u.UserName,
                        CategoryName = c.Name,
                        DateCreated = p.DateCreated,
                        DateUpdated = p.DateUpdated,
                        Content = "",
                        Slug = p.Slug,
                        SlugCate = c.Slug
                    };

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

                qrSearch = qrSearch.Where(p => p.DateCreated >= startOfDay && p.DateCreated < endOfDay || 
                                               p.DateUpdated >= startOfDay && p.DateUpdated < endOfDay);
            }
            else
            {
                qrSearch = qrSearch.Where(p => p.Title.Contains(model.SearchString) || 
                                                p.Author.Contains(model.SearchString) ||
                                                p.CategoryName.Contains(model.SearchString));
            }
            if (!qrSearch.Any())
            {
                model.MessageSearchResult = "Không tìm thấy bài viết nào.";
            }
            else
            {
                qr = qrSearch;
            }
        }

        model.currentPage = currentPage;
        model.totalPosts = await qr.CountAsync();
        model.countPages = (int)Math.Ceiling((double)model.totalPosts / model.ITEMS_PER_PAGE);

        if (model.currentPage < 1)
            model.currentPage = 1;
        if (model.currentPage > model.countPages)
            model.currentPage = model.countPages;

        qr = qr.OrderByDescending(p => p.DateUpdated);
        var qrView = qr.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                    .Take(model.ITEMS_PER_PAGE);

        model.Posts = await qrView.ToListAsync();
        return View(model);
    }

    public class ListPostId
    {
        public List<int> postIds { get; set; }
    }

    //POST: /admin/post/DeleteMultiPosts
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMultiPostsAsync ([FromBody] ListPostId Ids)
    {
        if (Ids.postIds == null)
        {
            StatusMessage = "Error Không có bài viết nào.";
            return Json(new{success = false, redirect=Url.Action("ManagePost")});
        }

        var posts = await _dbContext.Posts.Include(p => p.Image).Where(p => Ids.postIds.Contains(p.Id)).ToListAsync();

        if (!posts.Any())
        {
            StatusMessage = "Error Không tìm thấy bài viết nào.";
            return Json(new{success = false, redirect=Url.Action("ManagePost")});
        }

        foreach (var post in posts)
        {
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
            }
        }
        
        StatusMessage = "Đã xoá những bài viết đã chọn.";
        ManagePostModel model = new ManagePostModel();
        return Json(new{success = true, redirect=Url.Action("ManagePost")});
    }
}