using System.ComponentModel.DataAnnotations;
using HungTrinh.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HungTrinh.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IAntiforgery csrf) : ControllerBase
{
    public sealed record Credentials([Required, EmailAddress] string Email, [Required, MinLength(12), MaxLength(128)] string Password);
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf() => Ok(new { token = csrf.GetAndStoreTokens(HttpContext).RequestToken });
    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(Credentials input)
    {
        var user = new ApplicationUser { UserName = input.Email, Email = input.Email };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded) return BadRequest(new { message = "Registration failed. Check email and password requirements." });
        result = await users.AddToRoleAsync(user, Roles.User);
        if (!result.Succeeded)
        {
            await users.DeleteAsync(user);
            return Problem("Registration is temporarily unavailable. Roles must be initialized.");
        }
        return Ok(new { message = "Account created. Sign in to continue." });
    }
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(Credentials input)
    {
        var result = await signIn.PasswordSignInAsync(input.Email, input.Password, false, lockoutOnFailure: true);
        return result.Succeeded ? Ok(new { message = "Signed in." }) : Unauthorized(new { message = "Invalid credentials or account unavailable." });
    }
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return NoContent(); }
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(new { user.Id, user.Email, roles = await users.GetRolesAsync(user) });
    }
}
