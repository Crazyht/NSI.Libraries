using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NSI.Core.Common;
using NSI.Specifications.Filtering.Text;
using NSI.Specifications.Optimization;

namespace NSI.Specifications.Npgsql.Optimization.Npgsql;

/// <summary>
/// PostgreSQL specific text optimizations rewriting case-insensitive pattern specifications
/// (<c>Contains</c>, <c>StartsWith</c>, <c>EndsWith</c>) into native <c>ILIKE</c> expressions for
/// improved translation and potential index utilization.
/// </summary>
/// <remarks>
/// <para>Responsibilities:
/// <list type="bullet">
///   <item><description>Identify eligible text filtering specifications lowered on both sides
///     (member + term) indicating intent for case-insensitive comparison.</description></item>
///   <item><description>Rewrite eligible predicates into <c>EF.Functions.ILike(...)</c> calls with
///     correct wildcard placement based on pattern type.</description></item>
///   <item><description>Preserve existing predicate (no-op) when any structural guard fails to avoid
///     accidental semantic change.</description></item>
/// </list>
/// </para>
/// <para>Eligibility Rules (fail-fast any mismatch):
/// <list type="bullet">
///   <item><description>Original body must be a logical AND chain containing null-check guards.</description></item>
///   <item><description>Terminal call must be one of: <c>string.Contains</c>, <c>StartsWith</c>,
///     <c>EndsWith</c>.</description></item>
///   <item><description>Object and the single argument both invoked with <c>.ToLower()</c> or
///     <c>.ToLowerInvariant()</c>.</description></item>
///   <item><description>Search term must be a constant (avoids dynamic side-effects).</description></item>
///   <item><description>At least one explicit null guard (chain) on the target member path present.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Single static cached <see cref="MethodInfo"/> acquisition via MI helper
///     (zero repeated reflection).</description></item>
///   <item><description>Rewrite cost is O(n) where n = depth of AND chain; typical short.</description></item>
///   <item><description>No dynamic compilation; output remains an expression tree for provider use.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Class is stateless; all operations are pure and safe for concurrent use.</para>
/// <para>Escaping Strategy: Only <c>\\</c>, <c>%</c>, <c>_</c> are escaped (PostgreSQL LIKE meta
/// characters). Replacement order ensures newly introduced escapes are not double processed.</para>
/// </remarks>
/// <example>
/// <code>
/// // Registration (typically at startup / module init)
/// NpgsqlTextOptimizations.RegisterAll();
///
/// // A user specification (conceptual) that lowers both sides
/// // u => u.Name != null &amp;&amp; u.Name.ToLower().Contains("john".ToLower())
///
/// // After optimization becomes (simplified):
/// // u => EF.Functions.ILike(u.Name, "%john%") with proper escaping
/// </code>
/// </example>
public static class NpgsqlTextOptimizations {
  /// <summary>
  /// Cached MethodInfo for Npgsql ILIKE extension resolved via preferred MI expression helper
  /// (compile-time safety, zero delegate allocation, single static lookup).
  /// </summary>
  private static readonly MethodInfo ILikeMethod =
    MI.Of(() => NpgsqlDbFunctionsExtensions.ILike(default!, default!, default!));

  /// <summary>
  /// Registers all PostgreSQL (Npgsql) text optimizations.
  /// </summary>
  /// <remarks>
  /// <para>Registration associates multiple pattern optimisation handlers under provider key
  /// "Pg" inside <see cref="SpecOptimizationRegistry"/>. Idempotency depends on registry
  /// implementation (call only once during startup).</para>
  /// </remarks>
  public static void RegisterAll() {
    SpecOptimizationRegistry.Register("Pg", new ContainsOptimization());
    SpecOptimizationRegistry.Register("Pg", new StartsWithOptimization());
    SpecOptimizationRegistry.Register("Pg", new EndsWithOptimization());
  }

  /// <summary>
  /// Base class implementing common rewrite pipeline for text pattern specifications.
  /// </summary>
  /// <remarks>
  /// <para>Implements validation, structural extraction and conditional rewrite. Derived
  /// classes only supply the open generic specification type and pattern format string.</para>
  /// </remarks>
  private abstract class TextOptimizationBase: IFilterOptimization {
    /// <inheritdoc />
    public Type EntityType => typeof(object); // not used for lookup (generic text scenarios)

    /// <inheritdoc />
    public abstract Type SpecificationType { get; }

    /// <summary>Pattern format where {0} is the escaped user term (wildcards appended here).</summary>
    protected abstract string PatternFormat { get; }

    /// <inheritdoc />
    public LambdaExpression? TryRewriteLambda(object specification) {
      if (specification is null) {
        return null;
      }
      var specType = specification.GetType();
      if (!specType.IsGenericType || specType.GetGenericTypeDefinition() != SpecificationType) {
        return null;
      }
      // Invoke ToExpression() via reflection (original expression produced by specification)
      var toExpr = specType.GetMethod("ToExpression")!;
      if (toExpr.Invoke(specification, null) is not LambdaExpression original) {
        return null;
      }
      var rewritten = TryBuildInternal(original, PatternFormat);
      return rewritten ?? original; // fallback (no optimization path)
    }

    private static LambdaExpression? TryBuildInternal(LambdaExpression original, string format) {
      if (!IsExpectedRoot(original.Body, out var root)) {
        return null;
      }
      if (!TryGetEligibleCall(root, out var call)) {
        return null;
      }
      if (!IsIgnoreCasePattern(call)) {
        return null;
      }
      if (!TryGetTargetMember(call, out var memberExpr)) {
        return null;
      }
      if (!TryGetSearchTerm(call, out var term)) {
        return null;
      }
      if (!HasNullGuard(root, memberExpr)) {
        return null;
      }
      var parameter = original.Parameters[0];
      var escaped = EscapeLike(term);
      var pattern = string.Format(CultureInfo.InvariantCulture, format, escaped);
      var callLike = BuildILike(memberExpr, pattern);
      if (callLike == null) {
        return null;
      }
      var delegateType = typeof(Func<,>).MakeGenericType(parameter.Type, typeof(bool));
      return Expression.Lambda(delegateType, callLike, parameter);
    }

    private static bool IsExpectedRoot(Expression body, out BinaryExpression root) {
      if (body is BinaryExpression be && be.NodeType == ExpressionType.AndAlso) {
        root = be;
        return true;
      }
      root = null!;
      return false;
    }

    private static bool TryGetEligibleCall(BinaryExpression root, out MethodCallExpression call) {
      call = GetTerminalCall(root)!;
      if (call == null || call.Arguments.Count != 1) {
        return false;
      }
      var name = call.Method.Name;
      if (!string.Equals(name, nameof(string.Contains), StringComparison.Ordinal)
          && !string.Equals(name, nameof(string.StartsWith), StringComparison.Ordinal)
          && !string.Equals(name, nameof(string.EndsWith), StringComparison.Ordinal)) {
        return false;
      }
      return true;
    }

    private static bool IsIgnoreCasePattern(MethodCallExpression call) {
      static bool IsLower(MethodCallExpression m) => m.Arguments.Count == 0 &&
        (m.Method.Name == nameof(string.ToLowerInvariant) || m.Method.Name == nameof(string.ToLower));
      var objectLowered = call.Object is MethodCallExpression om && IsLower(om);
      var argLowered = call.Arguments[0] is MethodCallExpression am && IsLower(am);
      return objectLowered && argLowered;
    }

    private static bool TryGetTargetMember(MethodCallExpression call, out MemberExpression member) {
      var target = UnwrapLower(call.Object);
      if (target is MemberExpression m) {
        member = m;
        return true;
      }
      member = null!;
      return false;
    }

    private static bool TryGetSearchTerm(MethodCallExpression call, out string term) {
      term = ExtractConstant(call.Arguments[0]) ?? string.Empty;
      return !string.IsNullOrEmpty(term);
    }

    private static MethodCallExpression? GetTerminalCall(BinaryExpression root) {
      if (root.Right is MethodCallExpression direct) {
        return direct;
      }
      if (root.Right is BinaryExpression be && be.NodeType == ExpressionType.AndAlso) {
        return GetTerminalCall(be);
      }
      return null;
    }

    private static Expression? UnwrapLower(Expression? expr) =>
      expr is MethodCallExpression { Arguments.Count: 0 } m &&
        (m.Method.Name == nameof(string.ToLowerInvariant) || m.Method.Name == nameof(string.ToLower))
        ? m.Object
        : expr;

    private static string? ExtractConstant(Expression expr) {
      var inner = expr;
      if (inner is MethodCallExpression { Arguments.Count: 0 } lower &&
          (lower.Method.Name == nameof(string.ToLowerInvariant) ||
           lower.Method.Name == nameof(string.ToLower))) {
        inner = lower.Object!;
      }
      if (inner is ConstantExpression { Value: string s }) {
        return s;
      }
      return null;
    }

    private static bool HasNullGuard(Expression candidate, MemberExpression target) {
      if (candidate is BinaryExpression { NodeType: ExpressionType.AndAlso } be) {
        return HasNullGuard(be.Left, target) || HasNullGuard(be.Right, target);
      }
      if (candidate is BinaryExpression {
        NodeType: ExpressionType.NotEqual,
        Left: MemberExpression m,
        Right: ConstantExpression { Value: null }
      } && SamePath(m, target)) {
        return true;
      }
      return false;
    }

    private static bool SamePath(MemberExpression a, MemberExpression b) =>
      a.ToString().Equals(b.ToString(), StringComparison.Ordinal);

    /// <summary>
    /// Escapes a raw user term for safe inclusion inside an ILIKE pattern.
    /// </summary>
    /// <param name="value">Raw (untrusted) user input.</param>
    /// <returns>Escaped string where literal <c>\\</c>, <c>%</c> and <c>_</c> are preserved.</returns>
    /// <remarks>
    /// <para>Only three characters require escaping for PostgreSQL LIKE/ILIKE semantics.</para>
    /// <para>Backslash doubled first, then percent and underscore prefixed.</para>
    /// </remarks>
    private static string EscapeLike(string value) =>
      value
        .Replace("\\", "\\\\", StringComparison.Ordinal) // escape backslash first
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static Expression? BuildILike(MemberExpression member, string pattern) {
      if (ILikeMethod == null) {
        return null; // defensive (should never occur)
      }
      var functions = Expression.Property(null, typeof(EF), nameof(EF.Functions));
      return Expression.Call(ILikeMethod, functions, member, Expression.Constant(pattern, typeof(string)));
    }
  }

  /// <summary>Optimization for <see cref="ContainsSpecification{T}"/> (%term%).</summary>
  private sealed class ContainsOptimization: TextOptimizationBase {
    /// <inheritdoc />
    public override Type SpecificationType => typeof(ContainsSpecification<>);
    /// <inheritdoc />
    protected override string PatternFormat => "%{0}%";
  }

  /// <summary>Optimization for <see cref="StartsWithSpecification{T}"/> (term%).</summary>
  private sealed class StartsWithOptimization: TextOptimizationBase {
    /// <inheritdoc />
    public override Type SpecificationType => typeof(StartsWithSpecification<>);
    /// <inheritdoc />
    protected override string PatternFormat => "{0}%";
  }

  /// <summary>Optimization for <see cref="EndsWithSpecification{T}"/> (%term).</summary>
  private sealed class EndsWithOptimization: TextOptimizationBase {
    /// <inheritdoc />
    public override Type SpecificationType => typeof(EndsWithSpecification<>);
    /// <inheritdoc />
    protected override string PatternFormat => "%{0}";
  }
}
