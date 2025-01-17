using App.Models;
using Microsoft.AspNetCore.Identity;

public interface IUserBlockService
{
    bool IsBlockedUser(string? userId);
    IQueryable<PostsModel> GetFilteredPosts(IQueryable<PostsModel> posts);
    IQueryable<CommentsModel> GetFilteredComments(IQueryable<CommentsModel> comments);
}

public class UserBlockService : IUserBlockService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;

    public UserBlockService(IHttpContextAccessor httpContextAccessor,
                            UserManager<AppUser> userManager,
                            AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public bool IsBlockedUser(string? userId)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User;
        if (currentUser?.Identity == null || !currentUser.Identity.IsAuthenticated)
            return false;

        var currentUserId = _userManager.GetUserId(currentUser);
        if (string.IsNullOrEmpty(currentUserId)) return false;

        if (_dbContext.UserRelations == null)
            return false;

        if (IsRoleManage(currentUserId))
            return false;

        return _dbContext.UserRelations.Any(ur =>
            ((ur.UserId == currentUserId && ur.OtherUserId == userId) ||
            (ur.UserId == userId && ur.OtherUserId == currentUserId)) &&
            ur.Status == RelationshipStatus.Block);
    }

    public IQueryable<PostsModel> GetFilteredPosts(IQueryable<PostsModel> posts)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User;
        if (currentUser?.Identity == null || !currentUser.Identity.IsAuthenticated)
            return posts;

        var currentUserId = _userManager.GetUserId(currentUser);
        if (string.IsNullOrEmpty(currentUserId))
            return posts;

        if (_dbContext.UserRelations == null)
            return posts;

        if (IsRoleManage(currentUserId))
            return posts;

        var blockedUserIds = new HashSet<string>(_dbContext.UserRelations
            .Where(ur => (ur.UserId == currentUserId || ur.OtherUserId == currentUserId)
                            && ur.Status == RelationshipStatus.Block)
            .Select(ur => ur.UserId == currentUserId ? ur.OtherUserId : ur.UserId)
            .Where(id => id != null)!);

        return posts.Where(p => p.AuthorId != null && !blockedUserIds.Contains(p.AuthorId));
    }

    public IQueryable<CommentsModel> GetFilteredComments(IQueryable<CommentsModel> comments)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User;
        if (currentUser?.Identity == null || !currentUser.Identity.IsAuthenticated)
            return comments;

        var currentUserId = _userManager.GetUserId(currentUser);
        if (string.IsNullOrEmpty(currentUserId))
            return comments;

        if (_dbContext.UserRelations == null)
            return comments;

        if (IsRoleManage(currentUserId))
            return comments;

        var blockedUserIds = new HashSet<string>(_dbContext.UserRelations
            .Where(ur => (ur.UserId == currentUserId || ur.OtherUserId == currentUserId)
                            && ur.Status == RelationshipStatus.Block)
            .Select(ur => ur.UserId == currentUserId ? ur.OtherUserId : ur.UserId)
            .Where(id => id != null)!);

        return comments.Where(c => c.UserId != null && !blockedUserIds.Contains(c.UserId));
    }

    private bool IsRoleManage(string userId)
    {
        var roles = _userManager.GetRolesAsync(new AppUser { Id = userId }).Result;
        var privilegedRoles = new[] { "Admin", "Editor", "Moderator" };
        return roles.Any(r => privilegedRoles.Contains(r));
    }
}