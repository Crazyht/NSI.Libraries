# NSI.Specifications.Npgsql

Provider-specific optimizations for `NSI.Specifications` targeting PostgreSQL via the Npgsql EF Core provider.

## Features

* Case-insensitive `Contains` / `StartsWith` / `EndsWith` rewritten to `ILIKE`.
* Safe wildcard escaping (`%`, `_`, `\\`) preserving literal intent.
* Opt-in registration (no side effects unless you call the extension).

## Installation

```bash
dotnet add package NSI.Specifications.Npgsql --prerelease
```

## Registration

```csharp
services.AddNpgsqlSpecifications(); // internally calls NpgsqlTextOptimizations.RegisterAll()
```

This registers optimization entries under provider code `"Pg"`. When the active EF Core query root is resolved as Npgsql, eligible text specifications are rewritten just before translation.

## Eligibility Rules

A text specification (`ContainsSpecification<T>`, `StartsWithSpecification<T>`, `EndsWithSpecification<T>`) is rewritten only if:

1. The underlying lambda body is an `&&` chain whose terminal expression is the text method call.
2. Both the member access and the search term are explicitly lowered (`x.Name.ToLower().Contains(term.ToLower())`). Supports `ToLower()` and `ToLowerInvariant()`.
3. A null guard for the member path exists somewhere in the `&&` chain (`x.Name != null`).
4. The search term is non-empty.

If any condition fails the original expression is used unchanged.

## Escaping Logic

Only three characters are escaped because they affect PostgreSQL `LIKE` / `ILIKE` semantics:

| Character | Meaning | Escaped As |
|-----------|---------|------------|
| `%`       | Multi-character wildcard | `\%` |
| `_`       | Single-character wildcard | `\_` |
| `\\`     | Escape character | `\\\\` |

Order of operations: backslash first, then `%`, then `_`.

Example (`ContainsSpecification`): term = `a_%b\\c%d` → pattern fragment `%a\_\%b\\c\%d%` → SQL: `ILIKE '%a\_\%b\\c\%d%'`.

## Why Not Use `ILIKE` Always?

We only translate explicit ignore-case usages (both sides lowered) to avoid changing semantics of intentionally case-sensitive queries and to keep provider independence for default specs.

## Testing

Integration tests assert:

* Rewrites occur only for `ignoreCase: true` specs.
* No rewrite for empty terms.
* Correct pattern escaping for `%`, `_`, backslash and combinations.

## Extending

Future provider-specific optimizations can register via:

```csharp
SpecOptimizationRegistry.Register("Pg", new CustomOptimization());
```

Where `CustomOptimization` implements `IFilterOptimization`.

## Versioning

Ships alongside core specifications; still pre-release until additional patterns (equals ignoring accents, etc.) are added.

## License

MIT
