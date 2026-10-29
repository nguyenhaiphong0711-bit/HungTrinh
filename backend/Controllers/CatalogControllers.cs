using System.ComponentModel.DataAnnotations;
using HungTrinh.Data;
using HungTrinh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HungTrinh.Controllers;
[ApiController, Route("api/products"), Authorize]
public sealed class ProductsController(ApplicationDbContext db) : ControllerBase
{
    public sealed record ProductInput([Required, StringLength(200)] string Name,
        [Range(typeof(decimal), "0", "999999999")] decimal Price,
        [Range(1, int.MaxValue)] int CategoryId, [Range(0, int.MaxValue)] int Stock = 0);
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync());
    [HttpPost, Authorize(Roles = Roles.Editors)]
    public async Task<IActionResult> Create(ProductInput input)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == input.CategoryId)) return BadRequest(new { message = "Category does not exist." });
        var entity = new Product { Name = input.Name, Price = input.Price, CategoryId = input.CategoryId, Stock = input.Stock };
        db.Products.Add(entity); await db.SaveChangesAsync();
        return Created($"/api/products/{entity.Id}", entity);
    }
    [HttpPut("{id:int}"), Authorize(Roles = Roles.Editors)]
    public async Task<IActionResult> Update(int id, ProductInput input)
    {
        var entity = await db.Products.FindAsync(id);
        if (entity is null) return NotFound();
        if (!await db.Categories.AnyAsync(c => c.Id == input.CategoryId)) return BadRequest(new { message = "Category does not exist." });
        entity.Name = input.Name; entity.Price = input.Price; entity.CategoryId = input.CategoryId; entity.Stock = input.Stock;
        await db.SaveChangesAsync(); return NoContent();
    }
    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Editors)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.Products.FindAsync(id); if (entity is null) return NotFound();
        db.Products.Remove(entity); await db.SaveChangesAsync(); return NoContent();
    }
}
[ApiController, Route("api/categories"), Authorize]
public sealed class CategoriesController(ApplicationDbContext db) : ControllerBase
{
    public sealed record CategoryInput([Required, StringLength(100)] string Name);
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync());
    [HttpPost, Authorize(Roles = Roles.Editors)]
    public async Task<IActionResult> Create(CategoryInput input)
    {
        var entity = new Category { Name = input.Name }; db.Categories.Add(entity); await db.SaveChangesAsync();
        return Created($"/api/categories/{entity.Id}", entity);
    }
    [HttpPut("{id:int}"), Authorize(Roles = Roles.Editors)]
    public async Task<IActionResult> Update(int id, CategoryInput input)
    {
        var entity = await db.Categories.FindAsync(id); if (entity is null) return NotFound();
        entity.Name = input.Name; await db.SaveChangesAsync(); return NoContent();
    }
    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Editors)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.Categories.FindAsync(id); if (entity is null) return NotFound();
        if (await db.Products.AnyAsync(p => p.CategoryId == id)) return Conflict(new { message = "Category still contains products." });
        db.Categories.Remove(entity); await db.SaveChangesAsync(); return NoContent();
    }
}
