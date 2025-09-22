using NSI.Specifications.Core;
using Xunit;

namespace NSI.Specifications.Tests;
/// <summary>
/// Tests for core specification combinators.
/// </summary>
public sealed class CoreSpecificationTests {
  private sealed record TestItem(string Name, bool Active);

  private static readonly TestItem[] Sample = [
    new("Alpha", true),
    new("Beta", true),
    new("Alpha", false)
  ];

  private static readonly TestItem[] OrSample = [
    new("Alpha", false),
    new("Beta", false),
    new("Charlie", true)
  ];

  private static readonly string[] ExpectedOrNames = ["Beta", "Charlie"];

  private sealed class ActiveItemSpec: Specification<TestItem> {
    public override System.Linq.Expressions.Expression<System.Func<TestItem, bool>> ToExpression() =>
      x => x.Active;
  }

  private sealed class NameStartsWithSpec(string prefix): Specification<TestItem> {
    public override System.Linq.Expressions.Expression<System.Func<TestItem, bool>> ToExpression() =>
      x => x.Name.StartsWith(prefix);
  }

  private static readonly TestItem[] NotSample = [
    new("A", true),
    new("B", false)
  ];

  [Fact]
  public void And_ComposesCorrectly() {
    var spec = new ActiveItemSpec().And(new NameStartsWithSpec("A"));
    var filtered = Sample
      .AsQueryable()
      .Where(spec.ToExpression())
      .ToList();

    Assert.Single(filtered);
    Assert.Equal("Alpha", filtered[0].Name);
  }

  [Fact]
  public void Or_ComposesCorrectly() {
    var spec = new ActiveItemSpec().Or(new NameStartsWithSpec("B"));
    var filtered = OrSample
      .AsQueryable()
      .Where(spec.ToExpression())
      .OrderBy(x => x.Name)
      .ToList();
    var actual = filtered.Select(x => x.Name).ToArray();

    Assert.Equal(ExpectedOrNames, actual);
  }

  [Fact]
  public void Not_NegatesCorrectly() {
    var spec = new ActiveItemSpec().Not();
    var filtered = NotSample
      .AsQueryable()
      .Where(spec.ToExpression())
      .ToList();

    Assert.Single(filtered);
    Assert.Equal("B", filtered[0].Name);
  }
}
