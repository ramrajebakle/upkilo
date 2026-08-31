using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Upkilo.Infrastructure.Data.Seeders;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// The dev seeder runs during host startup, so a failure here does not fail one test — it fails
/// the WebApplicationFactory constructor and takes every test in that class with it. That is
/// what happened in CI: PublicRazorpayPaymentTests and OpenApiContractTests each boot a host,
/// xUnit runs test classes in parallel, both hosts passed the seeder's check-then-act guard
/// before either committed, and the loser died on
///
///   23505: duplicate key value violates unique constraint "IX_Tenants_Slug"
///
/// Nothing exercised the seeder directly, so a race in demo fixtures could only ever surface as
/// an unrelated integration test failing on a full CI run.
/// </summary>
public class DevDataSeederTests
{
    [Fact]
    public async Task SeedAsync_PopulatesTheDevTenants()
    {
        using var factory = new TestDbContextFactory();

        await DevDataSeeder.SeedAsync(factory.CreateContext());

        var context = factory.CreateContext();
        context.Tenants.Should().Contain(t => t.Slug == "glow-beauty-dev");
        context.Users.Should().Contain(u => u.Email == "owner@glowbeauty.test");
    }

    [Fact]
    public async Task SeedAsync_RunTwice_IsANoOpRatherThanACrash()
    {
        using var factory = new TestDbContextFactory();

        await DevDataSeeder.SeedAsync(factory.CreateContext());
        var act = async () => await DevDataSeeder.SeedAsync(factory.CreateContext());

        await act.Should().NotThrowAsync("the guard must make a repeat seed a no-op");

        // And it must not have duplicated anything on the way through.
        factory.CreateContext().Tenants.Count(t => t.Slug == "glow-beauty-dev").Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_WhenAnotherHostAlreadyInsertedADevTenant_TreatsItAsAlreadySeeded()
    {
        using var factory = new TestDbContextFactory();

        // Deterministic stand-in for the CI race. The guard only looks for "glow-beauty-dev",
        // so pre-inserting a DIFFERENT dev slug lets the guard pass and drives the seeder into
        // the same unique-constraint collision the losing host hit — without depending on
        // thread interleaving, which SQLite serialises away.
        var pre = factory.CreateContext();
        pre.Tenants.Add(new Upkilo.Core.Entities.Tenant
        {
            Id = Guid.NewGuid(),
            Name = "FitLife Gym",
            Slug = "fitlife-gym-dev",
            Email = "info@fitlifegym.test",
        });
        await pre.SaveChangesAsync();

        var act = async () => await DevDataSeeder.SeedAsync(factory.CreateContext());

        await act.Should().NotThrowAsync(
            "a unique violation here means another host already seeded, which is the success " +
            "condition — propagating it out of startup fails the whole WebApplicationFactory");

        factory.CreateContext().Tenants.Count(t => t.Slug == "fitlife-gym-dev").Should().Be(1);
    }
}
