using System.Diagnostics.CodeAnalysis;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Validation {
  /// <summary>
  /// Base implementation of <see cref="IValidator{T}"/>.
  /// </summary>
  /// <typeparam name="T">The type of object to validate.</typeparam>
  /// <remarks>
  /// <para>
  /// This class provides a flexible validator implementation that combines both synchronous
  /// and asynchronous validation rules. It allows building complex validation pipelines
  /// by adding multiple validation rules that are executed in sequence.
  /// </para>
  /// <para>
  /// Key features:
  /// <list type="bullet">
  ///   <item><description>Fluent API for adding validation rules</description></item>
  ///   <item><description>Support for both synchronous and asynchronous validation</description></item>
  ///   <item><description>Aggregation of validation errors from multiple rules</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example usage:
  /// <code>
  /// var validator = new Validator&lt;User&gt;()
  ///   .AddRule(new RequiredRule&lt;User&gt;(u => u.Email))
  ///   .AddRule(new EmailRule&lt;User&gt;(u => u.Email))
  ///   .AddAsyncRule(new UniqueRule&lt;User, string&gt;(
  ///     u => u.Email,
  ///     async (services, email, ct) => {
  ///       var repo = services.GetRequiredService&lt;IUserRepository&gt;();
  ///       return await repo.EmailExistsAsync(email, ct);
  ///     }
  ///   ));
  /// 
  /// var result = await validator.ValidateAsync(user);
  /// if (!result.IsValid) {
  ///   throw new ValidationException(result);
  /// }
  /// </code>
  /// </para>
  /// </remarks>
  public class Validator<T>: IValidator<T> {
    private readonly List<IValidationRule<T>> _SyncRules = [];
    private readonly List<IAsyncValidationRule<T>> _AsyncRules = [];

    /// <summary>
    /// Adds a synchronous validation rule.
    /// </summary>
    /// <param name="rule">The validation rule to add.</param>
    /// <returns>The current validator instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rule"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method allows building validation pipelines using a fluent API pattern.
    /// Rules are executed in the order they are added.
    /// </para>
    /// <para>
    /// Synchronous rules are always executed before asynchronous rules, regardless
    /// of the order they were added.
    /// </para>
    /// </remarks>
    public Validator<T> AddRule(IValidationRule<T> rule) {
      ArgumentNullException.ThrowIfNull(rule);
      _SyncRules.Add(rule);
      return this;
    }

    /// <summary>
    /// Adds an asynchronous validation rule.
    /// </summary>
    /// <param name="rule">The async validation rule to add.</param>
    /// <returns>The current validator instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rule"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method allows building validation pipelines using a fluent API pattern.
    /// Asynchronous rules are executed after all synchronous rules have completed.
    /// </para>
    /// <para>
    /// When using <see cref="Validate"/>, asynchronous rules are executed sequentially.
    /// When using <see cref="ValidateAsync"/>, asynchronous rules are executed in parallel.
    /// </para>
    /// </remarks>
    public Validator<T> AddAsyncRule(IAsyncValidationRule<T> rule) {
      ArgumentNullException.ThrowIfNull(rule);
      _AsyncRules.Add(rule);
      return this;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method executes all validation rules synchronously, including asynchronous rules.
    /// Asynchronous rules are executed using blocking calls, which may impact performance.
    /// </para>
    /// <para>
    /// For applications with asynchronous validation rules, it's recommended to use
    /// <see cref="ValidateAsync"/> instead to avoid blocking threads.
    /// </para>
    /// </remarks>
    [SuppressMessage(
      "Blocker Code Smell",
      "S4462:Calls to \"async\" methods should not be blocking",
      Justification = "Synchronous API requires blocking when async validation is used.")]
    public IValidationResult Validate(T instance, IValidationContext? context = null) {
      ArgumentNullException.ThrowIfNull(instance);

      context ??= ValidationContext.Empty();
      var errors = new List<IValidationError>();

      // Execute sync rules
      foreach (var rule in _SyncRules) {
        errors.AddRange(rule.Validate(instance, context));
      }

      // Execute async rules synchronously
      foreach (var rule in _AsyncRules) {
        var task = rule.ValidateAsync(instance, context, CancellationToken.None);
        errors.AddRange(task.GetAwaiter().GetResult());
      }

      return new ValidationResult(errors);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method executes validation rules asynchronously, with the following behavior:
    /// <list type="bullet">
    ///   <item><description>Synchronous rules are executed sequentially first</description></item>
    ///   <item><description>Asynchronous rules are executed in parallel after all synchronous rules</description></item>
    ///   <item><description>All validation errors are aggregated into a single result</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Executing asynchronous rules in parallel can improve performance, especially
    /// when rules involve I/O operations like database or API calls.
    /// </para>
    /// </remarks>
    public async Task<IValidationResult> ValidateAsync(
      T instance,
      IValidationContext? context = null,
      CancellationToken cancellationToken = default) {
      ArgumentNullException.ThrowIfNull(instance);
      context ??= ValidationContext.Empty();

      var errors = new List<IValidationError>();

      // Execute sync rules first
      foreach (var rule in _SyncRules) {
        errors.AddRange(rule.Validate(instance, context));
      }

      // Then execute async rules
      var asyncTasks = _AsyncRules
        .Select(rule => rule.ValidateAsync(instance, context, cancellationToken));

      var asyncResults = await Task.WhenAll(asyncTasks);

      foreach (var result in asyncResults) {
        errors.AddRange(result);
      }

      return new ValidationResult(errors);
    }
  }
}
