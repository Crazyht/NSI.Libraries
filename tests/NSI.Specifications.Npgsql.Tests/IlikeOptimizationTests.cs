using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Filtering;
using NSI.Specifications.Filtering.Text;

namespace NSI.Specifications.Npgsql.Tests;

/// <summary>
/// Integration tests verifying that case-insensitive text specifications are rewritten to
/// PostgreSQL ILIKE patterns (Contains / StartsWith / EndsWith) when Npgsql optimizations are registered.
/// </summary>
[Collection(PostgresCollection.CollectionName)]
public sealed class IlikeOptimizationTests(PostgresFixture fx)
{
    private readonly PostgresFixture _Fx = fx;

    [Fact]
    /// <summary>
    /// Verifies that Contains with ignoreCase=true is rewritten to ILIKE and returns expected rows.
    /// </summary>
    public async Task Contains_IgnoreCase_ShouldUseILike()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new ContainsSpecification<SampleEntity>(x => x.Name, "Alpha", ignoreCase: true);
        var query = ctx.Entities.Where(spec);
        var sql = query.ToQueryString();
        Assert.Contains("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
        var result = await query.ToListAsync();
        Assert.NotEmpty(result);
    }

    [Fact]
    /// <summary>
    /// Verifies that StartsWith with ignoreCase=true is rewritten to ILIKE.
    /// </summary>
    public void StartsWith_IgnoreCase_ShouldUseILike()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new StartsWithSpecification<SampleEntity>(x => x.Name!, "alp", ignoreCase: true);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.Contains("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>
    /// Ensures that a case-sensitive Contains is not rewritten.
    /// </summary>
    public void Contains_CaseSensitive_ShouldNotUseILike()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new ContainsSpecification<SampleEntity>(x => x.Name!, "Alpha", ignoreCase: false);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.DoesNotContain("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>
    /// Verifies EndsWith with ignoreCase=true is rewritten to ILIKE.
    /// </summary>
    public void EndsWith_IgnoreCase_ShouldUseILike()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new EndsWithSpecification<SampleEntity>(x => x.Name!, "ET", ignoreCase: true);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.Contains("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>
    /// Ensures that an empty Contains search term prevents rewriting.
    /// </summary>
    public void Contains_EmptyTerm_ShouldNotRewrite()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new ContainsSpecification<SampleEntity>(x => x.Name!, string.Empty, ignoreCase: true);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.DoesNotContain("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>
    /// Ensures that an empty StartsWith search term prevents rewriting.
    /// </summary>
    public void StartsWith_EmptyTerm_ShouldNotRewrite()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new StartsWithSpecification<SampleEntity>(x => x.Name!, string.Empty, ignoreCase: true);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.DoesNotContain("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    /// <summary>
    /// Ensures that an empty EndsWith search term prevents rewriting.
    /// </summary>
    public void EndsWith_EmptyTerm_ShouldNotRewrite()
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new EndsWithSpecification<SampleEntity>(x => x.Name!, string.Empty, ignoreCase: true);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.DoesNotContain("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }
}
