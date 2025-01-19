#nullable disable

namespace App.Areas.Dashboard.Models;

public class PostView
{
    public int Id { get; set; }
    public string Slug { get; set; }
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public string AuthorId { get; set; }
    public int TotalViews { get; set; }
}
public class UserView
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AvatarPath { get; set; }
    public int TotalPosts { get; set; }
}

public class IndexViewModel
{
    public int TotalVisits { get; set; }
    public int TotalPosts { get; set; }
    public int TotalUsers { get; set; }

    public List<(string Date, int Sessions)> VisitsChartData { get; set; }
    public List<(string Week, int PostCount)> PostsChartData { get; set; }
    public List<(string Week, int UserCount)> UsersChartData { get; set; }

    public List<PostView> TopPosts { get; set; }
    public List<UserView> TopUsers { get; set; }
}