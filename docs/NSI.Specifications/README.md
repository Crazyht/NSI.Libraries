# NSI.Specifications

Composable, testable, provider-aware specification pattern utilities for .NET / EF Core.

## Why

Avoid scattering ad‑hoc `Where`, `Include`, `OrderBy` and projection logic across repositories / services. Encapsulate *intent* as reusable specification objects that can:

* Combine logically (And / Or / Not)
* Translate to pure expression trees (EF Core friendly)
* Execute in-memory (fallback / unit tests)
* Be provider-optimized (e.g. PostgreSQL ILIKE rewrites)

## Install

```bash
dotnet add package NSI.Specifications --prerelease
# Optional provider optimizations (e.g. Npgsql)
dotnet add package NSI.Specifications.Npgsql --prerelease
```

## Quick Start

```csharp
// Filter authors whose name contains "al" (case-insensitive), include books, sort, project names.
var filter = new ContainsSpecification<Author>(a => a.Name, "al", ignoreCase: true);
var include = new IncludeSpecification<Author>(chains: [ IncludeChains.For<Author>(a => a.Books) ]);
var sort = SortSpecification<Author>.FromSingle(a => a.Name, SortDirection.Asc);
var project = new ProjectionSpecification<Author, string>(a => a.Name);

string[] names = context.Authors
	.Where(filter)       // provider-optimizable
	.Include(include)    // reflection-driven Include/ThenInclude chain
	.OrderBy(sort)       // stable multi-clause ordering
	.Select(project)     // typed projection
	.ToArray();
```

## Core Concepts

| Concept | Interface | Notes |
|---------|-----------|-------|
| General spec | `ISpecification<T>` | Exposes `ToExpression()` & `IsSatisfiedBy()` |
| Filtering | `IFilterSpecification<T>` | Builds `Expression<Func<T,bool>>` |
| Sorting | `ISortSpecification<T>` | Maintains ordered clause list |
| Projection | `IProjectionSpecification<TSource,TResult>` | Wraps selector expression |
| Include | `IIncludeSpecification<T>` | Typed lambda chains + string paths |

### Composition

`specA.And(specB)`, `specA.Or(specB)`, `spec.Not()` return new composed specifications without invoking delegates (pure tree merging, no `Expression.Invoke`).

## Filtering

Implemented (representative subset):

* Equality / In
* Null / Empty guards (deep safe navigation)
* Comparison (>, >=, <, <=, Between)
* Text: Contains / StartsWith / EndsWith (case handling)

All text specs optionally ignore case via lowercasing (can be optimized by providers later).

## Includes

Typed chains:

```csharp
var include = new IncludeSpecification<Order>(chains: [
	IncludeChains.For<Order>(o => o.Customer, c => c.Address)
]);
```

String paths supported simultaneously: `new IncludeSpecification<Order>(stringPaths: ["Customer.Address"])`.

## Sorting

```csharp
var sort = SortSpecification<User>
	.FromSingle(u => u.LastName, SortDirection.Asc)
	.Then(u => u.FirstName)
	.Then(u => u.CreatedAt, SortDirection.Desc);
```

Works for both IQueryable (translates) & IEnumerable (compiled delegates) using ordered chaining.

## Projection

Wrap your projection once, reuse everywhere, preserve translation:

```csharp
var toDto = new ProjectionSpecification<User, UserDto>(u => new UserDto(u.Id, u.Email));
var dtos = context.Users.Select(toDto).ToArray();
```

## Provider Optimization Registry

Hook that lets providers (e.g. PostgreSQL) register rewrites:

```csharp
// At startup (after adding the Npgsql assembly reference)
services.AddNpgsqlSpecifications(); // calls RegisterAll()
```

Optimizations can supply alternative expressions (e.g. ILIKE) when `ProviderNameResolver` resolves provider code ("Pg"). If none match, the base specification expression is used.

## Execution Paths

| Context | Where | Include | OrderBy | Select |
|---------|-------|---------|---------|--------|
| IQueryable | Expression preserved | Reflection Include/ThenInclude | Composed expression chain | `Queryable.Select` |
| IEnumerable | Compiled predicate | No-op | Delegated LINQ ordering | Compiled selector |

## Testing Strategy

* Pure unit tests exercise expression shape & composition
* Pipeline tests cover full chaining (Where → Include → OrderBy → Select)
* Provider-specific tests (future) assert SQL patterns via `ToQueryString()`

## Performance Notes

* `SpecOptimizationRegistry` lookups use concurrent dictionaries; overhead negligible vs EF translation.
* No delegate invocation in composed spec trees (avoids `Expression.Invoke`).
* IEnumerable path compiles each projection/sort key once per call site.

## Extensibility

Add a new filter spec:

```csharp
public sealed class ActiveSpecification<T>(Expression<Func<T,bool>> isActive) : Specification<T>, IFilterSpecification<T> {
	private readonly Expression<Func<T,bool>> _Expr = isActive ?? throw new ArgumentNullException(nameof(isActive));
	public override Expression<Func<T,bool>> ToExpression() => _Expr;
}
```

Register an optimization (example skeleton):

```csharp
SpecOptimizationRegistry.Register("Pg", new MyStartsWithOptimization());
```

## Roadmap

* Real PostgreSQL ILIKE rewrites (Issue #16)
* Translation validation suite (Issue #15)
* Pagination helpers (Skip/Take spec)
* Query builder façade

## Versioning

Pre-release `0.1.0-preview.*` until optimization layer finalized.

## License

MIT

---

For more examples see test project `NSI.Specifications.Tests`.
