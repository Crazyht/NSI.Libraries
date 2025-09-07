using System.Linq;
using NSI.Specifications.Optimization;
using Xunit;

namespace NSI.Specifications.Tests.Optimization;

public class ProviderNameResolverTests
{
    private sealed class DummyProvider : IQueryProvider
    {
        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new DummyQueryable(expression, this);
        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new DummyQueryable<TElement>(expression, this);
        public object? Execute(System.Linq.Expressions.Expression expression) => null;
        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => default!;
    }

    private sealed class DummyQueryable : IQueryable
    {
        public DummyQueryable(System.Linq.Expressions.Expression expr, IQueryProvider prov) { Expression = expr; Provider = prov; }
        public System.Collections.IEnumerator GetEnumerator() => System.Linq.Enumerable.Empty<object>().GetEnumerator();
        public System.Linq.Expressions.Expression Expression { get; }
        public IQueryProvider Provider { get; }
        public System.Type ElementType => typeof(object);
    }

    private sealed class DummyQueryable<T> : IQueryable<T>
    {
        public DummyQueryable(System.Linq.Expressions.Expression expr, IQueryProvider prov) { Expression = expr; Provider = prov; }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() => System.Linq.Enumerable.Empty<T>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public System.Linq.Expressions.Expression Expression { get; }
        public IQueryProvider Provider { get; }
        public System.Type ElementType => typeof(T);
    }

    [Theory]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal.NpgsqlQueryProvider", "Pg")]
    [InlineData("Microsoft.EntityFrameworkCore.Query.Internal.SqlServerQueryProvider", "SqlServer")]
    [InlineData("Microsoft.EntityFrameworkCore.Query.Internal.SqliteQueryProvider", "Sqlite")]
    [InlineData("Pomelo.EntityFrameworkCore.MySql.Query.Internal.MySqlQueryProvider", "MySql")]
    [InlineData("Microsoft.EntityFrameworkCore.Query.Internal.InMemoryQueryProvider", "InMemory")]
    [InlineData("Something.Unknown.Provider", "EfCore")]
    public void Resolve_FromName_Works(string name, string expected)
    {
        var code = ProviderNameResolver.Resolve(name);
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Resolve_FromQueryable_Defaults_WhenUnknown()
    {
        var q = new DummyQueryable<int>(System.Linq.Expressions.Expression.Constant(1), new DummyProvider());
        var code = ProviderNameResolver.Resolve(q);
        Assert.Equal("EfCore", code);
    }
}
