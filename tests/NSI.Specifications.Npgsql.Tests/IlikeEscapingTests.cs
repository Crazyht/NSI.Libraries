using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Filtering;
using Xunit;

namespace NSI.Specifications.Npgsql.Tests;

/// <summary>
/// Tests for escaping special characters in ILIKE pattern rewrite.
/// </summary>
[Collection(PostgresCollection.CollectionName)]
public sealed class IlikeEscapingTests(PostgresFixture fx)
{
    private readonly PostgresFixture _Fx = fx;

    [Theory]
    [InlineData("100%", "%100\\%%")] // contains %
    [InlineData("under_score", "%under\\_score%")] // contains _
    [InlineData("mix%and_", "%mix\\%and\\_%")] // both
    [InlineData("back\\slash", "%back\\\\slash%")] // backslash
    [InlineData("plain", "%plain%")] // no special chars
    [InlineData("a_%b\\c%d", "%a\\_\\%b\\\\c\\%d%")] // complex mixture
    public void Contains_EscapeCharacters_RewrittenPattern(string term, string expectedPatternFragment)
    {
        using var ctx = new SampleDbContext(_Fx.Options);
        var spec = new NSI.Specifications.Filtering.Text.ContainsSpecification<SampleEntity>(x => x.Name!, term, ignoreCase: true);
        var sql = ctx.Entities.Where(spec).ToQueryString();
        Assert.Contains("ILIKE", sql, System.StringComparison.OrdinalIgnoreCase); // rewrite happened
        Assert.Contains(expectedPatternFragment, sql, System.StringComparison.Ordinal); // escaped pattern present
    }
}
