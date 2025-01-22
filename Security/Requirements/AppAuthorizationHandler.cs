using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using Microsoft.AspNetCore.Identity;
namespace App.Security.Requirements;

public class AppAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<AppAuthorizationHandler> _logger;
    private readonly UserManager<AppUser> _userManager;

    public AppAuthorizationHandler(
        ILogger<AppAuthorizationHandler> logger,
        UserManager<AppUser> userManager
    )
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();
        foreach(var requirement in pendingRequirements)
        {
            if (requirement is PostUpdateRequirement)
            {
                if (await CanUpdatePost(context.User, context.Resource, (PostUpdateRequirement)requirement))
                {
                    context.Succeed(requirement);
                }
            }
            if (requirement is PostCreateRequirement)
            {
                if (await CanCreatePost(context.User, context.Resource, (PostCreateRequirement)requirement))
                {
                    context.Succeed(requirement);
                }
            }
            if (requirement is CmtCreateRequirement)
            {
                if (await CanComment(context.User, context.Resource, (CmtCreateRequirement)requirement))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }

    private async Task<bool> CanUpdatePost(ClaimsPrincipal user, object? resource, PostUpdateRequirement requirement)
    {
        if (user.HasClaim("Feature", "PostManage"))
        {
            return true;
        }

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null)
        {
            return false;
        }
        var authorId = resource as string;
        if (authorId == null)
        {
            return false;
        }

        return authorId == appUser.Id;
    }

    private async Task<bool> CanCreatePost(ClaimsPrincipal user, object? resource, PostCreateRequirement requirement)
    {
        if (user.HasClaim("Feature", "PostManage"))
        {
            return true;
        }

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null)
        {
            return false;
        }

        return appUser?.PostLockEnd == null || appUser?.PostLockEnd < DateTime.Now;
    }

    private async Task<bool> CanComment(ClaimsPrincipal user, object? resource, CmtCreateRequirement requirement)
    {
        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null)
        {
            return false;
        }

        return appUser?.CommentLockEnd == null || appUser?.CommentLockEnd < DateTime.Now;
    }
}