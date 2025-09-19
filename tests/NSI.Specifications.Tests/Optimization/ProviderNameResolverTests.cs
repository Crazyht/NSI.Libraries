using System.Collections;
using System.Linq.Expressions;
using NSI.Specifications.Optimization;
using Xunit;

namespace NSI.Specifications.Tests.Optimization;

public class ProviderNameResolverTests {
  private sealed class DummyProvider: IQueryProvider {
    public IQueryable CreateQuery(Expression expression) =>
      new DummyQueryable(expression, this);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
      new DummyQueryable<TElement>(expression, this);

    public object? Execute(Expression expression) => null;

    public TResult Execute<TResult>(Expression expression) => default!;
  }

  private sealed class DummyQueryable(Expression expr, IQueryProvider prov): IQueryable {
    public IEnumerator GetEnumerator() => Enumerable.Empty<object>().GetEnumerator();
    public Expression Expression { get; } = expr;
    public IQueryProvider Provider { get; } = prov;
    public Type ElementType => typeof(object);
  }

  private sealed class DummyQueryable<T>(Expression expr, IQueryProvider prov): IQueryable<T> {
    public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public Expression Expression { get; } = expr;
    public IQueryProvider Provider { get; } = prov;
    public Type ElementType => typeof(T);
  }

  [Theory]
  [InlineData(
    "Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal.NpgsqlQueryProvider",
    "Pg"
  )]
  [InlineData(
    "Microsoft.EntityFrameworkCore.Query.Internal.SqlServerQueryProvider",
    "SqlServer"
  )]
  [InlineData(
    "Microsoft.EntityFrameworkCore.Query.Internal.SqliteQueryProvider",
    "Sqlite"
  )]
  [InlineData(
    "Pomelo.EntityFrameworkCore.MySql.Query.Internal.MySqlQueryProvider",
    "MySql"
  )]
  [InlineData(
    "Microsoft.EntityFrameworkCore.Query.Internal.InMemoryQueryProvider",
    "InMemory"
  )]
  [InlineData("Something.Unknown.Provider", "EfCore")]
  public void Resolve_FromName_Works(string name, string expected) {
    var code = ProviderNameResolver.Resolve(name);
    Assert.Equal(expected, code);
  }

  [Fact]
  public void Resolve_FromQueryable_Defaults_WhenUnknown() {
    var q = new DummyQueryable<int>(Expression.Constant(1), new DummyProvider());
    var code = ProviderNameResolver.Resolve(q);
    Assert.Equal("EfCore", code);
  }
}
