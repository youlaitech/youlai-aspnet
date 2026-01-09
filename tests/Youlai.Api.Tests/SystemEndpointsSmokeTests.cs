using System.Net;
using System.Text.Json;
using Xunit;

namespace Youlai.Api.Tests;

public sealed class SystemEndpointsSmokeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SystemEndpointsSmokeTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task UsersMe_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DeptsList_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/depts");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DeptsOptions_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/depts/options");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MenusRoutes_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/menus/routes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MenusOptions_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/menus/options");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MenusList_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/menus");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MenusForm_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/menus/1/form");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UsersPage_ShouldReturnPageResultShape()
    {
        var resp = await _client.GetAsync("/api/v1/users?pageNum=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        AssertPageResultShape(root);
    }

    [Fact]
    public async Task RolesPage_ShouldReturnPageResultShape()
    {
        var resp = await _client.GetAsync("/api/v1/roles?pageNum=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        AssertPageResultShape(root);
    }

    [Fact]
    public async Task RolesOptions_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/roles/options");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DictsPage_ShouldReturnPageResultShape()
    {
        var resp = await _client.GetAsync("/api/v1/dicts?pageNum=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        AssertPageResultShape(root);
    }

    [Fact]
    public async Task DictsList_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/dicts/options");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DictsForm_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/dicts/1/form");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DictItemsPage_ShouldReturnPageResultShape()
    {
        var resp = await _client.GetAsync("/api/v1/dicts/gender/items?pageNum=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        AssertPageResultShape(root);
    }

    [Fact]
    public async Task DictItemsList_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/dicts/gender/items/options");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DictItemsForm_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/dicts/gender/items/1/form");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task NoticesPage_ShouldReturnPageResultShape()
    {
        var resp = await _client.GetAsync("/api/v1/notices?pageNum=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        AssertPageResultShape(root);
    }

    [Fact]
    public async Task NoticesForm_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/notices/1/form");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task NoticesDetail_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/api/v1/notices/1/detail");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task NoticesMy_ShouldReturnPageResultShape()
    {
        var resp = await _client.GetAsync("/api/v1/notices/my?pageNum=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        AssertPageResultShape(root);
    }

    private static void AssertPageResultShape(JsonElement root)
    {
        Assert.True(root.TryGetProperty("code", out _));
        Assert.True(root.TryGetProperty("msg", out _));

        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);

        Assert.True(root.TryGetProperty("page", out var page));
        Assert.True(page.TryGetProperty("pageNum", out _));
        Assert.True(page.TryGetProperty("pageSize", out _));
        Assert.True(page.TryGetProperty("total", out _));
    }
}
