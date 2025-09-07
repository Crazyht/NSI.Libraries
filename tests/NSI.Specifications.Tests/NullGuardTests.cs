using System;
using System.Linq.Expressions;
using NSI.Specifications.Core;
using Xunit;

namespace NSI.Specifications.Tests;

/// <summary>
/// Tests for the <see cref="NullGuard"/> helper.
/// </summary>
public sealed class NullGuardTests {
  private sealed class Node {
    public Node? Child { get; init; }
    public string? Name { get; init; }
  }

  [Fact]
  public void Safe_NullIntermediate_ReturnsFalseWithoutThrow() {
  var expr = NullGuard.Safe<Node, string?>(n => n.Child!.Child!.Name, name => name != null && name.Length > 0 && name[0] == 'X');
    var func = expr.Compile();
    var root = new Node { Child = null };
    var result = func(root);
    Assert.False(result);
  }

  [Fact]
  public void Safe_FullPathSatisfied_ReturnsTrue() {
    var leaf = new Node { Name = "Xylophone" };
    var mid = new Node { Child = leaf };
    var root = new Node { Child = new Node { Child = mid } };
  var expr = NullGuard.Safe<Node, string?>(n => n.Child!.Child!.Child!.Name, name => name != null && name.Length > 0 && name[0] == 'X');
    var compiled = expr.Compile();
    Assert.True(compiled(root));
  }
}
