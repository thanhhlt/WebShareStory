#nullable disable

using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public interface IDeleteUserService
{
    Task<bool> DeleteUserAsync(string userId);
}

public class DeleteUserService : IDeleteUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public DeleteUserService(
        UserManager<AppUser> userManager, 
        AppDbContext dbContext,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _env = env;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var userRelations = await _dbContext.UserRelations.Where(ur => ur.OtherUserId == userId).ToListAsync();
        if (userRelations.Any())
        {
            _dbContext.UserRelations.RemoveRange(userRelations);
        }

        var img = await _dbContext.Images.Where(i => i.UserId == user.Id && i.UseType == UseType.profile).FirstOrDefaultAsync();
        if (img != null)
        {
            if (img.FilePath.StartsWith("/imgs/"))
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Images/" + img.FilePath.Substring(6));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            _dbContext.Images.Remove(img);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}
