using System.Collections.Generic;
using System.Linq;
using NSI.Specifications.Sorting;
using Xunit;

namespace NSI.Specifications.Tests.Sorting;

/// <summary>
/// Tests for sorting specifications.
/// </summary>
public class SortingSpecificationTests
{
    private static readonly int[] ExpectedSingle = [1, 2];
    private static readonly (int A, int B)[] ExpectedTuples = [(0, 5), (1, 2), (1, 1)];
    private sealed class Item
    {
        public int A { get; init; }
        public int B { get; init; }
    }

    [Fact]
    public void SingleClause_ShouldSortAscending()
    {
        List<Item> data = [new() { A = 2 }, new() { A = 1 }];
        var spec = Sort.Create<Item, int>(x => x.A);
        var result = data.AsQueryable().OrderBy(spec).ToList();
        Assert.Equal(ExpectedSingle, result.Select(r => r.A).ToArray());
    }

    [Fact]
    public void MultiClause_ShouldApplyThenOrdering()
    {
        List<Item> data = [
            new() { A = 1, B = 2 },
            new() { A = 1, B = 1 },
            new() { A = 0, B = 5 }
        ];
        var spec = Sort.Create<Item, int>(x => x.A).Then(x => x.B, SortDirection.Desc);
        var result = data.AsQueryable().OrderBy(spec).ToList();
        Assert.Equal(ExpectedTuples, result.Select(r => (r.A, r.B)).ToArray());
    }
}
