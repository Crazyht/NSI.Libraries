using NSI.Specifications.Filtering.Text;
using Xunit;

namespace NSI.Specifications.Tests.Filtering;
/// <summary>
/// Tests for text filtering specifications.
/// </summary>
public sealed class TextSpecificationsTests {
  private sealed class Entity {
    public string? Name { get; init; }
    public Nested? Node { get; init; }
  }

  private sealed class Nested {
    public string? Code { get; init; }
    public Nested? Child { get; init; }
  }

  private static readonly Entity[] Data = [
    new() { Name = "Alpha", Node = new Nested { Code = "Root" } },
    new() {
      Name = "alphabet",
      Node = new Nested {
        Code = "branch",
        Child = new Nested { Code = "LeafX" }
      }
    },
    new() { Name = "beta", Node = null },
    new() { Name = null, Node = new Nested { Code = null } }
  ];

  private static readonly string[] ExpectedAlpha = ["Alpha", "alphabet"];

  [Fact]
  public void StartsWith_IgnoreCase_MixedCaseMatches() {
    var spec = new StartsWithSpecification<Entity>(
      e => e.Name,
      "ALP",
      ignoreCase: true
    );
    var result = Data
      .AsQueryable()
      .Where(spec.ToExpression())
      .Select(e => e.Name)
      .OrderBy(n => n)
      .ToArray();

    Assert.Equal(ExpectedAlpha, result);
  }

  [Fact]
  public void EndsWith_CaseSensitive_NoMatchWhenCaseDiffers() {
    var spec = new EndsWithSpecification<Entity>(
      e => e.Name,
      "BET",
      ignoreCase: false
    );
    var result = Data
      .AsQueryable()
      .Where(spec.ToExpression())
      .ToArray();

    // "alphabet" ends with "bet" but casing differs
    Assert.Empty(result);
  }

  [Fact]
  public void Contains_IgnoreCase_FindsSubstring() {
    var spec = new ContainsSpecification<Entity>(
      e => e.Name,
      "PHa",
      ignoreCase: true
    );
    var result = Data
      .AsQueryable()
      .Where(spec.ToExpression())
      .Select(e => e.Name)
      .OrderBy(n => n)
      .ToArray();

    Assert.Equal(ExpectedAlpha, result);
  }

  [Fact]
  public void StartsWith_NullDeepPath_ReturnsFalse() {
    var spec = new StartsWithSpecification<Entity>(
      e => e.Node!.Child!.Code,
      "Leaf",
      ignoreCase: true
    );
    var result = Data
      .AsQueryable()
      .Where(spec.ToExpression())
      .ToArray();

    // only second entity has Child.Code = LeafX
    Assert.Single(result);
  }

  [Fact]
  public void StartsWith_EmptyTerm_ReturnsFalse() {
    var spec = new StartsWithSpecification<Entity>(
      e => e.Name,
      string.Empty,
      ignoreCase: true
    );
    var result = Data
      .AsQueryable()
      .Where(spec.ToExpression())
      .ToArray();

    Assert.Empty(result);
  }
}
