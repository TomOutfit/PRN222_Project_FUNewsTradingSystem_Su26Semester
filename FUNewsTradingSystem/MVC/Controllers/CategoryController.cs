using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using FUNewsTradingSystem_MVC.Hubs;

namespace FUNewsTradingSystem_MVC.Controllers
{
    [Authorize(Policy = "StaffOnly")]
    [Route("Staff/Categories")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public CategoryController(ICategoryService categoryService, IHubContext<NotificationHub> notificationHub)
        {
            _categoryService = categoryService;
            _notificationHub = notificationHub;
        }

        // GET /Staff/Categories
        [HttpGet("")]
        public async Task<IActionResult> Index(int? page)
        {
            var pageNumber = PaginationSettings.ValidatePageNumber(page);
            var pageSize = PaginationSettings.DefaultPageSize;

            var categories = await _categoryService.GetAllCategoriesAsync();

            var model = categories.Select(c => new CategoryListItemViewModel
            {
                CategoryId = c.CategoryID,
                CategoryName = c.CategoryName,
                CategoryDescription = c.CategoryDescription,
                ParentCategoryID = c.ParentCategoryID,

                ParentCategoryName =
        c.ParentCategory != null
            ? c.ParentCategory.CategoryName
            : null,

                IsActive = c.IsActive
            }).OrderBy(c => c.CategoryId).ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        // GET /Staff/Categories/CreatePartial
        [HttpGet("CreatePartial")]
        public async Task<IActionResult> CreatePartial()
        {
            var topLevelCategories = await _categoryService.GetTopLevelAsync();

            ViewBag.ParentCategories = new SelectList(topLevelCategories, "CategoryID", "CategoryName");

            var emptyCategory = new Category { IsActive = true };

            return PartialView("~/Views/Staff/Categories/_CreateCategoryModal.cshtml",
                   emptyCategory);
        }

        // POST /Staff/Categories/Create (AJAX JSON)
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            var result = await _categoryService.CreateCategoryAsync(category);
            if (result)
            {
                await _notificationHub.Clients.All.SendAsync("ReceiveCRUDNotification", "create", "Tạo Mới Thành Công", $"Danh mục '{category.CategoryName}' đã được tạo thành công trên hệ thống.");
                return Ok(new { success = true, message = "Created successfully!" });
            }
            return BadRequest(new { success = false, message = "Failed to create category." });
        }

        // GET /Staff/Categories/EditPartial/{id}
        [HttpGet("EditPartial/{id}")]
        public async Task<IActionResult> EditPartial(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var allTopCategories = await _categoryService.GetTopLevelAsync();

            var validParentCategories = allTopCategories.Where(c => c.CategoryID != id).ToList();

            ViewBag.ParentCategories = new SelectList(validParentCategories, "CategoryID", "CategoryName", category.ParentCategoryID);

            return PartialView("~/Views/Staff/Categories/_EditCategoryModal.cshtml",
                   category);
        }

        // POST /Staff/Categories/Edit (AJAX JSON)
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            var result = await _categoryService.UpdateCategoryAsync(category);
            if (result)
            {
                await _notificationHub.Clients.All.SendAsync("ReceiveCRUDNotification", "update", "Cập Nhật Thành Công", $"Danh mục '{category.CategoryName}' đã được cập nhật thành công.");
                return Ok(new { success = true, message = "Updated successfully!" });
            }
            return BadRequest(new { success = false, message = "Failed to update category." });
        }

        // POST /Staff/Categories/ToggleActive/{id}
        [HttpPost("ToggleActive/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var newStatus = await _categoryService.ToggleActiveAsync(id);
            await _notificationHub.Clients.All.SendAsync("ReceiveCRUDNotification", "update", "Cập Nhật Thành Công", $"Danh mục ID {id} đã thay đổi trạng thái.");
            return Ok(new { success = true, newIsActive = newStatus });
        }

        // POST /Staff/Categories/Delete/{id}
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (result)
            {
                await _notificationHub.Clients.All.SendAsync("ReceiveCRUDNotification", "delete", "Xóa Thành Công", $"Danh mục ID {id} đã bị xóa khỏi hệ thống.");
                return Ok(new { success = true, message = "Deleted successfully!" });
            }
            return BadRequest(new { success = false, message = "Cannot delete: category has linked articles." });
        }
    }
}