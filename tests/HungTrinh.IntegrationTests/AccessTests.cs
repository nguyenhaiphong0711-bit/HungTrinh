using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HungTrinh.Data;
using HungTrinh.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HungTrinh.IntegrationTests;

public sealed class AppFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = Guid.NewGuid().ToString();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }

    public async Task<HttpClient> ClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
        });
        using var scope = Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Administrator", "Staff", "User" })
            Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        return client;
    }

    public async Task SetRoleAsync(string email, string role)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        var current = await users.GetRolesAsync(user);
        Assert.True((await users.RemoveFromRolesAsync(user, current)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
    }
}

public sealed class AccessTests
{
    private const string Email = "integration@example.test";
    private const string Password = "Strong!Password123";

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("203.0.113.10", false)]
    public async Task ForwardedHttpsIsAcceptedOnlyFromTrustedProxy(string remoteAddress, bool trusted)
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        var context = await factory.Server.SendAsync(request =>
        {
            request.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
            request.Request.Scheme = "http";
            request.Request.Host = new HostString("localhost");
            request.Request.Method = "GET";
            request.Request.Path = "/api/auth/csrf";
            request.Request.Headers["X-Forwarded-Proto"] = "https";
        });
        Assert.Equal(trusted, context.Request.IsHttps);
        if (trusted)
        {
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Contains(context.Response.Headers.SetCookie, cookie => cookie is not null && cookie.StartsWith("HungTrinh.Csrf=") && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // SecurePolicy.Always refuses minting antiforgery tokens on an HTTP request.
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.DoesNotContain(context.Response.Headers.SetCookie, cookie => cookie is not null && cookie.StartsWith("HungTrinh.Csrf="));
        }
    }

    private static async Task<HttpResponseMessage> MutateAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task RegisterAsync(HttpClient client)
    {
        using var response = await MutateAsync(client, HttpMethod.Post, "/api/auth/register", new { email = Email, password = Password, role = "Administrator", roles = new[] { "Administrator" } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await MutateAsync(client, HttpMethod.Post, "/api/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Staff", HttpStatusCode.Forbidden)]
    [InlineData("Administrator", HttpStatusCode.OK)]
    public async Task AdministratorAreaChecksServerSideRole(string? role, HttpStatusCode expected)
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        if (role is not null)
        {
            await RegisterAsync(client);
            await factory.SetRoleAsync(Email, role);
            await LoginAsync(client);
        }
        using var response = await client.GetAsync("/api/admin/users");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationCannotChooseAdministratorRole()
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        await RegisterAsync(client);
        await LoginAsync(client);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal(new[] { "User" }, me.GetProperty("roles").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [Fact]
    public async Task MissingAndInvalidCsrfTokensAreRejected()
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        using var missing = await client.PostAsJsonAsync("/api/auth/register", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");
        using var invalid = await client.PostAsJsonAsync("/api/auth/register", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Theory]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Staff", HttpStatusCode.Created)]
    [InlineData("Administrator", HttpStatusCode.Created)]
    public async Task CatalogWritesRequireEditorRole(string role, HttpStatusCode expected)
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        await RegisterAsync(client);
        await factory.SetRoleAsync(Email, role);
        await LoginAsync(client);
        using var read = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var category = await MutateAsync(client, HttpMethod.Post, "/api/categories", new { name = "Test category" });
        Assert.Equal(expected, category.StatusCode);
        if (expected == HttpStatusCode.Forbidden)
        {
            using var denied = await MutateAsync(client, HttpMethod.Post, "/api/products", new { name = "Product", price = 100, categoryId = 1, stock = 5 });
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            return;
        }
        var categoryData = await category.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = categoryData.GetProperty("id").GetInt32();
        using var product = await MutateAsync(client, HttpMethod.Post, "/api/products", new { name = "Product", price = 100, categoryId, stock = 5 });
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        Assert.Contains(products.EnumerateArray(), p => p.GetProperty("name").GetString() == "Product");
        using var deleteCategory = await MutateAsync(client, HttpMethod.Delete, $"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteCategory.StatusCode);
        var productData = await product.Content.ReadFromJsonAsync<JsonElement>();
        var productId = productData.GetProperty("id").GetInt32();
        using var updateCategory = await MutateAsync(client, HttpMethod.Put, $"/api/categories/{categoryId}", new { name = "Updated category" });
        Assert.Equal(HttpStatusCode.NoContent, updateCategory.StatusCode);
        using var updateProduct = await MutateAsync(client, HttpMethod.Put, $"/api/products/{productId}", new { name = "Updated product", price = 250, categoryId, stock = 2 });
        Assert.Equal(HttpStatusCode.NoContent, updateProduct.StatusCode);
        var updated = await client.GetFromJsonAsync<JsonElement>("/api/products");
        Assert.Contains(updated.EnumerateArray(), p => p.GetProperty("id").GetInt32() == productId && p.GetProperty("name").GetString() == "Updated product" && p.GetProperty("price").GetDecimal() == 250 && p.GetProperty("stock").GetInt32() == 2);
        using var removeProduct = await MutateAsync(client, HttpMethod.Delete, $"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, removeProduct.StatusCode);
        using var removeCategory = await MutateAsync(client, HttpMethod.Delete, $"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, removeCategory.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/products")).EnumerateArray());
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/categories")).EnumerateArray());
    }

    [Fact]
    public async Task WrongPasswordIsRejectedAndLogoutInvalidatesSession()
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        await RegisterAsync(client);
        using var denied = await MutateAsync(client, HttpMethod.Post, "/api/auth/login", new { email = Email, password = "Incorrect!Password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var anonymous = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await LoginAsync(client);
        using var noCsrf = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var logout = await MutateAsync(client, HttpMethod.Post, "/api/auth/logout");
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var loggedOut = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(10, 0)]
    [InlineData(10, 9999)]
    public async Task InvalidProductInputsAreRejected(decimal price, int categoryId)
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        await RegisterAsync(client);
        await factory.SetRoleAsync(Email, "Staff");
        await LoginAsync(client);
        using var category = await MutateAsync(client, HttpMethod.Post, "/api/categories", new { name = "Validation category" });
        Assert.Equal(HttpStatusCode.Created, category.StatusCode);
        using var product = await MutateAsync(client, HttpMethod.Post, "/api/products", new { name = "Invalid product", price, categoryId, stock = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, product.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/products")).EnumerateArray());
    }

    [Fact]
    public async Task AnonymousCannotReadCatalog()
    {
        using var factory = new AppFactory();
        using var client = await factory.ClientAsync();
        using var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
