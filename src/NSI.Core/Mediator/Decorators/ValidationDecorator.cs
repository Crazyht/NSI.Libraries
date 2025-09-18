using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Mediator.Decorators;

/// <summary>
/// Decorator that provides automatic validation for requests using the validation framework.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// This decorator automatically applies validation to requests by resolving the appropriate
/// <see cref="IValidator{T}"/> from the dependency injection container. If no validator is
/// registered for the request type, validation is skipped and the request continues normally
/// through the pipeline without any performance overhead.
/// </para>
/// <para>
/// Validation execution flow:
/// <list type="number">
///   <item><description>Attempts to resolve <see cref="IValidator{TRequest}"/> from DI container</description></item>
///   <item><description>If no validator found, continues to next handler without validation</description></item>
///   <item><description>If validator found, creates <see cref="ValidationContext"/> with service provider</description></item>
///   <item><description>Executes asynchronous validation with cancellation support</description></item>
///   <item><description>If validation fails, short-circuits pipeline and returns validation errors</description></item>
///   <item><description>If validation passes, continues to next handler in pipeline</description></item>
/// </list>
/// </para>
/// <para>
/// Key architectural benefits:
/// <list type="bullet">
///   <item><description>Separation of concerns: Validation logic isolated in dedicated validators</description></item>
///   <item><description>Automatic application: No manual validation calls needed in handlers</description></item>
///   <item><description>Flexible registration: Validators can be conditionally registered per request type</description></item>
///   <item><description>Performance optimized: Zero overhead when no validator is registered</description></item>
///   <item><description>Result pattern integration: Validation errors seamlessly integrated with Result&lt;T&gt;</description></item>
/// </list>
/// </para>
/// <para>
/// Performance considerations: The decorator uses service resolution to check for validators,
/// which has minimal overhead. When no validator is registered, the lookup is fast and
/// the request continues immediately. Validation contexts are created with the current
/// service provider for dependency injection support within validation rules.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the validation decorator in the pipeline
/// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(ValidationDecorator&lt;,&gt;));
/// 
/// // Register validators for specific request types
/// services.AddScoped&lt;IValidator&lt;CreateUserCommand&gt;, CreateUserCommandValidator&gt;();
/// services.AddScoped&lt;IValidator&lt;UpdateUserCommand&gt;, UpdateUserCommandValidator&gt;();
/// 
/// // Validator implementation example
/// public class CreateUserCommandValidator: IValidator&lt;CreateUserCommand&gt; {
///   public async Task&lt;ValidationResult&gt; ValidateAsync(
///     CreateUserCommand request,
///     ValidationContext context,
///     CancellationToken cancellationToken = default) {
///     
///     var errors = new List&lt;IValidationError&gt;();
///     
///     if (string.IsNullOrWhiteSpace(request.Email)) {
///       errors.Add(new ValidationError("Email", "Email is required", "REQUIRED"));
///     }
///     
///     if (string.IsNullOrWhiteSpace(request.Name)) {
///       errors.Add(new ValidationError("Name", "Name is required", "REQUIRED"));
///     }
///     
///     return new ValidationResult(errors);
///   }
/// }
/// 
/// // Usage: Validation happens automatically
/// var result = await mediator.ProcessAsync(new CreateUserCommand { 
///   Email = "user@example.com",
///   Name = "John Doe"
/// });
/// 
/// // Handle validation results
/// return result.Match(
///   onSuccess: user =&gt; Ok(user),
///   onFailure: error =&gt; error.Type switch {
///     ErrorType.Validation =&gt; BadRequest(error.ValidationErrors),
///     _ =&gt; StatusCode(500, "An error occurred")
///   }
/// );
/// </code>
/// </example>
/// <seealso cref="IRequestDecorator{TRequest, TResponse}"/>
/// <seealso cref="IValidator{T}"/>
/// <seealso cref="ValidationContext"/>
/// <seealso cref="ValidationResult"/>
/// <seealso cref="Result{T}"/>
/// <seealso cref="ResultError"/>
public class ValidationDecorator<TRequest, TResponse>(
  IServiceProvider serviceProvider,
  ILogger<ValidationDecorator<TRequest, TResponse>> logger): IRequestDecorator<TRequest, TResponse>
  where TRequest: IRequest<TResponse> {

  private readonly IServiceProvider _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
  private readonly ILogger<ValidationDecorator<TRequest, TResponse>> _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

  /// <summary>
  /// Handles the request with validation performed before processing.
  /// </summary>
  /// <param name="request">The request to validate and handle.</param>
  /// <param name="continuation">Function to invoke the next handler in the pipeline.</param>
  /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
  /// <returns>
  /// A task that represents the asynchronous operation containing the result.
  /// Returns validation errors if validation fails, otherwise continues to next handler.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="request"/> or <paramref name="continuation"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// Validation execution strategy:
  /// <list type="bullet">
  ///   <item><description>Service resolution: Uses GetService&lt;T&gt;() for optional validator lookup</description></item>
  ///   <item><description>Context creation: Provides service provider for validator dependency injection</description></item>
  ///   <item><description>Error aggregation: Combines multiple validation errors into single Result failure</description></item>
  ///   <item><description>Pipeline control: Short-circuits on failure, continues on success</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Performance optimizations applied:
  /// <list type="bullet">
  ///   <item><description>Fast exit when no validator is registered</description></item>
  ///   <item><description>High-performance logging using LoggerMessage patterns</description></item>
  ///   <item><description>Minimal string allocations in error message formatting</description></item>
  ///   <item><description>Efficient validation error collection and processing</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public async Task<Result<TResponse>> HandleAsync(
    TRequest request,
    RequestHandlerFunction<TResponse> continuation,
    CancellationToken cancellationToken = default) {

    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(continuation);

    var requestName = typeof(TRequest).Name;

    // Try to resolve validator for this request type
    var validator = _ServiceProvider.GetService<IValidator<TRequest>>();

    if (validator is null) {
      // No validator registered, skip validation and continue
      _Logger.LogDecoratorValidationSkipped(requestName);
      return await continuation();
    }

    _Logger.LogDecoratorValidationStarting(requestName);

    // Create validation context with the current service provider
    var validationContext = new ValidationContext(_ServiceProvider);

    // Perform validation
    var validationResult = await validator.ValidateAsync(request, validationContext, cancellationToken);

    if (!validationResult.IsValid) {
      var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
      _Logger.LogDecoratorValidationFailed(requestName, errorMessages);

      return Result.Failure<TResponse>(ResultError.Validation(
        "VALIDATION_FAILED",
        "Request validation failed",
        validationResult.Errors));
    }

    _Logger.LogDecoratorValidationPassed(requestName);

    // Validation passed, continue to next handler
    return await continuation();
  }
}
