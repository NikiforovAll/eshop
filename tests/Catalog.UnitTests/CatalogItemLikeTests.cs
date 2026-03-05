using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using eShop.Catalog.API;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Infrastructure.EntityConfigurations;
using eShop.Catalog.API.IntegrationEvents;
using eShop.Catalog.API.Model;
using eShop.Catalog.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace eShop.Catalog.UnitTests;

[TestClass]
public class CatalogItemLikeTests
{
    public TestContext TestContext { get; set; } = null!;

    private static CatalogContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var config = new ConfigurationBuilder().Build();
        return new TestCatalogContext(options, config);
    }

    /// <summary>
    /// Satisfies the 'required' DbSet properties without needing object initializer syntax.
    /// EF Core auto-initializes DbSet properties via the base constructor.
    /// </summary>
    private class TestCatalogContext
        : CatalogContext
    {
        [SetsRequiredMembers]
        public TestCatalogContext(DbContextOptions<CatalogContext> options, IConfiguration configuration)
            : base(options, configuration) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new CatalogItemLikeEntityTypeConfiguration());
            builder.Entity<CatalogItem>().Ignore(ci => ci.Embedding);
        }
    }

    private static CatalogServices CreateServices(CatalogContext context)
    {
        return new CatalogServices(
            context,
            Substitute.For<ICatalogAI>(),
            Options.Create(new CatalogOptions { LikeCacheDurationSeconds = 30 }),
            Substitute.For<ILogger<CatalogServices>>(),
            Substitute.For<ICatalogIntegrationEventService>(),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static HttpContext CreateHttpContext(string? userId = null)
    {
        var context = new DefaultHttpContext();
        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId)], "test"));
        }
        return context;
    }

    [TestMethod]
    public async Task GetItemLikes_NoLikes_ReturnsZeroCountAndNotLiked()
    {
        using var context = CreateContext(nameof(GetItemLikes_NoLikes_ReturnsZeroCountAndNotLiked));
        var services = CreateServices(context);

        var result = await CatalogApi.GetItemLikes(CreateHttpContext(), services, id: 1);

        Assert.AreEqual(0, result.Value!.Count);
        Assert.IsFalse(result.Value.IsLiked);
    }


    [TestMethod]
    public async Task GetItemLikes_UserHasLiked_ReturnsCorrectCountAndIsLiked()
    {
        const string userId = "user-1";
        using var context = CreateContext(nameof(GetItemLikes_UserHasLiked_ReturnsCorrectCountAndIsLiked));

        context.CatalogItemLikes.AddRange(
            new CatalogItemLike { CatalogItemId = 1, UserId = userId, CreatedAt = DateTime.UtcNow },
            new CatalogItemLike { CatalogItemId = 1, UserId = "user-2", CreatedAt = DateTime.UtcNow },
            new CatalogItemLike { CatalogItemId = 1, UserId = "user-3", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.CancellationToken);

        var services = CreateServices(context);
        var result = await CatalogApi.GetItemLikes(CreateHttpContext(userId), services, id: 1);

        Assert.AreEqual(3, result.Value!.Count);
        Assert.IsTrue(result.Value.IsLiked);
    }

    [TestMethod]
    public async Task ToggleItemLike_FirstLike_AddsLikeAndReturnsIsLiked()
    {
        const string userId = "user-1";
        using var context = CreateContext(nameof(ToggleItemLike_FirstLike_AddsLikeAndReturnsIsLiked));
        var services = CreateServices(context);

        var result = await CatalogApi.ToggleItemLike(CreateHttpContext(userId), services, id: 1);

        Assert.AreEqual(1, result.Value!.Count);
        Assert.IsTrue(result.Value.IsLiked);
        Assert.AreEqual(1, await context.CatalogItemLikes.CountAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ToggleItemLike_AlreadyLiked_RemovesLikeAndReturnsNotLiked()
    {
        const string userId = "user-1";
        using var context = CreateContext(nameof(ToggleItemLike_AlreadyLiked_RemovesLikeAndReturnsNotLiked));

        context.CatalogItemLikes.Add(
            new CatalogItemLike { CatalogItemId = 1, UserId = userId, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.CancellationToken);

        var services = CreateServices(context);
        var result = await CatalogApi.ToggleItemLike(CreateHttpContext(userId), services, id: 1);

        Assert.AreEqual(0, result.Value!.Count);
        Assert.IsFalse(result.Value.IsLiked);
        Assert.AreEqual(0, await context.CatalogItemLikes.CountAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ToggleItemLike_MultipleUsers_CountReflectsAllLikes()
    {
        using var context = CreateContext(nameof(ToggleItemLike_MultipleUsers_CountReflectsAllLikes));

        context.CatalogItemLikes.Add(
            new CatalogItemLike { CatalogItemId = 1, UserId = "user-1", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.CancellationToken);

        var services = CreateServices(context);
        var result = await CatalogApi.ToggleItemLike(CreateHttpContext("user-2"), services, id: 1);

        Assert.AreEqual(2, result.Value!.Count);
        Assert.IsTrue(result.Value.IsLiked);
    }

    [TestMethod]
    public async Task ToggleItemLike_Unlike_OnlyRemovesCurrentUsersLike()
    {
        using var context = CreateContext(nameof(ToggleItemLike_Unlike_OnlyRemovesCurrentUsersLike));

        context.CatalogItemLikes.AddRange(
            new CatalogItemLike { CatalogItemId = 1, UserId = "user-1", CreatedAt = DateTime.UtcNow },
            new CatalogItemLike { CatalogItemId = 1, UserId = "user-2", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.CancellationToken);

        var services = CreateServices(context);
        var result = await CatalogApi.ToggleItemLike(CreateHttpContext("user-1"), services, id: 1);

        Assert.AreEqual(1, result.Value!.Count);
        Assert.IsFalse(result.Value.IsLiked);

        var remainingLike = await context.CatalogItemLikes.SingleAsync(TestContext.CancellationToken);
        Assert.AreEqual("user-2", remainingLike.UserId);
    }
}
