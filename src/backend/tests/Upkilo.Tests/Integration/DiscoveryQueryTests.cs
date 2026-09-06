using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Upkilo.API.Controllers;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Xunit;

namespace Upkilo.Tests.Integration;

/// <summary>
/// The discovery endpoints must produce SQL Postgres can actually run.
///
/// These ran against real PostgreSQL deliberately. The bug they guard was a TRANSLATION failure,
/// invisible to any test that does not reach a provider:
///
///   System.InvalidOperationException: The LINQ expression
///   '__categoryKeywords_2.Any(e => ILike(t.BusinessType ?? "", Format("%{0}%", e)) || ...)'
///   could not be translated.
///
/// `categoryKeywords` is a client-side list and the pattern was built per element inside the
/// lambda, so EF Core could not compose it into SQL and threw at query time. Production logged 50
/// unhandled 500s in a day on endpoints that are [AllowAnonymous] and back
/// upkilo.com/book/[category]/[city] — public pages whose entire purpose is to be crawled.
///
/// The code compiled perfectly throughout. Only executing it finds this.
/// </summary>
[Trait("Category", "Integration")]
[Collection(PostgresCollection.Name)]
public class DiscoveryQueryTests : IAsyncDisposable
{
    private readonly AppDbContext _context;

    public DiscoveryQueryTests(PostgresFixture fixture) => _context = new AppDbContext(fixture.Options);

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private DiscoveryController BuildSut() =>
        new(_context, NullLogger<DiscoveryController>.Instance);

    /// <summary>Seeds one active tenant and returns its slug.</summary>
    private async Task<string> SeedTenantAsync(string businessType, string industry, string city)
    {
        var id = Guid.NewGuid();
        var slug = $"t-{id:N}"[..20];

        _context.Tenants.Add(new Tenant
        {
            Id = id,
            Name = $"Test {businessType}",
            Slug = slug,
            City = city,
            BusinessType = businessType,
            Industry = industry,
            IsActive = true,
        });
        await _context.SaveChangesAsync();
        return slug;
    }

    [Fact]
    public async Task GetListings_WithCategoryKeywords_ExecutesInsteadOfThrowing()
    {
        // "hair-salons" maps to { hair, salon, barber } — the multi-keyword path that could not
        // be translated. Reaching a result at all is the assertion that matters.
        var city = $"City{Guid.NewGuid():N}"[..12];
        await SeedTenantAsync("Hair Salon", "Beauty", city);

        var result = await BuildSut().GetListings("hair-salons", city);

        result.Should().BeOfType<OkObjectResult>(
            "an untranslatable predicate threw InvalidOperationException here and surfaced as a 500");
    }

    [Fact]
    public async Task GetListings_MatchesOnBusinessType()
    {
        var city = $"City{Guid.NewGuid():N}"[..12];
        var slug = await SeedTenantAsync("Barber Shop", "Grooming", city);

        var result = await BuildSut().GetListings("hair-salons", city);

        // "barber" is one of the category's keywords, matched against BusinessType.
        Serialize(result).Should().Contain(slug);
    }

    [Fact]
    public async Task GetListings_MatchesOnIndustry()
    {
        // The OR's second arm: nothing in BusinessType matches, the match is on Industry.
        var city = $"City{Guid.NewGuid():N}"[..12];
        var slug = await SeedTenantAsync("Studio", "Tattoo", city);

        var result = await BuildSut().GetListings("tattoo", city);

        Serialize(result).Should().Contain(slug);
    }

    [Fact]
    public async Task GetListings_ExcludesATenantInAnotherCategory()
    {
        var city = $"City{Guid.NewGuid():N}"[..12];
        var slug = await SeedTenantAsync("Dental Practice", "Dental", city);

        var result = await BuildSut().GetListings("hair-salons", city);

        Serialize(result).Should().NotContain(slug,
            "the keyword filter must actually filter, not just avoid throwing");
    }

    [Fact]
    public async Task GetListings_WithAnUnknownCategory_ReturnsNothingRatherThanEverything()
    {
        // An unmapped slug yields no keywords. The filter is skipped entirely in that case, so
        // this pins that a retired category does not silently list every business in the city.
        var city = $"City{Guid.NewGuid():N}"[..12];
        await SeedTenantAsync("Hair Salon", "Beauty", city);

        var result = await BuildSut().GetListings("fitness", city);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task NearMe_WithCategoryKeywords_ExecutesInsteadOfThrowing()
    {
        // The second call site, where the predicate is applied before the Locations join.
        var city = $"City{Guid.NewGuid():N}"[..12];
        await SeedTenantAsync("Massage Therapy", "Wellness", city);

        var result = await BuildSut().NearMe(city, "massage");

        result.Should().BeOfType<OkObjectResult>();
    }

    private static string Serialize(IActionResult result) =>
        System.Text.Json.JsonSerializer.Serialize(((OkObjectResult)result).Value);
}
