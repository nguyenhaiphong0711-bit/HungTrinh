using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HungTrinh.Models;
public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Staff = "Staff";
    public const string User = "User";
    public const string Editors = Administrator + "," + Staff;
    public static readonly string[] All = [Administrator, Staff, User];
}
public sealed class ApplicationUser : IdentityUser { }
public sealed class Category
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
}
public sealed class Product
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Range(typeof(decimal), "0", "999999999")] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}
