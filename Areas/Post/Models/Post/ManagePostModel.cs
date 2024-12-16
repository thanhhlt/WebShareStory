#nullable disable

using App.Models;

namespace App.Areas.Post.Models.Post;

public class PostView : PostsModel
{
    public string Author { get; set; }
    public string CategoryName { get; set; }
    public string SlugCate { get; set; }
}

public class ManagePostModel
{
    public List<PostView> Posts { get; set; }
    public int totalPosts { get; set; }
    public int countPages { get; set; }

    public int ITEMS_PER_PAGE { get; set; } = 20;

    public int currentPage { get; set; }

    public string SearchString { get; set; }

    public string MessageSearchResult { get; set; }
}