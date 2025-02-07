#nullable disable

using System.Globalization;
using System.Threading.Tasks;
using App.Areas.Dashboard.Models;
using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Dashboard.Controllers;

[Area("Dashboard")]
public class DashboardController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IGoogleAnalyticsService _googleAnalyticsService;

    public DashboardController(
        AppDbContext dbContext,
        IGoogleAnalyticsService googleAnalyticsService
    )
    {
        _dbContext = dbContext;
        _googleAnalyticsService = googleAnalyticsService;
    }

    [HttpGet("/dashboard")]
    public async Task<ActionResult> Index()
    {
        var model = new IndexViewModel();

        var viewsData = GetNumViews();
        model.TotalVisits = viewsData.TotalVisits;
        model.VisitsChartData = viewsData.VisitsChartData;

        var postsData = GetNumPosts();
        model.TotalPosts = postsData.TotalPosts;
        model.PostsChartData = postsData.PostsChartData;

        var usersData = GetNumUsers();
        model.TotalUsers = usersData.TotalUsers;
        model.UsersChartData = usersData.UsersChartData;

        model.TopPosts = await GetTopPosts();
        model.TopUsers = await GetTopUsers();

        return View(model);
    }

    private IndexViewModel GetNumViews()
    {
        var model = new IndexViewModel();

        var startDate = DateTime.Now.AddDays(-30);
        var endDate = DateTime.Now;
        var visitsLast30Days = _googleAnalyticsService.GetDailyPageViews(startDate, endDate);

        var totalVisits = visitsLast30Days.Sum(x => x.PageViews);

        var visitsLast15Days = visitsLast30Days
            .AsReadOnly()
            .OrderByDescending(x => DateTime.ParseExact(x.Date, "yyyyMMdd", null))
            .Take(15)
            .OrderBy(x => DateTime.ParseExact(x.Date, "yyyyMMdd", null))
            .ToList();

        model.TotalVisits = totalVisits;
        model.VisitsChartData = visitsLast15Days;
        return model;
    }

    private IndexViewModel GetNumPosts()
    {
        var model = new IndexViewModel();

        var totalPosts = _dbContext.Posts.Count();

        var today = DateTime.Now.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var startDate = today.AddDays(-(42 + daysSinceMonday));
        var endDate = today;

        var weeklyPostStats = _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.DateCreated >= startDate && p.DateCreated <= endDate)
            .AsEnumerable()
            .GroupBy(p => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(p.DateCreated, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
            .Select(g => new
            {
                Week = g.Key,
                Year = g.First().DateCreated.Year,
                PostCount = g.Count()
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Week)
            .ToList();

        var chartData = new List<(string Week, int PostCount)>();
        foreach (var stat in weeklyPostStats)
        {
            string weekLabel = $"T{stat.Week} ({stat.Year})";
            chartData.Add((weekLabel, stat.PostCount));
        }

        model.TotalPosts = totalPosts;
        model.PostsChartData = chartData;
        return model;
    }

    private IndexViewModel GetNumUsers()
    {
        var model = new IndexViewModel();

        var totalUsers = _dbContext.Users.Count();

        var today = DateTime.Now.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var startDate = today.AddDays(-(42 + daysSinceMonday));
        var endDate = today;

        var weeklyUserStats = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.AccountCreationDate >= startDate && u.AccountCreationDate <= endDate)
            .AsEnumerable()
            .GroupBy(u => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(u.AccountCreationDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
            .Select(g => new
            {
                Week = g.Key,
                Year = g.First().AccountCreationDate.Year,
                UserCount = g.Count()
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Week)
            .ToList();

        var chartData = new List<(string Week, int UserCount)>();
        foreach (var stat in weeklyUserStats)
        {
            string weekLabel = $"T{stat.Week} ({stat.Year})";
            chartData.Add((weekLabel, stat.UserCount));
        }

        model.TotalUsers = totalUsers;
        model.UsersChartData = chartData;
        return model;
    }

    private async Task<List<PostView>> GetTopPosts()
    {
        var posts = await _dbContext.Posts
                            .AsNoTracking()
                            .OrderByDescending(p => p.NumViews)
                            .Take(7)
                            .Select(p => new PostView 
                            {
                                Id = p.Id,
                                Slug = p.Slug,
                                Title = p.Title,
                                AuthorName = p.User.UserName,
                                AuthorId = p.User.Id,
                                TotalViews = p.NumViews
                            }).ToListAsync();
        return posts;
    }

    private async Task<List<UserView>> GetTopUsers()
    {
        var users = await _dbContext.Users
                            .AsNoTracking()
                            .OrderByDescending(u => u.Posts.Count())
                            .Take(5)
                            .Select(u => new UserView
                            {
                                Id = u.Id,
                                Name = u.UserName,
                                AvatarPath = _dbContext.Images.Where(i => i.UserId == u.Id && i.UseType == UseType.profile)
                                                            .Select(i => i.FilePath).FirstOrDefault() ?? "/images/no_avt.jpg",
                                TotalPosts = u.Posts.Count()
                            }).ToListAsync();
        return users;
    }
}