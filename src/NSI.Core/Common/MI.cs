using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace NSI.Core.Common;

/// <summary>
/// High-performance MethodInfo resolution helper using expressions.
/// </summary>
/// <remarks>
/// <para>
/// This class provides compile-time safe MethodInfo resolution using expression trees
/// instead of reflection-by-name, which is both faster and refactoring-safe.
/// It supports the recommended approach for MethodInfo resolution as outlined
/// in the NSI.Libraries coding standards.
/// </para>
/// <para>
/// Key benefits:
/// <list type="bullet">
///   <item><description>Compile-time safety - method signatures validated at build time</description></item>
///   <item><description>Refactoring safety - method renames are automatically tracked</description></item>
///   <item><description>Performance optimization - no string-based method lookups</description></item>
///   <item><description>IntelliSense support - full IDE support for method discovery</description></item>
/// </list>
/// </para>
/// <para>
/// This helper should be used with static readonly caching for optimal performance.
/// Always use <c>default!</c> for parameter values in expressions to avoid allocations
/// and satisfy nullable analysis requirements.
/// </para>
/// <para>
/// Thread Safety: All methods in this class are thread-safe as they only perform
/// expression tree analysis and return immutable <see cref="MethodInfo"/> instances.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage patterns
/// public static class KnownMethods {
///   // Static method resolution
///   public static readonly MethodInfo StringConcat = MI.Of(() => 
///     string.Concat(default!, default!));
///   
///   // Instance method resolution  
///   public static readonly MethodInfo StringIndexOf = MI.Of&lt;string, char, int&gt;(
///     s => s.IndexOf(default!));
///   
///   // Extension method resolution
///   public static readonly MethodInfo LinqWhere = MI.Of(() => 
///     Enumerable.Where&lt;object&gt;(default!, default!));
/// }
/// 
/// // Usage in code
/// var method = KnownMethods.StringIndexOf;
/// var result = method.Invoke(someString, new object[] { 'c' });
/// </code>
/// </example>
public static class MI {
  /// <summary>
  /// Resolves MethodInfo for a static method with no return value.
  /// </summary>
  /// <param name="expression">
  /// Expression tree representing the method call. Use <c>default!</c> for all parameters.
  /// </param>
  /// <returns>The <see cref="MethodInfo"/> for the specified method.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="expression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="expression"/> does not represent a method call.
  /// </exception>
  /// <example>
  /// <code>
  /// // Resolve Console.WriteLine(string) method
  /// var writeLineMethod = MI.Of(() => Console.WriteLine(default!));
  /// </code>
  /// </example>
  public static MethodInfo Of(Expression<Action> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return ExtractMethodInfo(expression.Body, nameof(expression));
  }

  /// <summary>
  /// Resolves MethodInfo for an instance method with no return value.
  /// </summary>
  /// <typeparam name="T">The type containing the method.</typeparam>
  /// <param name="expression">
  /// Expression tree representing the method call. Use <c>default!</c> for all parameters.
  /// </param>
  /// <returns>The <see cref="MethodInfo"/> for the specified method.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="expression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="expression"/> does not represent a method call.
  /// </exception>
  /// <example>
  /// <code>
  /// // Resolve List&lt;string&gt;.Add(string) method
  /// var addMethod = MI.Of&lt;List&lt;string&gt;&gt;(list => list.Add(default!));
  /// </code>
  /// </example>
  public static MethodInfo Of<T>(Expression<Action<T>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return ExtractMethodInfo(expression.Body, nameof(expression));
  }

  /// <summary>
  /// Resolves MethodInfo for a static method with a return value.
  /// </summary>
  /// <typeparam name="TResult">The return type of the method.</typeparam>
  /// <param name="expression">
  /// Expression tree representing the method call. Use <c>default!</c> for all parameters.
  /// </param>
  /// <returns>The <see cref="MethodInfo"/> for the specified method.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="expression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="expression"/> does not represent a method call.
  /// </exception>
  /// <example>
  /// <code>
  /// // Resolve string.Empty getter (property backing method)
  /// var emptyMethod = MI.Of&lt;string&gt;(() => string.Empty);
  /// 
  /// // Resolve DateTime.Now getter
  /// var nowMethod = MI.Of&lt;DateTime&gt;(() => DateTime.Now);
  /// </code>
  /// </example>
  public static MethodInfo Of<TResult>(Expression<Func<TResult>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return ExtractMethodInfo(expression.Body, nameof(expression));
  }

  /// <summary>
  /// Resolves MethodInfo for an instance method with a return value.
  /// </summary>
  /// <typeparam name="T">The type containing the method.</typeparam>
  /// <typeparam name="TResult">The return type of the method.</typeparam>
  /// <param name="expression">
  /// Expression tree representing the method call. Use <c>default!</c> for all parameters.
  /// </param>
  /// <returns>The <see cref="MethodInfo"/> for the specified method.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="expression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="expression"/> does not represent a method call.
  /// </exception>
  /// <example>
  /// <code>
  /// // Resolve string.IndexOf(char) method
  /// var indexOfMethod = MI.Of&lt;string, int&gt;(s => s.IndexOf(default!));
  /// 
  /// // Resolve List&lt;T&gt;.Count property getter
  /// var countMethod = MI.Of&lt;List&lt;int&gt;, int&gt;(list => list.Count);
  /// </code>
  /// </example>
  public static MethodInfo Of<T, TResult>(Expression<Func<T, TResult>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return ExtractMethodInfo(expression.Body, nameof(expression));
  }

  /// <summary>
  /// Resolves MethodInfo for a static method with two parameters and a return value.
  /// </summary>
  /// <typeparam name="T1">The type of the first parameter.</typeparam>
  /// <typeparam name="T2">The type of the second parameter.</typeparam>
  /// <typeparam name="TResult">The return type of the method.</typeparam>
  /// <param name="expression">
  /// Expression tree representing the method call. Use <c>default!</c> for all parameters.
  /// </param>
  /// <returns>The <see cref="MethodInfo"/> for the specified method.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="expression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="expression"/> does not represent a method call.
  /// </exception>
  /// <example>
  /// <code>
  /// // Resolve string.Concat(string, string) method
  /// var concatMethod = MI.Of&lt;string, string, string&gt;(() => 
  ///   string.Concat(default!, default!));
  /// 
  /// // Resolve Math.Max(int, int) method  
  /// var maxMethod = MI.Of&lt;int, int, int&gt;(() => Math.Max(default!, default!));
  /// </code>
  /// </example>
  public static MethodInfo Of<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return ExtractMethodInfo(expression.Body, nameof(expression));
  }

  /// <summary>
  /// Resolves MethodInfo for a static method with three parameters and a return value.
  /// </summary>
  /// <typeparam name="T1">The type of the first parameter.</typeparam>
  /// <typeparam name="T2">The type of the second parameter.</typeparam>
  /// <typeparam name="T3">The type of the third parameter.</typeparam>
  /// <typeparam name="TResult">The return type of the method.</typeparam>
  /// <param name="expression">
  /// Expression tree representing the method call. Use <c>default!</c> for all parameters.
  /// </param>
  /// <returns>The <see cref="MethodInfo"/> for the specified method.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="expression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="expression"/> does not represent a method call.
  /// </exception>
  /// <example>
  /// <code>
  /// // Resolve string.Substring(int, int) method - but as a 3-parameter example
  /// // This is typically used for extension methods with 3+ parameters
  /// var extensionMethod = MI.Of&lt;IQueryable&lt;object&gt;, Expression&lt;Func&lt;object, bool&gt;&gt;, IQueryable&lt;object&gt;&gt;(() => 
  ///   Queryable.Where(default!, default!));
  /// </code>
  /// </example>
  [SuppressMessage(
    "Minor Code Smell", 
    "S2436:Reduce the number of generic parameters in the 'MI.Of' method to no more than the 3 authorized.",
    Justification = "MethodInfo resolution helper requires multiple overloads to support various method signatures. " +
                   "Limiting to 3 generic parameters would prevent resolution of methods with more parameters, " +
                   "which is essential for comprehensive reflection support in this utility class.")]
  public static MethodInfo Of<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> expression) {
    ArgumentNullException.ThrowIfNull(expression);
    return ExtractMethodInfo(expression.Body, nameof(expression));
  }

  /// <summary>
  /// Extracts the MethodInfo from an expression tree body.
  /// </summary>
  /// <param name="body">The expression body to analyze.</param>
  /// <param name="parameterName">The parameter name for exception reporting.</param>
  /// <returns>The extracted <see cref="MethodInfo"/>.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when the expression body is not a method call or property access.
  /// </exception>
  private static MethodInfo ExtractMethodInfo(Expression body, string parameterName) => body switch {
    MethodCallExpression methodCall => methodCall.Method,
    MemberExpression { Member: PropertyInfo property } =>
      property.GetMethod ?? throw new ArgumentException(
        $"Property '{property.Name}' does not have a getter method.", parameterName),
    _ => throw new ArgumentException(
      $"Expression must be a method call or property access, but was: {body.NodeType}", parameterName)
  };
}
