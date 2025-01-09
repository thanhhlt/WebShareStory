using Microsoft.AspNetCore.Authorization;

namespace App.Security.Requirements;
public class PostUpdateRequirement : IAuthorizationRequirement
{
    public PostUpdateRequirement() {}
}