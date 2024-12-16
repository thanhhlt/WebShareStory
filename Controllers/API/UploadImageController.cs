#nullable disable

using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class UploadImageController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public UploadImageController(
        AppDbContext dbContext,
        IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string userId, [FromForm] int postId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Không có ảnh.");

        string directoryPath = Path.Combine(_env.ContentRootPath, "Images/Posts", postId.ToString());
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        //Generate file path
        int fileName;
        var filePaths = await _dbContext.Images.Where(i => i.PostId == postId && i.UseType == UseType.post)
                                            .Select(i => i.FilePath).ToListAsync();
        if (filePaths == null)
        {
            fileName = 1;
        }
        else
        {
            fileName = filePaths
                    .Select(filePath => 
                    {
                        var directoryPath = Path.GetDirectoryName(filePath);
                        var parts = directoryPath.Split('/');
                        var postIdFromPath = int.Parse(parts.Last());
                        
                        return postIdFromPath;
                    })
                    .Max();
        }
        string fileNameExtension = fileName.ToString() + Path.GetExtension(file.FileName).ToLower();
        string filePath = Path.Combine(directoryPath, fileNameExtension);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        filePath = Path.Combine("/imgs/Posts", postId.ToString(), fileNameExtension);
        var image = new ImagesModel
        {
            FileName = file.FileName,
            FilePath = filePath,
            UseType = UseType.post,
            UserId = userId,
            PostId = postId
        };

        _dbContext.Images.Add(image);
        await _dbContext.SaveChangesAsync();

        return Ok(new { imagePath = image.FilePath });
    }
}