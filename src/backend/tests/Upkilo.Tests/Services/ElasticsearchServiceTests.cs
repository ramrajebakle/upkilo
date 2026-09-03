using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Search had three defects that only a test comparing the WRITE path with the READ path could
/// have caught, plus a query built by string interpolation.
///
/// These tests do not need a live Elasticsearch: the interesting behaviour is which index name
/// gets built and what shape the query takes, both of which are decided before any request is
/// issued. That is deliberate — the previous tests only asserted "does not throw", which passed
/// happily while the feature could not work at all.
/// </summary>
public class ElasticsearchServiceTests
{
    private static ElasticsearchService CreateService(string? uri = "http://localhost:9200")
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Elasticsearch:Uri"]).Returns(uri);
        return new ElasticsearchService(config.Object, new Mock<ILogger<ElasticsearchService>>().Object);
    }

    /// <summary>Reads the private index map, which is the single source of truth for names.</summary>
    private static System.Collections.Generic.IReadOnlyDictionary<Type, string> IndexMap()
        => (System.Collections.Generic.IReadOnlyDictionary<Type, string>)
            typeof(ElasticsearchService)
                .GetField("IndexSuffixByType", BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null)!;

    [Fact]
    public void Instantiation_WithValidUri_DoesNotThrow()
        => ((Action)(() => CreateService("http://localhost:9200"))).Should().NotThrow();

    /// <summary>
    /// Unconfigured must mean "issue no request", not "issue one and wait for it to fail".
    ///
    /// The URI used to default to http://localhost:9200, so every deployment — none of which has
    /// Elasticsearch — built a client and spent the full 10s RequestTimeout per search before
    /// returning empty. On a B1 instance that is a request thread parked for ten seconds on a
    /// feature that cannot succeed.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAvailable_IsFalse_WhenNoUriConfigured(string? uri)
        => CreateService(uri).IsAvailable.Should().BeFalse();

    [Fact]
    public void IsAvailable_IsTrue_WhenUriConfigured()
        => CreateService("http://es.internal:9200").IsAvailable.Should().BeTrue();

    [Fact]
    public async Task WhenUnavailable_SearchReturnsImmediately_WithoutWaitingOnTheNetwork()
    {
        var svc = CreateService(null);
        var sw = Stopwatch.StartNew();

        await svc.GlobalSearchAsync("tenant-1", "anything");
        await svc.AutocompleteAsync("tenant-1", "anything");
        await svc.IndexEntityAsync("tenant-1", new Client { Id = Guid.NewGuid() });

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(1000,
            "an unconfigured service must short-circuit, not spend the 10s client timeout");
    }

    /// <summary>
    /// THE defect. The write path and the read path must resolve the same index for the same
    /// entity, and nothing enforced that:
    ///
    ///   writes            -> "{tenant}_object"   (interceptor's compile-time T is `object`)
    ///   GlobalSearchAsync -> "{tenant}_client"   (singular)
    ///   AutocompleteAsync -> "{tenant}_clients"  (plural)
    ///
    /// Three names for one entity, so a document written was never a document found.
    /// </summary>
    [Theory]
    [InlineData(typeof(Client))]
    [InlineData(typeof(Booking))]
    [InlineData(typeof(Service))]
    public void EverySearchableEntity_HasExactlyOneIndexSuffix(Type entityType)
    {
        var map = IndexMap();

        map.Should().ContainKey(entityType,
            "an entity the interceptor indexes must have a suffix, or its writes go nowhere");
        map[entityType].Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void IndexSuffixes_AreUnique_SoTwoEntitiesCannotShareAnIndex()
    {
        var suffixes = IndexMap().Values.ToList();

        suffixes.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// The interceptor indexes exactly Client, Booking and Service. If it learns a new entity
    /// and the map is not updated, that entity's writes silently vanish — so pin the pairing.
    /// </summary>
    [Fact]
    public void TheIndexMap_CoversEveryEntityTheInterceptorWrites()
    {
        var interceptor = CodeWithoutComments(InterceptorPath());
        var map = IndexMap();

        foreach (var name in new[] { "Client", "Booking", "Service" })
        {
            interceptor.Should().Contain($"is {name}",
                "this test's premise is that the interceptor indexes these entities");
            map.Keys.Select(k => k.Name).Should().Contain(name);
        }
    }

    /// <summary>
    /// The interceptor must pass the RUNTIME type on delete. It used to call
    /// DeleteEntityAsync&lt;object&gt;, which resolved to "{tenant}_object" — an index no search
    /// reads — so deleting a client left it findable.
    /// </summary>
    [Fact]
    public void TheInterceptor_DeletesUsingTheRuntimeType_NotObject()
    {
        var src = CodeWithoutComments(InterceptorPath());

        src.Should().NotContain("DeleteEntityAsync<object>",
            "that resolves to the index {tenant}_object, which nothing searches");
        src.Should().Contain("entry.Entity.GetType()");
    }

    /// <summary>
    /// Free-text search must not be assembled by interpolating the caller's input into a
    /// query_string query. query_string is a query LANGUAGE — field selectors, booleans,
    /// ranges, regex — so interpolation let the caller write the query rather than supply a
    /// term, reaching fields it should not and burning CPU on a crafted regex. Tenant-bounded,
    /// but injection nonetheless.
    /// </summary>
    [Fact]
    public void FreeTextSearch_UsesABoundQuery_NotStringInterpolation()
    {
        var src = CodeWithoutComments(ServicePath());

        src.Should().NotContain("QueryString(",
            "query_string with interpolated input is injectable; use multi_match");
        src.Should().NotContain("$\"*{query}*\"",
            "a forced leading wildcard also makes every search a full-index scan");
        src.Should().Contain("MultiMatch");
    }

    /// <summary>
    /// ?type= lands in an index NAME, so it must be filtered against the known set. The tenant
    /// prefix always bounded this to the caller's own data — it was never a tenant escape — but
    /// an arbitrary value has no business reaching index resolution.
    /// </summary>
    [Fact]
    public async Task Autocomplete_IgnoresUnknownTypes_RatherThanNamingArbitraryIndexes()
    {
        var svc = CreateService("http://es.invalid:9200");

        // Nothing to assert on the wire without a live ES; what matters is that an unknown type
        // yields no index to query at all, so the call returns empty instead of erroring.
        var result = await svc.AutocompleteAsync("tenant-1", "abc", new[] { "../../etc", "*", "secrets" });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Autocomplete_AcceptsKnownTypes()
    {
        var known = IndexMap().Values.First();
        var svc = CreateService(null); // unavailable: exercises the guard, not the network

        var result = await svc.AutocompleteAsync("tenant-1", "abc", new[] { known });

        result.Should().BeEmpty("unavailable returns empty — the point is it does not throw");
    }

    [Fact]
    public async Task IndexEntityAsync_WithAnUnmappedEntity_IsANoOp()
    {
        var svc = CreateService("http://es.invalid:9200");

        // A Tenant is never searchable; it must not be written to some improvised index.
        var act = async () => await svc.IndexEntityAsync("tenant-1", new Tenant { Id = Guid.NewGuid() });

        await act.Should().NotThrowAsync();
    }

    private static string ServicePath() => RepoFile(
        "Upkilo.Infrastructure", "Services", "ElasticsearchService.cs");

    private static string InterceptorPath() => RepoFile(
        "Upkilo.Infrastructure", "Data", "SearchSyncInterceptor.cs");

    /// <summary>
    /// Resolves repo paths from this file's own COMPILE-TIME location, not from
    /// AppContext.BaseDirectory.
    ///
    /// The runtime output directory is not reliably inside the repo — a build with a redirected
    /// BaseOutputPath puts it somewhere else entirely — so both "count ../.. hops" and "walk up
    /// looking for the .sln" fail there. Either would have made the source-reading assertions
    /// below throw (or, worse, skip and pass vacuously) depending on how the suite was invoked.
    /// [CallerFilePath] is baked in at compile time and is correct regardless.
    /// </summary>
    /// <summary>
    /// Returns the file's CODE with comments removed.
    ///
    /// The assertions below search for the defective constructs this change removed. Those
    /// same constructs are quoted in the comments that explain why they went, so reading the
    /// raw file matched the explanation and failed on correct code. Documenting a fix must not
    /// be able to fail the test for that fix.
    /// </summary>
    private static string CodeWithoutComments(string path)
    {
        var src = System.IO.File.ReadAllText(path);
        src = System.Text.RegularExpressions.Regex.Replace(
            src,
            @"/\*.*?\*/",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);

        var lines = src.Split('\n')
            .Where(l => !l.TrimStart().StartsWith("//"));

        return string.Join("\n", lines);
    }

    private static string BackendRoot([System.Runtime.CompilerServices.CallerFilePath] string thisFile = "")
    {
        // <backend>/tests/Upkilo.Tests/Services/ElasticsearchServiceTests.cs
        var servicesDir = System.IO.Path.GetDirectoryName(thisFile)!;
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(servicesDir, "..", "..", ".."));
    }

    private static string RepoFile(params string[] parts)
        => System.IO.Path.Combine(new[] { BackendRoot() }.Concat(parts).ToArray());
}
