using NSI.Core.Mediator.Abstractions;

namespace NSI.Core.Mediator;

/// <summary>
/// Represents the result of mediator handler validation operations performed during application startup.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates the comprehensive validation results for mediator infrastructure,
/// providing detailed information about handler registration completeness, dependency resolution
/// status, and any configuration issues discovered during the validation process.
/// </para>
/// <para>
/// Validation scope and functionality:
/// <list type="bullet">
///   <item><description>Handler registration: Verifies all request types have corresponding registered handlers</description></item>
///   <item><description>Dependency validation: Ensures all handler dependencies can be resolved from DI container</description></item>
///   <item><description>Interface compliance: Validates handler implementations correctly implement required interfaces</description></item>
///   <item><description>Pipeline integrity: Checks decorator registration and compatibility with request types</description></item>
///   <item><description>Configuration consistency: Validates mediator options and configuration settings</description></item>
/// </list>
/// </para>
/// <para>
/// Usage in startup validation:
/// <list type="bullet">
///   <item><description>Fail-fast behavior: Critical validation failures prevent application startup</description></item>
///   <item><description>Diagnostic information: Detailed error messages for troubleshooting configuration issues</description></item>
///   <item><description>Performance optimization: Validation occurs once at startup to avoid runtime overhead</description></item>
///   <item><description>Development feedback: Immediate feedback during development and deployment processes</description></item>
/// </list>
/// </para>
/// <para>
/// The validation result integrates with the mediator options system and can be configured
/// to perform different levels of validation based on environment and deployment requirements.
/// Results are logged for diagnostic purposes and can be used for health check reporting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Startup validation configuration
/// services.AddMediator(options =&gt; {
///   options.ValidateHandlersAtStartup = true;
/// });
/// 
/// // Manual validation execution
/// var validator = serviceProvider.GetRequiredService&lt;IMediatorValidator&gt;();
/// var result = await validator.ValidateAsync();
/// 
/// if (!result.IsValid) {
///   var errors = string.Join(Environment.NewLine, result.Errors);
///   throw new InvalidOperationException($"Mediator validation failed:{Environment.NewLine}{errors}");
/// }
/// 
/// // Validation in health checks
/// public class MediatorConfigurationHealthCheck : IHealthCheck {
///   public Task&lt;HealthCheckResult&gt; CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) {
///     var validator = serviceProvider.GetRequiredService&lt;IMediatorValidator&gt;();
///     var result = validator.ValidateConfiguration();
///     
///     return Task.FromResult(result.IsValid 
///       ? HealthCheckResult.Healthy($"Validated {result.ValidatedHandlerCount} handlers successfully")
///       : HealthCheckResult.Unhealthy($"Validation failed: {string.Join(", ", result.Errors)}"));
///   }
/// }
/// 
/// // Logging validation results
/// if (result.IsValid) {
///   logger.LogInformation("Mediator validation completed successfully. Validated {HandlerCount} handlers.", 
///     result.ValidatedHandlerCount);
/// } else {
///   logger.LogError("Mediator validation failed with {ErrorCount} errors: {Errors}", 
///     result.Errors.Count, string.Join("; ", result.Errors));
/// }
/// </code>
/// </example>
/// <seealso cref="MediatorOptions"/>
/// <seealso cref="MediatorImplementation"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
public class MediatorValidationResult {

  /// <summary>
  /// Initializes a new instance of the <see cref="MediatorValidationResult"/> class with validation success status.
  /// </summary>
  /// <param name="isValid">
  /// <see langword="true"/> if validation passed without errors; otherwise, <see langword="false"/>.
  /// </param>
  /// <param name="validatedHandlerCount">
  /// The number of handlers that were successfully validated. Must be non-negative.
  /// </param>
  /// <param name="errors">
  /// Optional collection of validation error messages. If null, an empty collection will be created.
  /// </param>
  /// <remarks>
  /// <para>
  /// This constructor allows creating validation results with specific validation outcomes,
  /// supporting both successful validation scenarios and failure cases with detailed error information.
  /// </para>
  /// <para>
  /// The constructor validates input parameters to ensure consistency between the success status
  /// and the presence of errors. If errors are provided, the validation status will be set to false.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Successful validation
  /// var successResult = new MediatorValidationResult(isValid: true, validatedHandlerCount: 15);
  /// 
  /// // Failed validation with specific errors
  /// var errors = new[] { 
  ///   "Handler not found for GetUserQuery", 
  ///   "CreateUserCommand handler has unresolved dependencies" 
  /// };
  /// var failureResult = new MediatorValidationResult(isValid: false, validatedHandlerCount: 13, errors);
  /// 
  /// // Validation with mixed results
  /// var partialResult = new MediatorValidationResult(
  ///   isValid: false, 
  ///   validatedHandlerCount: 10, 
  ///   errors: new[] { "Optional validation warnings" });
  /// </code>
  /// </example>
  public MediatorValidationResult(bool isValid, int validatedHandlerCount, IEnumerable<string>? errors = null) {
    ArgumentOutOfRangeException.ThrowIfNegative(validatedHandlerCount);

    var errorList = errors?.ToList() ?? [];

    // Ensure consistency: if there are errors, validation cannot be considered successful
    IsValid = isValid && errorList.Count == 0;
    ValidatedHandlerCount = validatedHandlerCount;
    Errors = errorList.AsReadOnly();
  }

  /// <summary>
  /// Gets a value indicating whether the mediator validation was successful.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if all validation checks passed without errors; 
  /// otherwise, <see langword="false"/> indicating configuration issues were found.
  /// </value>
  /// <remarks>
  /// <para>
  /// This property indicates the overall validation status and determines whether the
  /// application can safely proceed with mediator operations. A value of <see langword="false"/>
  /// typically indicates critical configuration issues that would prevent proper operation.
  /// </para>
  /// <para>
  /// Validation success criteria:
  /// <list type="bullet">
  ///   <item><description>All registered request types have corresponding handlers</description></item>
  ///   <item><description>All handler dependencies can be resolved from the DI container</description></item>
  ///   <item><description>No interface implementation violations or type mismatches</description></item>
  ///   <item><description>Decorator pipeline configuration is valid and compatible</description></item>
  ///   <item><description>No critical configuration errors in mediator options</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public bool IsValid { get; }

  /// <summary>
  /// Gets the read-only collection of validation error messages encountered during the validation process.
  /// </summary>
  /// <value>
  /// A read-only list containing detailed error messages describing validation failures.
  /// Empty collection indicates no errors were found during validation.
  /// </value>
  /// <remarks>
  /// <para>
  /// Error messages provide specific information about configuration issues including:
  /// <list type="bullet">
  ///   <item><description>Missing handler registrations with specific request type names</description></item>
  ///   <item><description>Unresolvable dependencies with service type and registration details</description></item>
  ///   <item><description>Interface implementation violations with specific type information</description></item>
  ///   <item><description>Decorator configuration conflicts with affected request types</description></item>
  ///   <item><description>Invalid mediator options with problematic setting names</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Error message format follows structured patterns for easy parsing and integration
  /// with logging systems, monitoring tools, and automated deployment validation.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// foreach (var error in validationResult.Errors) {
  ///   logger.LogError("Mediator validation error: {Error}", error);
  /// }
  /// 
  /// // Check for specific error patterns
  /// var missingHandlers = validationResult.Errors
  ///   .Where(error =&gt; error.Contains("Handler not found"))
  ///   .ToList();
  /// 
  /// if (missingHandlers.Any()) {
  ///   throw new InvalidOperationException($"Critical handlers missing: {string.Join(", ", missingHandlers)}");
  /// }
  /// </code>
  /// </example>
  public IReadOnlyList<string> Errors { get; }

  /// <summary>
  /// Gets the total number of handlers that were successfully validated during the validation process.
  /// </summary>
  /// <value>
  /// A non-negative integer representing the count of handlers that passed all validation checks.
  /// This includes both request handlers and notification handlers that were validated.
  /// </value>
  /// <remarks>
  /// <para>
  /// This count includes validation of:
  /// <list type="bullet">
  ///   <item><description>Request handlers: Handlers implementing <see cref="IRequestHandler{TRequest, TResponse}"/></description></item>
  ///   <item><description>Notification handlers: Handlers for <see cref="INotification"/> implementations</description></item>
  ///   <item><description>Decorator handlers: Decorators implementing <see cref="IRequestDecorator{TRequest, TResponse}"/></description></item>
  ///   <item><description>Generic handlers: Handlers with generic type constraints and complex type parameters</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The count represents successfully validated handlers only. Handlers that failed validation
  /// due to missing dependencies, interface violations, or other configuration issues are not
  /// included in this count but are reported in the <see cref="Errors"/> collection.
  /// </para>
  /// <para>
  /// This metric is useful for:
  /// <list type="bullet">
  ///   <item><description>Monitoring: Tracking handler coverage and registration completeness</description></item>
  ///   <item><description>Health checks: Reporting mediator infrastructure status</description></item>
  ///   <item><description>Diagnostics: Comparing expected vs actual handler registration counts</description></item>
  ///   <item><description>Deployment validation: Ensuring all required handlers are properly registered</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Log validation metrics
  /// logger.LogInformation(
  ///   "Mediator validation completed: {IsValid}, Handlers: {HandlerCount}, Errors: {ErrorCount}",
  ///   result.IsValid, 
  ///   result.ValidatedHandlerCount, 
  ///   result.Errors.Count);
  /// 
  /// // Health check integration
  /// var healthData = new Dictionary&lt;string, object&gt; {
  ///   ["validated_handlers"] = result.ValidatedHandlerCount,
  ///   ["validation_errors"] = result.Errors.Count,
  ///   ["is_valid"] = result.IsValid
  /// };
  /// 
  /// // Monitoring and alerting
  /// if (result.ValidatedHandlerCount &lt; expectedHandlerCount) {
  ///   logger.LogWarning(
  ///     "Handler count mismatch: Expected {Expected}, Validated {Actual}",
  ///     expectedHandlerCount, 
  ///     result.ValidatedHandlerCount);
  /// }
  /// </code>
  /// </example>
  public int ValidatedHandlerCount { get; }

  /// <summary>
  /// Creates a successful validation result with the specified handler count.
  /// </summary>
  /// <param name="validatedHandlerCount">
  /// The number of handlers that were successfully validated. Must be non-negative.
  /// </param>
  /// <returns>
  /// A <see cref="MediatorValidationResult"/> instance indicating successful validation.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This factory method provides a convenient way to create successful validation results
  /// without needing to specify validation status or error collections explicitly.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var successResult = MediatorValidationResult.Success(handlerCount: 25);
  /// 
  /// Debug.Assert(successResult.IsValid);
  /// Debug.Assert(successResult.ValidatedHandlerCount == 25);
  /// Debug.Assert(successResult.Errors.Count == 0);
  /// </code>
  /// </example>
  public static MediatorValidationResult Success(int validatedHandlerCount) =>
    new(isValid: true, validatedHandlerCount, errors: null);

  /// <summary>
  /// Creates a failed validation result with the specified errors and handler count.
  /// </summary>
  /// <param name="errors">
  /// Collection of validation error messages describing the configuration issues found.
  /// </param>
  /// <param name="validatedHandlerCount">
  /// The number of handlers that were successfully validated before errors were encountered.
  /// Defaults to 0. Must be non-negative.
  /// </param>
  /// <returns>
  /// A <see cref="MediatorValidationResult"/> instance indicating failed validation with detailed error information.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="errors"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This factory method provides a convenient way to create failed validation results
  /// with detailed error information for troubleshooting and diagnostics.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var errors = new[] {
  ///   "Handler not found for request type 'GetUserQuery'",
  ///   "Unable to resolve dependency 'IUserRepository' for 'CreateUserCommandHandler'"
  /// };
  /// 
  /// var failureResult = MediatorValidationResult.Failure(errors, validatedHandlerCount: 10);
  /// 
  /// Debug.Assert(!failureResult.IsValid);
  /// Debug.Assert(failureResult.ValidatedHandlerCount == 10);
  /// Debug.Assert(failureResult.Errors.Count == 2);
  /// </code>
  /// </example>
  public static MediatorValidationResult Failure(IEnumerable<string> errors, int validatedHandlerCount = 0) {
    ArgumentNullException.ThrowIfNull(errors);
    return new(isValid: false, validatedHandlerCount, errors);
  }

  /// <summary>
  /// Creates a failed validation result with a single error message.
  /// </summary>
  /// <param name="error">
  /// The validation error message describing the configuration issue.
  /// </param>
  /// <param name="validatedHandlerCount">
  /// The number of handlers that were successfully validated before the error was encountered.
  /// Defaults to 0. Must be non-negative.
  /// </param>
  /// <returns>
  /// A <see cref="MediatorValidationResult"/> instance indicating failed validation with the specified error.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="error"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This factory method provides a convenient way to create failed validation results
  /// for single error scenarios, such as critical configuration failures.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// var result = MediatorValidationResult.Failure(
  ///   "Critical error: IMediator service not registered in DI container",
  ///   validatedHandlerCount: 0);
  /// 
  /// Debug.Assert(!result.IsValid);
  /// Debug.Assert(result.Errors.Count == 1);
  /// Debug.Assert(result.ValidatedHandlerCount == 0);
  /// </code>
  /// </example>
  public static MediatorValidationResult Failure(string error, int validatedHandlerCount = 0) {
    ArgumentNullException.ThrowIfNull(error);
    return new(isValid: false, validatedHandlerCount, errors: [error]);
  }
}
