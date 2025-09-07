using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Filtering;
using NSI.Specifications.Filtering.Text;
using Xunit;

namespace NSI.Specifications.Npgsql.Tests;

/// <summary>
/// Escaping tests for StartsWith and EndsWith rewritten to ILIKE patterns.
/// </summary>
[Collection(PostgresCollection.CollectionName)]
public sealed class IlikeStartsEndsEscapingTests(PostgresFixture fx) {
  private readonly PostgresFixture _Fx = fx;

  [Theory]
  // StartsWith cases
  [InlineData("Starts", "100%", "ILIKE '100\\%%'")] // escaped % then trailing pattern %
  [InlineData("Starts", "under_score", "ILIKE 'under\\_score%'")] // escaped _
  [InlineData("Starts", "a_%b\\c%d", "ILIKE 'a\\_\\%b\\\\c\\%d%'")] // complex
                                                                    // EndsWith cases
  [InlineData("Ends", "%tail", "ILIKE '%\\%tail'")] // leading escape in middle
  [InlineData("Ends", "under_", "ILIKE '%under\\_'")] // underscore end
  [InlineData("Ends", "mix%_end", "ILIKE '%mix\\%\\_end'")] // both in middle
  public void StartsOrEndsWith_EscapeCharacters_PatternCorrect(string mode, string term, string expectedFragment) {
    using var ctx = new SampleDbContext(_Fx.Options);
    IQueryable<SampleEntity> query = ctx.Entities;
    if (mode == "Starts") {
      var spec = new StartsWithSpecification<SampleEntity>(e => e.Name!, term, ignoreCase: true);
      query = query.Where(spec);
    } else {
      var spec = new EndsWithSpecification<SampleEntity>(e => e.Name!, term, ignoreCase: true);
      query = query.Where(spec);
    }
    var sql = query.ToQueryString();
    Assert.Contains("ILIKE", sql, System.StringComparison.OrdinalIgnoreCase);
    Assert.Contains(expectedFragment, sql, System.StringComparison.Ordinal);
  }

  [Theory]
  [InlineData("Starts", "Alpha")] // no rewrite expected when ignoreCase=false
  [InlineData("Ends", "Alpha")]
  public void StartsOrEndsWith_CaseSensitive_NoRewrite(string mode, string term) {
    using var ctx = new SampleDbContext(_Fx.Options);
    IQueryable<SampleEntity> query = ctx.Entities;
    if (mode == "Starts") {
      var spec = new StartsWithSpecification<SampleEntity>(e => e.Name!, term, ignoreCase: false);
      query = query.Where(spec);
    } else {
      var spec = new EndsWithSpecification<SampleEntity>(e => e.Name!, term, ignoreCase: false);
      query = query.Where(spec);
    }
    var sql = query.ToQueryString();
    Assert.DoesNotContain("ILIKE", sql, System.StringComparison.OrdinalIgnoreCase);
  }
}
