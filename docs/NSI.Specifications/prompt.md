# PROMPT COMPLET — Générer une librairie C# du pattern Specification

## (IEnumerable / IQueryable, EF Core 9) avec : Tri fluent `Then(...)`, **Include**, filtres standards, et **null‑safe path**

> **À copier‑coller tel quel** pour générer une solution .NET 9 prête à l’emploi.

---

## 🎯 Rôle & mission

Tu es un expert **C#/.NET 9**, **EF Core 9**, et **design d’API**. Tu dois produire une **librairie de specifications** appliquées via **extensions LINQ** pour `IEnumerable<T>` et `IQueryable<T>`, **sans méthode `Apply`** dans les specs. Le design doit :

* proposer **filtres standards** (And/Or/Not/Equals/Between/>/>=/\</<=/IsNull/IsEmpty/StartsWith/EndsWith/Contains/In/…)
* offrir des comparaisons **string case‑sensitive ou insensitives** au choix
* être **null‑safe** pour tout chemin d’expression (ex : `p => p.Model.Power.EnergyClass`)
* inclure un système de **tri fluent `Then(...)`**
* ajouter des **includes** via `IIncludeSpecification<T>` (compatibles EF Core `Include`/`ThenInclude`)
* permettre des **optimisations provider** (ex : Postgres `ILIKE`) via un **registre** externe

---

## ✅ Objectifs fonctionnels

1. **Interfaces publiques** :

   * `ISpecification` (marqueur)
   * `IFilterSpecification<T>`
   * `ISortSpecification<T>` + `ISortClause<T>` (tri multi‑colonnes) + **API fluent `Then(...)`**
   * `IProjectionSpecification<TSource,TResult>`
   * **`IIncludeSpecification<T>`** (chaînes d’include)
2. **Extensions LINQ** sur `IQueryable<T>` & `IEnumerable<T>` :

   * `Where(IFilterSpecification<T>)`
   * `OrderBy(ISortSpecification<T>)`
   * `Select(IProjectionSpecification<T,TResult>)`
   * **`Include(IIncludeSpecification<T>)`** (no‑op sur `IEnumerable<T>`, active sur `IQueryable<T>`)
3. **Null‑safety** :

   * Toute spec acceptant un `selector` doit **générer** des expressions qui *n’évaluent pas* de membre si l’un des segments du chemin est `null` (pas d’`NRE`).
   * Exemple : `InSpecification<Product>(p => p.Model.Power.EnergyClass, ["A+","A"])` ne jette pas si `Model`/`Power`/`EnergyClass` est `null`.
4. **Case‑sensitivity configurables** pour les `string`.
5. **Optimisations provider** : registre `(provider, baseSpec) → expression optimisée` (ex : Npgsql `ILIKE`).

---

## 🧩 Design de l’API publique

### Marqueur

```csharp
public interface ISpecification {}
```

### Filtre

```csharp
public interface IFilterSpecification<T> : ISpecification
{
    // Expression portable (EF traduisible / LINQ compilable)
    Expression<Func<T, bool>> Criteria { get; }

    // Optionnel : prédicat in‑memory spécifique (sinon on Compile() Criteria)
    Func<T, bool>? Condition => null;
}
```

#### Filtres standards à implémenter

> Chaque classe **doit** : (1) exposer le/les paramètres, (2) construire `Criteria` **null‑safe**, (3) proposer `Condition` si utile pour in‑memory, (4) pour `string`, accepter un paramètre `ignoreCase` (ou un enum `StringComparisonMode`).

* **Logiques** : `AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>`
* **Égalité** : `EqualsSpecification<T, TKey>`
* **In** : `InSpecification<T, TKey>`
* **Comparaisons** : `GreaterThanSpecification<T, TKey>`, `GreaterThanOrEqualSpecification<T, TKey>`, `LessThanSpecification<T, TKey>`, `LessThanOrEqualSpecification<T, TKey>` (avec `TKey : IComparable<TKey>`)
* **Between** : `BetweenSpecification<T, TKey>` (bornes inclusives/exclusives configurables)
* **Null / Empty** : `IsNullSpecification<T, TKey>`, `IsEmptySpecification<T>` (string ou ICollection)
* **Texte** : `StartsWithSpecification<T>`, `EndsWithSpecification<T>`, `ContainsSpecification<T>` (toutes avec `ignoreCase`)

> Fournir un **helper interne** `NullGuard` pour générer des gardes `x.Prop1 != null && x.Prop1.Prop2 != null && …` et obtenir l’expression de valeur finale à comparer.

##### Exemple attendu — `InSpecification`

```csharp
public sealed class InSpecification<T, TKey> : IFilterSpecification<T>
{
    public InSpecification(Expression<Func<T, TKey>> selector, IEnumerable<TKey> values,
                           IEqualityComparer<TKey>? comparer = null)
    { /* construit Criteria null‑safe : guard && values.Contains(valueExpr) */ }

    public Expression<Func<T, bool>> Criteria { get; }
    public Func<T, bool>? Condition { get; } // optionnel : values.Contains(selector(entity)) avec null‑guard
}
```

##### Exemple attendu — `StartsWithSpecification` (string)

* Paramètres : `Expression<Func<T,string?>> selector`, `string prefix`, `bool ignoreCase = true`
* **Base portable** : si `ignoreCase`, normaliser (`ToLowerInvariant`) **des deux côtés** ou utiliser un **dialect rewriter** comme fallback (cf. optimisations) ; sinon `StartsWith(prefix)` direct.
* **Optimisation Npgsql** : via registre → `EF.Functions.ILike(field, prefix + "%")`.

### Tri — **fluent `Then(...)`**

```csharp
public enum SortDirection { Ascending, Descending }

public interface ISortClause<T>
{
    LambdaExpression KeySelector { get; }
    SortDirection Direction { get; }
    int OrderIndex { get; }
}

public interface ISortSpecification<T> : ISpecification
{
    IReadOnlyList<ISortClause<T>> Clauses { get; }
}

public sealed class SortClause<T, TKey> : ISortClause<T>
{
    public SortClause(Expression<Func<T, TKey>> key, SortDirection dir, int index)
    { KeySelector = key ?? throw new ArgumentNullException(nameof(key)); Direction = dir; OrderIndex = index; }
    public LambdaExpression KeySelector { get; }
    public SortDirection Direction { get; }
    public int OrderIndex { get; }
}

public sealed class SortSpecification<T> : ISortSpecification<T>
{
    private readonly List<ISortClause<T>> _clauses;
    internal SortSpecification(List<ISortClause<T>> clauses) => _clauses = clauses;
    public IReadOnlyList<ISortClause<T>> Clauses => _clauses;
}

public static class Sort
{
    public static SortSpecification<T> SortSpecification<T, TKey>(Expression<Func<T, TKey>> key, SortDirection direction)
        => new SortSpecification<T>(new List<ISortClause<T>> { new SortClause<T, TKey>(key, direction, 0) });

    public static SortSpecification<T> Then<T>(this SortSpecification<T> first, SortSpecification<T> next)
    {
        var merged = new List<ISortClause<T>>(first.Clauses.Count + next.Clauses.Count);
        merged.AddRange(first.Clauses);
        var start = merged.Count;
        for (int i = 0; i < next.Clauses.Count; i++)
        {
            var c = next.Clauses[i];
            merged.Add(new SortClause<T, object>((Expression<Func<T, object>>)ToObjectLambda(c.KeySelector), c.Direction, start + i));
        }
        return new SortSpecification<T>(merged);
    }

    public static SortSpecification<T> Then<T, TKey>(this SortSpecification<T> first, Expression<Func<T, TKey>> key, SortDirection direction)
        => first.Then(SortSpecification<T, TKey>(key, direction));

    public static ISortSpecification<T> Then<T>(this ISortSpecification<T> first, ISortSpecification<T> next)
        => (first as SortSpecification<T> ?? new SortSpecification<T>(first.Clauses.ToList()))
           .Then(next as SortSpecification<T> ?? new SortSpecification<T>(next.Clauses.ToList()));

    private static LambdaExpression ToObjectLambda(LambdaExpression le)
    {
        var p = le.Parameters[0];
        return Expression.Lambda(Expression.Convert(le.Body, typeof(object)), p);
    }
}
```

### Projection

```csharp
public interface IProjectionSpecification<TSource, TResult> : ISpecification
{
    Expression<Func<TSource, TResult>> Selector { get; }
}
```

### **Include**

```csharp
public interface IIncludeSpecification<T> : ISpecification
{
    // Chaînes typées d’include (Segments[0] est le premier Include, puis ThenInclude…)
    IReadOnlyList<IIncludeChain<T>> Chains { get; }

    // Fallback optionnel : chemins en string ("Orders.Items.Product")
    IReadOnlyList<string>? StringPaths => null;
}

public interface IIncludeChain<T>
{
    IReadOnlyList<LambdaExpression> Segments { get; } // ex: [x=>x.Orders, o=>o.Items, i=>i.Product]
}

public sealed class IncludeSpecification<T> : IIncludeSpecification<T>
{
    public IncludeSpecification(IEnumerable<IIncludeChain<T>> chains, IEnumerable<string>? paths = null) { /* ... */ }
    public IReadOnlyList<IIncludeChain<T>> Chains { get; }
    public IReadOnlyList<string>? StringPaths { get; }
}
```

#### Extensions `Include`

* `IQueryable<T>` :

  * Pour chaque `chain.Segments` :

    * appliquer `Include(first)` puis `ThenInclude(next)` successifs ;
    * gérer référence vs collection (sélectionner le bon overload via helpers/reflection typée) ;
  * si `StringPaths` fournis, les appliquer avec `Include(path)`.
* `IEnumerable<T>` : **no‑op** (retourne la source telle quelle).

> Fournir un `IncludeExpressionHelper` qui choisit les bons overloads `Include/ThenInclude` et applique la chaîne.

---

## 🔧 Extensions d’application (Where/OrderBy/Select/Include)

```csharp
public static class SpecificationExtensions
{
    // WHERE
    public static IQueryable<T> Where<T>(this IQueryable<T> source, IFilterSpecification<T> spec)
    {
        var provider = ProviderNameResolver.Resolve(source);
        if (SpecOptimizationRegistry.TryGetOptimizedCriteria(provider, spec, out var optimized))
            return Queryable.Where(source, optimized);
        return Queryable.Where(source, spec.Criteria);
    }

    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, IFilterSpecification<T> spec)
        => Enumerable.Where(source, (spec.Condition ?? spec.Criteria.Compile()));

    // ORDER BY (respect de l’ordre Then(...))
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, ISortSpecification<T> sort) { /* impl comme précédemment */ }
    public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, ISortSpecification<T> sort) { /* idem */ }

    // SELECT
    public static IQueryable<TResult> Select<T, TResult>(this IQueryable<T> source, IProjectionSpecification<T, TResult> spec)
        => Queryable.Select(source, spec.Selector);
    public static IEnumerable<TResult> Select<T, TResult>(this IEnumerable<T> source, IProjectionSpecification<T, TResult> spec)
        => Enumerable.Select(source, spec.Selector.Compile());

    // INCLUDE
    public static IQueryable<T> Include<T>(this IQueryable<T> source, IIncludeSpecification<T> spec)
        => IncludeExpressionHelper.Apply(source, spec);

    public static IEnumerable<T> Include<T>(this IEnumerable<T> source, IIncludeSpecification<T> spec)
        => source; // no-op in-memory
}
```

---

## 🧠 Null‑safe path — helper requis

Implémente un helper interne **générique** pour transformer n’importe quel `selector : Expression<Func<T,TValue>>` en un triplet :

* `ParameterExpression param`
* `Expression valueExpr` (accès final)
* `Expression nullGuard` (conjonction de `!= null` pour chaque segment)

Exemple : `p => p.Model.Power.EnergyClass` →

* `valueExpr = p.Model.Power.EnergyClass`
* `nullGuard = p.Model != null && p.Model.Power != null && p.Model.Power.EnergyClass != null`

Pour une spec `In`, composer : `entity => nullGuard && values.Contains(valueExpr)`

> **Exigence** : la **composition doit rester traduisible** par EF Core 9 (aucun `Compile()` à l’intérieur de `Criteria`).

---

## 🚀 Optimisations provider (registre)

Réutilise le **registre d’optimisations** `(provider, baseSpecOpenType) → optimizationOpenType` :

* Interface `IFilterOptimization` + `FilterOptimization<T,TSpec>` (déjà définies)
* `ProviderNameResolver.Resolve(IQueryable source)` renvoie "Pg", "SqlServer", "Sqlite", "MySql", "InMemory", "EfCore"
* Exemple Npgsql : `PgStartsWithOptimization<T>` transforme `StartsWith` insensible en `EF.Functions.ILike(field, prefix + "%")` (utiliser `MethodInfo` par **méthode‑groupe**, pas `GetMethod("…")`).

> Le registre **ne concerne que les filtres**. Le tri et les includes n’en ont généralement pas besoin.

---

## 🧪 Tests à produire

* **Null‑safety** : chaque spec avec un chemin profond **ne jette jamais** et renvoie le bon résultat.
* **Filtres string** : vérifier `ignoreCase` en in‑memory et SQL (pour Npgsql, `ToQueryString()` contient `ILIKE`).
* **In** : `Contains` côté EF génère un `IN (...)` (liste paramétrée) ; avec chemins `null`, le résultat attendu est `false`.
* **Tri fluent** : `ToQueryString()` montre `ORDER BY a DESC, b ASC, ...` ; en mémoire, l’ordre est identique à `OrderBy/ThenBy` manuel.
* **Include** : les chaînes typées appliquent `Include/ThenInclude` corrects ; `StringPaths` appliqués via `Include(path)`.

---

## 📦 Packaging & Qualité

* Target : `net9.0`, `nullable enable`.
* Analyzers : `Microsoft.CodeAnalysis.NetAnalyzers`, `StyleCop.Analyzers`.
* Docs XML publiques.
* README détaillé (exemples d’usage, limitations, perf tips).
* License MIT, SemVer.

---

## 📁 Structure de solution souhaitée

```
/specs
  ├─ src
  │   ├─ YourCompany.Specifications
  │   │   ├─ Abstractions
  │   │   │   ├─ ISpecification.cs
  │   │   │   ├─ IFilterSpecification.cs
  │   │   │   ├─ ISortSpecification.cs
  │   │   │   ├─ IProjectionSpecification.cs
  │   │   │   └─ IIncludeSpecification.cs
  │   │   ├─ Core
  │   │   │   ├─ SpecificationExtensions.cs
  │   │   │   ├─ ProviderNameResolver.cs
  │   │   │   ├─ SpecOptimizationRegistry.cs
  │   │   │   ├─ FilterOptimization.cs
  │   │   │   └─ NullGuard.cs
  │   │   ├─ Filtering (standards)
  │   │   │   ├─ AndSpecification.cs
  │   │   │   ├─ OrSpecification.cs
  │   │   │   ├─ NotSpecification.cs
  │   │   │   ├─ EqualsSpecification.cs
  │   │   │   ├─ InSpecification.cs
  │   │   │   ├─ GreaterThanSpecification.cs
  │   │   │   ├─ GreaterThanOrEqualSpecification.cs
  │   │   │   ├─ LessThanSpecification.cs
  │   │   │   ├─ LessThanOrEqualSpecification.cs
  │   │   │   ├─ BetweenSpecification.cs
  │   │   │   ├─ IsNullSpecification.cs
  │   │   │   ├─ IsEmptySpecification.cs
  │   │   │   ├─ StartsWithSpecification.cs
  │   │   │   ├─ EndsWithSpecification.cs
  │   │   │   └─ ContainsSpecification.cs
  │   │   ├─ Sorting
  │   │   │   ├─ SortClause.cs
  │   │   │   ├─ SortSpecification.cs
  │   │   │   └─ SortApi.cs  // `using static` Sort
  │   │   ├─ Projection
  │   │   │   └─ ProjectionSpecification.cs
  │   │   └─ Include
  │   │       ├─ IIncludeChain.cs
  │   │       ├─ IncludeSpecification.cs
  │   │       └─ IncludeExpressionHelper.cs
  │   └─ YourCompany.Specifications.Npgsql
  │       ├─ PgMethods.cs
  │       ├─ PgStartsWithOptimization.cs
  │       ├─ PgContainsOptimization.cs
  │       └─ PgEndsWithOptimization.cs
  └─ tests
      ├─ YourCompany.Specifications.Tests
      └─ YourCompany.Specifications.Npgsql.Tests
```

---

## 🧑‍💻 Exemples d’usage

```csharp
using static YourCompany.Specifications.Sort;

// Filtres (null‑safe) + tri fluent + include + projection
var filter = new InSpecification<Product, string>(p => p.Model.Power.EnergyClass, new[] { "A+", "A" });
var sort   = SortSpecification<Customer>(c => c.Ranking, SortDirection.Descending)
             .Then(SortSpecification<Customer>(c => c.Stats.TotalAmoung, SortDirection.Ascending))
             .Then(SortSpecification<Customer>(c => c.Lastname, SortDirection.Ascending))
             .Then(SortSpecification<Customer>(c => c.Firstname, SortDirection.Ascending));

var include = new IncludeSpecification<Customer>(
    chains: new[] {
        IncludeChains.For<Customer>(c => c.Orders, (Order o) => o.Items, (OrderItem i) => i.Product)
    }
);

var query = db.Customers
    .Where(filter)
    .Include(include)
    .OrderBy(sort)
    .Select(new ProjectionSpecification<Customer, CustomerDto>(c => new CustomerDto
    {
        Id = c.Id,
        Name = c.Firstname + " " + c.Lastname,
        Ranking = c.Ranking
    }));

var list = customersList.Where(filter).OrderBy(sort).ToList();
```

---

## 🎛️ Critères d’acceptation

* Aucune méthode `Apply` dans les interfaces de specs.
* Filtres standards fournis, **null‑safe** et **EF‑translatables**.
* Options de **case‑sensitivity** pour specs `string`.
* `Include` fonctionne sur `IQueryable<T>` (typed chains ou string paths), **no‑op** en `IEnumerable<T>`.
* Tri fluent `Then(...)` fonctionnel sur `IQueryable<T>` et `IEnumerable<T>`.
* Registre d’optimisations opérationnel pour Postgres (`ILIKE` sur Starts/Contains/Ends insensibles à la casse).
* Jeux de tests couvrant in‑memory + EF Core 9 + plugin Npgsql.

---
