using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using eShop.Catalog.API.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogItemLikeTests : IClassFixture<CatalogApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public CatalogItemLikeTests(CatalogApiFixture fixture)
    {
        _webApplicationFactory = fixture;
    }

    private HttpClient CreateHttpClient(ApiVersion apiVersion)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), apiVersion);
        return _webApplicationFactory.CreateDefaultClient(handler);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task GetItemLikes_ReturnsZeroForItemWithNoLikes(double version)
    {
        var httpClient = CreateHttpClient(new ApiVersion(version));

        var response = await httpClient.GetAsync("/api/catalog/items/7/likes", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<CatalogItemLikeResult>(body, _jsonSerializerOptions);

        Assert.Equal(0, result.Count);
        Assert.False(result.IsLiked);
    }

    [Theory]
    [InlineData(1.0, 10)]
    [InlineData(2.0, 11)]
    public async Task ToggleItemLike_LikesItem(double version, int itemId)
    {
        var httpClient = CreateHttpClient(new ApiVersion(version));

        // Act - toggle to like
        var response = await httpClient.PutAsync($"/api/catalog/items/{itemId}/like", null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<CatalogItemLikeResult>(body, _jsonSerializerOptions);

        Assert.Equal(1, result.Count);
        Assert.True(result.IsLiked);

        // Verify via GET
        response = await httpClient.GetAsync($"/api/catalog/items/{itemId}/likes", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result = JsonSerializer.Deserialize<CatalogItemLikeResult>(body, _jsonSerializerOptions);

        Assert.Equal(1, result.Count);
        Assert.True(result.IsLiked);
    }

    [Theory]
    [InlineData(1.0, 12)]
    [InlineData(2.0, 13)]
    public async Task ToggleItemLike_TogglesLikeOff(double version, int itemId)
    {
        var httpClient = CreateHttpClient(new ApiVersion(version));

        // First toggle to like
        var response = await httpClient.PutAsync($"/api/catalog/items/{itemId}/like", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        // Second toggle to unlike
        response = await httpClient.PutAsync($"/api/catalog/items/{itemId}/like", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<CatalogItemLikeResult>(body, _jsonSerializerOptions);

        Assert.Equal(0, result.Count);
        Assert.False(result.IsLiked);
    }
}
