using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Marker interface describing a specification whose intent is to filter an <typeparamref name="T"/>
/// sequence (query predicate) rather than perform validation or transformation.
/// </summary>
/// <typeparam name="T">Entity (aggregate / value object) type evaluated.</typeparam>
/// <remarks>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Represents a pure, side‑effect free boolean predicate over <typeparamref name="T"/>.</description></item>
///   <item><description>Intended for query composition (LINQ providers, IQueryable pipelines).</description></item>
///   <item><description>May be combined via logical operators (And / Or / Not) supplied by specification extensions.</description></item>
///   <item><description>Focuses on filtering concerns (equality, ranges, text search, etc.).</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Keep expressions provider-translatable (avoid invoking local non-deterministic methods).</description></item>
///   <item><description>Favor immutable, reusable specification instances (cache when applied repeatedly).</description></item>
///   <item><description>Compose multiple small focused filter specs instead of one large complex predicate.</description></item>
///   <item><description>Leverage domain primitives / value objects inside the expression to centralize logic.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Expression tree built once per spec instance (typically O(1)).</description></item>
///   <item><description>Delegate compilation (for in-memory evaluation) should be cached by consumers when hot.</description></item>
///   <item><description>Combining multiple specs adds logical conjunction/disjunction depth; keep trees shallow where possible.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Implementations should be immutable and therefore safe for concurrent reuse.</para>
/// </remarks>
/// <example>
/// <code>
/// // Example: simple reusable filter specifications
/// public sealed class ActiveSpecification: IFilterSpecification&lt;User&gt; {
///   private static readonly Expression&lt;Func&lt;User, bool&gt;&gt; Expr = u =&gt; u.Active;
///   public Expression&lt;Func&lt;User, bool&gt;&gt; ToExpression() =&gt; Expr;
///   public bool IsSatisfiedBy(User candidate) =&gt; Expr.Compile()(candidate);
/// }
///
/// public sealed class MinimumAgeSpecification: IFilterSpecification&lt;User&gt; {
///   private readonly int _Min;
///   public MinimumAgeSpecification(int min) =&gt; _Min = min;
///   public Expression&lt;Func&lt;User, bool&gt;&gt; ToExpression() =&gt; u =&gt; u.Age &gt;= _Min;
///   public bool IsSatisfiedBy(User candidate) =&gt; candidate.Age &gt;= _Min;
/// }
///
/// // Composition (via extension helpers, e.g. And)
/// var spec = new ActiveSpecification().And(new MinimumAgeSpecification(18));
/// var adults = users.AsQueryable().Where(spec.ToExpression());
/// </code>
/// </example>
/// <seealso cref="ISpecification{T}"/>
public interface IFilterSpecification<T>: ISpecification<T> { }
