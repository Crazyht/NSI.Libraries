using System; // required for Func<>
using System.Globalization; // required for CultureInfo.InvariantCulture
#pragma warning disable S1128 // False positive: using is required for CultureInfo
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using NSI.Specifications.Filtering.Text;
using NSI.Specifications.Optimization;

namespace NSI.Specifications.Npgsql.Optimization.Npgsql;

/// <summary>
/// PostgreSQL-specific text optimizations mapping case-insensitive Contains/StartsWith/EndsWith to ILIKE.
/// </summary>
public static class NpgsqlTextOptimizations {
  // Cached MethodInfo for Npgsql ILIKE extension (compile-time safe, avoids repeated reflection lookups)
  private static readonly System.Reflection.MethodInfo ILikeMethod = ((Func<DbFunctions, string, string, bool>)NpgsqlDbFunctionsExtensions.ILike).Method;
  /// <summary>
  /// Registers all PostgreSQL (Npgsql) text optimizations.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Each optimization rewrites a case-insensitive text specification (explicitly expressed by both
  /// sides being lowered via <see cref="string.ToLower()"/>) into a call to <c>EF.Functions.ILike</c>, producing
  /// an equivalent, database-native ILIKE expression. Only specifications that explicitly lower both
  /// the member access and the search term are considered eligible to avoid rewriting case-sensitive
  /// queries inadvertently.
  /// </para>
  /// <para>
  /// The escaping logic only escapes the two PostgreSQL LIKE wildcards (<c>%</c> and <c>_</c>) plus the backslash
  /// itself. Backslash is escaped first (<c>\\</c>) to avoid double-processing already escaped sequences, then
  /// <c>%</c> becomes <c>\%</c> and <c>_</c> becomes <c>\_</c>. No other characters are modified.
  /// </para>
  /// </remarks>
  public static void RegisterAll() {
    SpecOptimizationRegistry.Register("Pg", new ContainsOptimization());
    SpecOptimizationRegistry.Register("Pg", new StartsWithOptimization());
    SpecOptimizationRegistry.Register("Pg", new EndsWithOptimization());
  }

  // Open generic optimization implementations (SpecificationType returns generic definition)
  private abstract class TextOptimizationBase: IFilterOptimization {
    public Type EntityType => typeof(object); // not used for lookup
    public abstract Type SpecificationType { get; }
    protected abstract string PatternFormat { get; }

    public LambdaExpression? TryRewriteLambda(object specification) {
      if (specification is null) {
        return null;
      }
      var specType = specification.GetType();
      if (!specType.IsGenericType || specType.GetGenericTypeDefinition() != SpecificationType) {
        return null;
      }
      // Invoke ToExpression() via reflection to obtain original lambda
      var toExpr = specType.GetMethod("ToExpression")!;
      if (toExpr.Invoke(specification, null) is not LambdaExpression original) {
        return null;
      }
      var rewritten = TryBuildInternal(original, PatternFormat);
      return rewritten ?? original; // fallback to original predicate (no optimization)
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
      var methodName = call.Method.Name;
      if (!string.Equals(methodName, nameof(string.Contains), StringComparison.Ordinal)
          && !string.Equals(methodName, nameof(string.StartsWith), StringComparison.Ordinal)
          && !string.Equals(methodName, nameof(string.EndsWith), StringComparison.Ordinal)) {
        return false;
      }
      return true;
    }
    private static bool IsIgnoreCasePattern(MethodCallExpression call) {
      static bool IsLower(MethodCallExpression m) => m.Arguments.Count == 0 && (m.Method.Name == nameof(string.ToLowerInvariant) || m.Method.Name == nameof(string.ToLower));
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
    private static Expression? UnwrapLower(Expression? expr)
            => expr is MethodCallExpression { Arguments.Count: 0 } m && (m.Method.Name == nameof(string.ToLowerInvariant) || m.Method.Name == nameof(string.ToLower))
                ? m.Object
                : expr;
    private static string? ExtractConstant(Expression expr) {
      var inner = expr;
      if (inner is MethodCallExpression { Arguments.Count: 0 } lower && (lower.Method.Name == nameof(string.ToLowerInvariant) || lower.Method.Name == nameof(string.ToLower))) {
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
      if (candidate is BinaryExpression { NodeType: ExpressionType.NotEqual, Left: MemberExpression m, Right: ConstantExpression { Value: null } }) {
        return SamePath(m, target);
      }
      return false;
    }
    private static bool SamePath(MemberExpression a, MemberExpression b) => a.ToString().Equals(b.ToString(), StringComparison.Ordinal);
    /// <summary>
    /// Escapes a raw user term for safe inclusion inside an ILIKE pattern.
    /// </summary>
    /// <param name="value">Raw (untrusted) user input.</param>
    /// <returns>Escaped string where literal <c>\\</c>, <c>%</c> and <c>_</c> are preserved as literals.</returns>
    /// <remarks>
    /// <para>
    /// Only three characters require escaping for PostgreSQL LIKE/ILIKE semantics:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>%</c> wildcard (any sequence)</description></item>
    ///   <item><description><c>_</c> wildcard (single character)</description></item>
    ///   <item><description><c>\\</c> escape character itself</description></item>
    /// </list>
    /// <para>
    /// The order of replacement matters: backslash must be doubled first to avoid re-escaping the
    /// backslashes introduced when later escaping <c>%</c> and <c>_</c>.
    /// </para>
    /// </remarks>
    private static string EscapeLike(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal) // escape backslash first
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    private static Expression? BuildILike(MemberExpression member, string pattern) {
      if (ILikeMethod == null) {
        return null; // should not happen unless signature changes
      }
      var functions = Expression.Property(null, typeof(EF), nameof(EF.Functions));
      return Expression.Call(ILikeMethod, functions, member, Expression.Constant(pattern, typeof(string)));
    }
  }

  private sealed class ContainsOptimization: TextOptimizationBase {
    public override Type SpecificationType => typeof(ContainsSpecification<>);
    protected override string PatternFormat => "%{0}%";
  }
  private sealed class StartsWithOptimization: TextOptimizationBase {
    public override Type SpecificationType => typeof(StartsWithSpecification<>);
    protected override string PatternFormat => "{0}%";
  }
  private sealed class EndsWithOptimization: TextOptimizationBase {
    public override Type SpecificationType => typeof(EndsWithSpecification<>);
    protected override string PatternFormat => "%{0}";
  }

  // Removed old TryBuild (logic moved into TextOptimizationBase)

  // Helper methods moved into TextOptimizationBase to satisfy analyzer.
}
