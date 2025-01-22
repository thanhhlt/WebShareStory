using Microsoft.AspNetCore.Authorization;

namespace App.Security.Requirements;
public class PostCreateRequirement : IAuthorizationRequirement
{
    public PostCreateRequirement() {}
}