using System;
using System.Linq;
using NSI.Specifications.Filtering.Comparison;
using Xunit;

namespace NSI.Specifications.Tests.Filtering;

/// <summary>
/// Tests for comparison and range specifications.
/// </summary>
public sealed class ComparisonSpecificationsTests {
  private sealed class Item {
    public int Value { get; init; }
    public Nested? Node { get; init; }
  }
  private sealed class Nested {
    public int Depth { get; init; }
    public Nested? Child { get; init; }
  }

  private static readonly Item[] Data = [
      new() { Value = 1, Node = new Nested { Depth = 5 } },
            new() { Value = 5, Node = new Nested { Depth = 10, Child = new Nested { Depth = 15 } } },
            new() { Value = 10, Node = null },
            new() { Value = 15, Node = new Nested { Depth = 20 } }
  ];

  private static readonly int[] ExpectedGreaterThan5 = [10, 15];
  private static readonly int[] ExpectedLessThanOrEqual5 = [1, 5];
  private static readonly int[] ExpectedBetweenInclusive = [5, 10];
  private static readonly int[] ExpectedBetweenExclusive = [10];
  private static readonly int[] ExpectedBetweenMixed = [5, 10];

  [Fact]
  public void GreaterThanSpecification_Filters() {
    var spec = new GreaterThanSpecification<Item, int>(x => x.Value, 5);
    var result = Data.AsQueryable().Where(spec.ToExpression()).Select(i => i.Value).OrderBy(v => v).ToArray();
    Assert.Equal(ExpectedGreaterThan5, result);
  }

  [Fact]
  public void LessThanOrEqualSpecification_IncludesBoundary() {
    var spec = new LessThanOrEqualSpecification<Item, int>(x => x.Value, 5);
    var result = Data.AsQueryable().Where(spec.ToExpression()).Select(i => i.Value).OrderBy(v => v).ToArray();
    Assert.Equal(ExpectedLessThanOrEqual5, result);
  }

  [Fact]
  public void BetweenSpecification_Inclusive_IncludesBounds() {
    var spec = new BetweenSpecification<Item, int>(x => x.Value, 5, 10, true, true);
    var result = Data.AsQueryable().Where(spec.ToExpression()).Select(i => i.Value).OrderBy(v => v).ToArray();
    Assert.Equal(ExpectedBetweenInclusive, result);
  }

  [Fact]
  public void BetweenSpecification_Exclusive_ExcludesBounds() {
    var spec = new BetweenSpecification<Item, int>(x => x.Value, 5, 15, false, false);
    var result = Data.AsQueryable().Where(spec.ToExpression()).Select(i => i.Value).OrderBy(v => v).ToArray();
    Assert.Equal(ExpectedBetweenExclusive, result);
  }

  [Fact]
  public void BetweenSpecification_Mixed_InclusiveLowerExclusiveUpper() {
    var spec = new BetweenSpecification<Item, int>(x => x.Value, 5, 15, true, false);
    var result = Data.AsQueryable().Where(spec.ToExpression()).Select(i => i.Value).OrderBy(v => v).ToArray();
    Assert.Equal(ExpectedBetweenMixed, result);
  }

  [Fact]
  public void GreaterThanSpecification_NullDeepPath_Excluded() {
    var spec = new GreaterThanSpecification<Item, int>(x => x.Node!.Child!.Depth, 10);
    var result = Data.AsQueryable().Where(spec.ToExpression()).ToList();
    // Only item with Value=5 has Node.Child.Depth=15 > 10
    Assert.Single(result);
    Assert.Equal(5, result[0].Value);
  }
}
