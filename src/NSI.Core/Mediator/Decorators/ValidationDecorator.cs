using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSI.Core.Mediator.Abstractions;
using NSI.Core.Results;
using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;

namespace NSI.Core.Mediator.Decorators {
  /// <summary>
  /// Decorator that provides automatic validation for requests using the validation framework.
  /// </summary>
  /// <typeparam name="TRequest">The type of request being handled.</typeparam>
  /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
  /// <remarks>
  /// <para>
  /// This decorator automatically applies validation to requests by resolving the appropriate
  /// <see cref="IValidator{T}"/> from the dependency injection container. If no validator is
  /// registered for the request type, validation is skipped and the request continues normally.
  /// </para>
  /// <para>
  /// The validation process:
  /// <list type="bullet">
  ///   <item><description>Attempts to resolve <see cref="IValidator{TRequest}"/> from DI</description></item>
  ///   <item><description>If found, validates the request asynchronously</description></item>
  ///   <item><description>If validation fails, short-circuits and returns validation errors</description></item>
  ///   <item><description>If validation passes or no validator exists, continues to next handler</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// This approach promotes clean separation of concerns by keeping validation logic
  /// in dedicated validator classes while automatically applying it through the mediator pipeline.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Register the decorator
  /// services.AddScoped(typeof(IRequestDecorator&lt;,&gt;), typeof(ValidationDecorator&lt;,&gt;));
  /// 
  /// // Register a validator for a specific request type
  /// services.AddScoped&lt;IValidator&lt;CreateUserCommand&gt;, CreateUserCommandValidator&gt;();
  /// 
  /// // The decorator will automatically apply validation when CreateUserCommand is processed
  /// var result = await mediator.ProcessAsync(new CreateUserCommand { ... });
  /// </code>
  /// </example>
  /// <remarks>
  /// Initializes a new instance of the <see cref="ValidationDecorator{TRequest, TResponse}"/> class.
  /// </remarks>
  /// <param name="serviceProvider">The service provider for resolving validators.</param>
  /// <param name="logger">The logger for diagnostic information.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="serviceProvider"/> or <paramref name="logger"/> is null.
  /// </exception>
  public class ValidationDecorator<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ILogger<ValidationDecorator<TRequest, TResponse>> logger): IRequestDecorator<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {

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
        _Logger.LogDecoratorValidationStarting(requestName);
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
}
