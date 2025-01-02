#nullable disable

using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Components
{   
    [ViewComponent]
    public class MainContentSidebar : ViewComponent
    {
        private readonly AppDbContext _dbContext;

        public MainContentSidebar(
            AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
            public List<Category> ChildCategories { get; set; } = new List<Category>();
        }
        public class LatestPost
        {
            public int Id { get; set; }
            public string Slug { get; set; }
            public string Title { get; set; }
            public string CateName { get; set; }
            public string AuthorId { get; set; }
            public string Author { get; set; }
            public string AvatarPath { get; set; }
        }
        public class TopUsers
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string AvatarPath { get; set; }
            public int NumPosts { get; set; }
        }
        public class IndexViewModel
        {
            public List<Category> Categories { get; set; }
            public List<LatestPost> LatestPosts { get; set; }
            public List<TopUsers> TopUsers { get; set; }
            public int NumUsers { get; set; }
            public int NumPosts { get; set; }
            public int NumPostInWeek { get; set; }
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            IndexViewModel model = new IndexViewModel();

            var categories = await _dbContext.Categories.Where(c => c.ParentCateId == null)
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
                                                                Slug = cc.Slug
                                                            }).ToList()
                                                        }).ToListAsync();
            
            var latestPosts = await _dbContext.Posts.OrderByDescending(p => p.DateCreated).Take(5)
                                                    .Include(p => p.Category)
                                                    .Include(p => p.User)
                                                    .Select(p => new LatestPost
                                                    {
                                                        Id = p.Id,
                                                        Slug = p.Slug,
                                                        Title = p.Title,
                                                        CateName = p.Category.Name,
                                                        AuthorId = p.AuthorId,
                                                        Author = p.User.UserName,
                                                        AvatarPath = _dbContext.Images.Where(i => i.UserId == p.AuthorId && i.UseType == UseType.profile)
                                                                                    .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg"
                                                    }).ToListAsync();

            var topUsers = await _dbContext.Users.Include(u => u.Posts)
                                                .OrderByDescending(u => u.Posts.Count).Take(5)
                                                .Select(u => new TopUsers
                                                {
                                                    Id = u.Id,
                                                    UserName = u.UserName,
                                                    AvatarPath = _dbContext.Images.Where(i => i.UserId == u.Id && i.UseType == UseType.profile)
                                                                                .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
                                                    NumPosts = u.Posts.Count
                                                }).ToListAsync();
            
            var numUsers = await _dbContext.Users.CountAsync();
            var numPosts = await _dbContext.Posts.CountAsync();
            var firstDayOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek + (int)DayOfWeek.Monday);
            var numPostsInWeek = await _dbContext.Posts
                .Where(p => p.DateCreated >= firstDayOfWeek)
                .CountAsync();

            model.Categories = categories;
            model.LatestPosts = latestPosts;
            model.TopUsers = topUsers;
            model.NumUsers = numUsers;
            model.NumPosts = numPosts;
            model.NumPostInWeek = numPostsInWeek;

            return View(model);
        }
    }
}