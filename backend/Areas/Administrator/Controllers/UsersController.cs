using HungTrinh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HungTrinh.Areas.Administrator.Controllers;
[Area("Administrator"), ApiController, Route("api/admin/users"), Authorize(Roles = Roles.Administrator)]
public sealed class UsersController(UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = new List<object>();
        foreach (var user in await users.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync())
            result.Add(new { user.Id, user.Email, roles = await users.GetRolesAsync(user) });
        return Ok(result);
    }
}
