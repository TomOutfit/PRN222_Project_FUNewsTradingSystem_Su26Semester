using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_MVC.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "StaffOnly")]
[Route("Staff/Tags")]
public class TagController : Controller
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? page)
    {
        var pageNumber = PaginationSettings.ValidatePageNumber(page);
        var pageSize = PaginationSettings.DefaultPageSize;

        var tags = await _tagService.GetAllTagsAsync();
        var pagedTags = tags
            .OrderByDescending(t => t.TagID)
            .ToPagedList(pageNumber, pageSize);
        
        return View(pagedTags);
    }

    [HttpGet("CreatePartial")]
    public IActionResult CreatePartial()
    {
        return PartialView("~/Views/Staff/Tags/_CreateTagModal.cshtml", new Tag());
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] Tag tag)
    {
        try
        {
            await _tagService.CreateTagAsync(tag);

            return Ok(new
            {
                success = true,
                message = "Tag created successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet("EditPartial/{id}")]
    public async Task<IActionResult> EditPartial(int id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);

        if (tag == null)
            return NotFound();

        return PartialView("~/Views/Staff/Tags/_EditTagModal.cshtml",tag);
    }

    [HttpPost("Edit")]
    public async Task<IActionResult> Edit([FromBody] Tag tag)
    {
        try
        {
            await _tagService.UpdateTagAsync(tag);

            return Ok(new
            {
                success = true,
                message = "Updated successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _tagService.DeleteTagAsync(id);

            return Ok(new
            {
                success = true,
                message = "Deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}