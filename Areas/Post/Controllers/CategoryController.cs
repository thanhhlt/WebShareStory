#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Models;
using App.Areas.Post.Models.Category;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Post.Controllers
{
    [Area("Post")]
    [Route("admin/category/[action]/{id?}")]
    public class CategoryController : Controller
    {
        private readonly ILogger<CategoryController> _logger;
        private readonly AppDbContext _dbContext;

        public CategoryController(
            ILogger<CategoryController> logger,
            AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        // GET: /admin/category/
        [HttpGet("/admin/category")]
        public async Task<IActionResult> Index()
        {
            var qr = (from c in _dbContext.Categories select c)
                     .Include(c => c.ParentCate)
                     .Include(c => c.ChildCates);

            IndexViewModel model = new IndexViewModel();
            model.Categories = (await qr.ToListAsync())
                                .Where(c => c.ParentCate == null).ToList();       

            return View(model);
        }

        //GET: /admin/category/Create
        [HttpGet]
        public async Task<IActionResult> Create ()
        {
            var allCates = await _dbContext.Categories.Select(c => c.Name).ToListAsync();
            CreateModel model = new CreateModel()
            {
                AllCates = new SelectList(allCates)
            };
            return View(model);
        }

        //POST: /admin/category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync (CreateModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Tạo danh mục thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            CategoriesModel category = new CategoriesModel()
            {
                Name = model.Name,
                Slug = ""
            };
            category.SetSlug();

            if (model.ParentCate != null)
            {
                var parentCateId = await _dbContext.Categories.Where(c => c.Name == model.ParentCate)
                                                    .Select(c => c.Id).FirstOrDefaultAsync();
                category.ParentCateId = parentCateId;
            }
            if (!string.IsNullOrEmpty(model.Description))
            {
                category.Description = model.Description;
            }

            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            StatusMessage = $"Đã tạo danh mục {model.Name}";
            return Json(new{success = true, redirect = Url.Action("Index")});
        }

        // GET: /admin/category/Edit/id
        [HttpGet]
        public async Task<IActionResult> EditAsync (int? id)
        {
            if (id == null)
            {
                return NotFound("Không tìm thấy danh mục.");
            }
            var category = await _dbContext.Categories.Where(c => c.Id == id)
                                                    .Include(c => c.ParentCate)
                                                    .FirstOrDefaultAsync();
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục.");
            }

            var allCates = await _dbContext.Categories.Where(c => c.Name != category.Name).Select(c => c.Name).ToListAsync();
            EditModel model = new EditModel()
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                AllCates = new SelectList(allCates)
            };
            if (category.ParentCate != null)
            {
                model.ParentCate = category.ParentCate.Name;
            }
            return View(model);
        }

        //POST: /admin/category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync (EditModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Thay đổi danh mục thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }
            var category = await _dbContext.Categories.Where(c => c.Id == model.Id)
                                                    .Include(c => c.ParentCate)
                                                    .FirstOrDefaultAsync();
            if (category == null)
            {
                StatusMessage = "Không tìm thấy danh mục.";
                return Json(new{success = false});
            }

            if (model.ParentCate != null)
            {
                var parentCateId = await _dbContext.Categories.Where(c => c.Name == model.ParentCate)
                                                    .Select(c => c.Id).FirstOrDefaultAsync();
                category.ParentCateId = parentCateId;
            }
            category.Name = model.Name;
            category.Description = model.Description;
            category.SetSlug();
            await _dbContext.SaveChangesAsync();
            
            StatusMessage = $"Đã thay đổi danh mục {model.Name}";
            return Json(new{success = true, redirect = Url.Action("Index")});
        }

        //POST: /admin/category/Delete
        [HttpGet]
        public async Task<IActionResult> DeleteAsync (int? id)
        {
            if (id == null)
            {
                return NotFound("Không tìm thấy danh mục.");
            }
            var category = await _dbContext.Categories.Where(c => c.Id == id)
                                                    .Include(c => c.ParentCate)
                                                    .Include(c => c.ChildCates)
                                                    .FirstOrDefaultAsync();
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục.");
            }

            if (category.ChildCates!= null && category.ChildCates.Any())
            {
                int? parentCateId = category.ParentCate != null ? category.ParentCate.Id : null;
                foreach (var cCategory in category.ChildCates)
                {
                    cCategory.ParentCateId = parentCateId;
                }
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();

            StatusMessage = $"Đã xoá danh mục {category.Name}";
            return RedirectToAction(nameof(Index));
        }
    }
}
