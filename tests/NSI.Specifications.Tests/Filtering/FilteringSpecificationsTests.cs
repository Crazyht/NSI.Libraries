using System;
using System.Linq;
using NSI.Specifications.Filtering;
using Xunit;

namespace NSI.Specifications.Tests.Filtering;

/// <summary>
/// Tests for foundational filtering specifications.
/// </summary>
public sealed class FilteringSpecificationsTests {
  private static readonly string[] _ExpectedNames = ["A", "C"];
  private sealed class Node {
    public Node? Child { get; init; }
    public string? Name { get; init; }
    public string? Tag { get; init; }
  }

  [Fact]
  public void EqualsSpecification_MatchAndNoMatch() {
    var spec = new EqualsSpecification<Node, string?>(n => n.Name, "A");
    var data = new[] { new Node { Name = "A" }, new Node { Name = "B" } };
    var result = data.AsQueryable().Where(spec.ToExpression()).ToList();
    Assert.Single(result);
    Assert.Equal("A", result[0].Name);
  }

  [Fact]
  public void EqualsSpecification_DeepPathNull_ReturnsFalse() {
    var spec = new EqualsSpecification<Node, string?>(n => n.Child!.Name, "X");
    var data = new[] { new Node { Child = null } };
    var result = data.AsQueryable().Where(spec.ToExpression()).ToList();
    Assert.Empty(result);
  }

  [Fact]
  public void InSpecification_FiltersCorrectly() {
    var spec = new InSpecification<Node, string?>(n => n.Name, _ExpectedNames);
    var data = new[] { new Node { Name = "A" }, new Node { Name = "B" }, new Node { Name = "C" } };
    var result = data.AsQueryable().Where(spec.ToExpression()).OrderBy(n => n.Name).ToList();
    var names = result.Select(n => n.Name).OfType<string>().ToArray();
    Assert.Equal(_ExpectedNames, names);
  }

  [Fact]
  public void InSpecification_EmptySet_AlwaysFalse() {
    var spec = new InSpecification<Node, string?>(n => n.Name, []);
    var data = new[] { new Node { Name = "A" } };
    var result = data.AsQueryable().Where(spec.ToExpression()).ToList();
    Assert.Empty(result);
  }

  [Fact]
  public void IsNullSpecification_IntermediateNull_ReturnsTrue() {
    var spec = new IsNullSpecification<Node, string>(n => n.Child!.Name!);
    var data = new[] { new Node { Child = null } };
    var result = data.AsQueryable().Where(spec.ToExpression()).ToList();
    Assert.Single(result);
  }

  [Fact]
  public void IsEmptySpecification_StringEmpty_ReturnsTrue() {
    var spec = new IsEmptySpecification<Node>(n => n.Tag!);
    var data = new[] { new Node { Tag = string.Empty }, new Node { Tag = null } };
    var result = data.AsQueryable().Where(spec.ToExpression()).ToList();
    Assert.Single(result);
    Assert.Equal(string.Empty, result[0].Tag);
  }
}
