using System.Linq.Expressions;
using NSI.Specifications.Abstractions;
using NSI.Specifications.Filtering;
using NSI.Specifications.Optimization;
using Xunit;

namespace NSI.Specifications.Tests.Optimization;

public class WhereExtensionsTests {
  private sealed class TestSpec(Expression<Func<int, bool>> expr): ISpecification<int> {
    public Expression<Func<int, bool>> ToExpression() => expr;
    public bool IsSatisfiedBy(int candidate) => expr.Compile()(candidate);
  }

  private sealed class FakeOptimization: IFilterOptimization {
    public Type EntityType => typeof(int);
    public Type SpecificationType => typeof(TestSpec);
    public LambdaExpression? TryRewriteLambda(object specification) {
      if (specification is TestSpec) {
        Expression<Func<int, bool>> expr = x => x % 2 == 0; // force even numbers
        return expr;
      }

      return null;
    }
  }

  [Fact]
  public void Where_IEnumerable_FallsBack() {
    var data = Enumerable.Range(1, 5);
    var spec = new TestSpec(x => x > 3);
    var result = data.Where(spec).ToArray();
    Assert.Equal(ExpectedFallback, result);
  }

  [Fact]
  public void Where_IQueryable_UsesOptimization_WhenRegistered() {
    SpecOptimizationRegistry.Register("EfCore", new FakeOptimization());
    var data = Enumerable.Range(1, 6).AsQueryable();
    var spec = new TestSpec(x => x > 3); // would yield 4,5,6 without optimization
    var result = data.Where(spec).ToArray();
    Assert.Equal(ExpectedOptimized, result); // optimized to even numbers
  }

  private static readonly int[] ExpectedFallback = [4, 5];
  private static readonly int[] ExpectedOptimized = [2, 4, 6];
}
