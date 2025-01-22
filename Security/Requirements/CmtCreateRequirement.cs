using Microsoft.AspNetCore.Authorization;

namespace App.Security.Requirements;
public class CmtCreateRequirement : IAuthorizationRequirement
{
    public CmtCreateRequirement() {}
}