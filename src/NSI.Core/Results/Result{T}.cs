namespace NSI.Core.Results;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error, implementing functional programming patterns for robust error handling.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
/// <para>
/// The Result pattern provides a functional approach to error handling by explicitly
/// representing success and failure states in the type system. This eliminates the need
/// for exceptions in expected error scenarios and forces developers to handle both cases
/// at compile time, leading to more robust and predictable code.
/// </para>
/// <para>
/// Key benefits and design principles:
/// <list type="bullet">
///   <item><description>Type safety: Compile-time guarantee that errors are handled, preventing silent failures</description></item>
///   <item><description>Performance: No exception overhead for expected failures, using efficient value types</description></item>
///   <item><description>Composability: Chain operations with automatic error propagation using Map, Bind, and Match</description></item>
///   <item><description>Explicit contracts: Makes error handling visible in the API signature</description></item>
///   <item><description>Immutability: Results are immutable value types, ensuring thread safety</description></item>
/// </list>
/// </para>
/// <para>
/// Functional programming patterns supported:
/// <list type="bullet">
///   <item><description>Functor pattern: Map operations transform success values while preserving failure states</description></item>
///   <item><description>Monad pattern: Bind operations enable chaining of Result-returning functions</description></item>
///   <item><description>Railway-oriented programming: Automatic error propagation through operation chains</description></item>
///   <item><description>Pattern matching: Explicit handling of both success and failure cases with Match</description></item>
///   <item><description>Side effects: Tap operations for logging and debugging without affecting the result</description></item>
/// </list>
/// </para>
/// <para>
/// Performance characteristics: This struct is designed for minimal memory overhead with value semantics.
/// Success results store the value directly without boxing. Failure results carry structured error information
/// but avoid expensive exception stack traces unless explicitly needed. All operations are designed to be
/// zero-allocation in the success path.
/// </para>
/// <para>
/// Thread safety: Result instances are immutable value types, making them inherently thread-safe.
/// Multiple threads can safely read from the same Result instance without synchronization. The contained
/// values should also be considered for thread safety in concurrent scenarios.
/// </para>
/// <para>
/// Integration patterns: Results integrate seamlessly with async/await, LINQ operations, dependency injection,
/// HTTP API responses, and structured logging. They provide a consistent error handling model across
/// service boundaries and can be easily converted to HTTP problem details or other response formats.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic creation and usage
/// var successResult = Result.Success(42);
/// var failureResult = Result.Failure&lt;int&gt;("Invalid input");
/// 
/// // Functional composition with chaining
/// var processedResult = ParseInteger("10")
///   .Map(x => x * 2)                    // Transform success value
///   .Bind(x => x > 15 
///     ? Result.Success(x) 
///     : Result.Failure&lt;int&gt;("Too small")) // Chain validation
///   .Tap(value => _logger.LogInformation("Processed value: {Value}", value)) // Side effect
///   .TapError(error => _logger.LogWarning("Processing failed: {Error}", error)); // Error side effect
/// 
/// // Pattern matching for final handling
/// var message = processedResult.Match(
///   onSuccess: value => $"Result: {value}",
///   onFailure: error => $"Error: {error.Message}"
/// );
/// 
/// // Service layer integration
/// public class OrderService {
///   public async Task&lt;Result&lt;Order&gt;&gt; CreateOrderAsync(CreateOrderRequest request) {
///     return await ValidateRequest(request)
///       .Bind(async validRequest => await CheckInventory(validRequest))
///       .Bind(async availableItems => await CalculateTotal(availableItems))
///       .Bind(async orderData => await SaveOrder(orderData))
///       .TapError(error => _logger.LogError("Order creation failed: {Error}", error));
///   }
/// }
/// 
/// // API controller integration with HTTP responses
/// [ApiController]
/// public class OrdersController {
///   [HttpPost]
///   public async Task&lt;IActionResult&gt; CreateOrder(CreateOrderRequest request) {
///     var result = await _orderService.CreateOrderAsync(request);
///     
///     return result.Match(
///       onSuccess: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order),
///       onFailure: error => error.Type switch {
///         ErrorType.Validation => BadRequest(error.ToProblemDetails()),
///         ErrorType.NotFound => NotFound(error.ToProblemDetails()),
///         ErrorType.Conflict => Conflict(error.ToProblemDetails()),
///         ErrorType.BusinessRule => UnprocessableEntity(error.ToProblemDetails()),
///         _ => Problem(error.ToProblemDetails())
///       }
///     );
///   }
/// }
/// 
/// // LINQ integration for batch processing
/// var results = orders
///   .Select(order => ProcessOrder(order))
///   .Where(result => result.IsSuccess)
///   .Select(result => result.Value)
///   .ToList();
/// 
/// // Async enumeration with Results
/// await foreach (var item in GetItemsAsync()) {
///   var result = await ProcessItemAsync(item);
///   if (result.IsFailure) {
///     await _logger.LogErrorAsync("Item processing failed: {Error}", result.Error);
///     break; // Stop on first failure
///   }
/// }
/// </code>
/// </example>
/// <seealso cref="Result"/>
/// <seealso cref="ResultError"/>
/// <seealso cref="ErrorType"/>
public readonly struct Result<T>: IEquatable<Result<T>> {
  
  private readonly T _Value;
  private readonly ResultError _Error;

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> struct with a success value, ensuring type safety and null validation.
  /// </summary>
  /// <param name="value">The success value that represents the successful operation outcome.</param>
  /// <exception cref="ArgumentNullException">Thrown when value is null for reference types, maintaining the success contract integrity.</exception>
  /// <remarks>
  /// <para>
  /// This internal constructor is used by the static factory methods to create successful results
  /// with proper validation. It ensures that success results never contain null values for reference types,
  /// maintaining the integrity of the success contract.
  /// </para>
  /// <para>
  /// Validation strategy: Null validation is performed only for reference types to prevent
  /// inconsistent states. Value types (including nullable value types) are accepted as-is
  /// since they cannot be null in the traditional sense.
  /// </para>
  /// <para>
  /// Performance considerations: The validation is a simple null check with minimal overhead.
  /// The struct initialization is optimized for value semantics with direct field assignment.
  /// </para>
  /// </remarks>
  internal Result(T value) {
    if (value is null) {
      throw new ArgumentNullException(nameof(value), "Success value cannot be null");
    }

    _Value = value;
    _Error = default;
    IsSuccess = true;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> struct with an error, creating a failure result with detailed error information.
  /// </summary>
  /// <param name="error">The error describing the failure with type categorization, code, and message.</param>
  /// <remarks>
  /// <para>
  /// This internal constructor creates a failure result with comprehensive error information.
  /// The value field is set to default to avoid holding potentially expensive object references
  /// in failure scenarios.
  /// </para>
  /// <para>
  /// Error handling strategy: The error parameter should contain sufficient information for
  /// debugging, logging, user communication, and HTTP response mapping. The error type enables
  /// automated response status code selection in API scenarios.
  /// </para>
  /// <para>
  /// Performance considerations: Failure results avoid storing the default value to minimize
  /// memory overhead, especially for expensive-to-construct types.
  /// </para>
  /// </remarks>
  internal Result(ResultError error) {
    _Value = default!;
    _Error = error;
    IsSuccess = false;
  }

  /// <summary>
  /// Gets a value indicating whether the result represents a successful operation outcome.
  /// </summary>
  /// <value><c>true</c> if the operation succeeded and contains a valid value; otherwise, <c>false</c>.</value>
  /// <remarks>
  /// <para>
  /// This property is the primary discriminator for Result pattern matching. It determines
  /// which fields are safe to access and which operations are valid to perform.
  /// </para>
  /// <para>
  /// Usage pattern: Always check IsSuccess before accessing Value, or use the Match method
  /// for safe pattern matching that handles both cases explicitly.
  /// </para>
  /// </remarks>
  public bool IsSuccess { get; }

  /// <summary>
  /// Gets a value indicating whether the result represents a failed operation with error information.
  /// </summary>
  /// <value><c>true</c> if the operation failed and contains error details; otherwise, <c>false</c>.</value>
  /// <remarks>
  /// <para>
  /// This property is the inverse of IsSuccess and provides a convenient way to check for
  /// failure states in conditional logic and LINQ expressions.
  /// </para>
  /// <para>
  /// Usage pattern: Use this property in guard clauses, early returns, and filtering operations
  /// where failure detection is the primary concern.
  /// </para>
  /// </remarks>
  public bool IsFailure => !IsSuccess;

  /// <summary>
  /// Gets the success value from a successful result, providing safe access to the operation outcome.
  /// </summary>
  /// <value>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, throws an exception.</value>
  /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failure result, preventing unsafe access.</exception>
  /// <remarks>
  /// <para>
  /// This property provides access to the success value with runtime validation to prevent
  /// unsafe access patterns. It should only be accessed after verifying IsSuccess or within
  /// the success branch of a Match operation.
  /// </para>
  /// <para>
  /// Safety considerations: Direct access to Value on failure results throws an exception
  /// to prevent silent bugs and data corruption. Use Match, Map, or Bind for safe access patterns.
  /// </para>
  /// <para>
  /// Performance characteristics: Property access is direct field access with a single boolean check.
  /// No allocation or boxing occurs for value types.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Safe access pattern
  /// if (result.IsSuccess) {
  ///   var value = result.Value; // Safe access
  ///   ProcessValue(value);
  /// }
  /// 
  /// // Preferred pattern matching approach
  /// result.Match(
  ///   onSuccess: value => ProcessValue(value), // Safe automatic access
  ///   onFailure: error => HandleError(error)
  /// );
  /// 
  /// // LINQ integration
  /// var successfulResults = results
  ///   .Where(r => r.IsSuccess)
  ///   .Select(r => r.Value); // Safe because of Where filter
  /// </code>
  /// </example>
  public T Value => IsSuccess ? _Value : throw new InvalidOperationException("Cannot access Value of a failure result");

  /// <summary>
  /// Gets the error information from a failed result, providing safe access to failure details.
  /// </summary>
  /// <value>The error information if <see cref="IsFailure"/> is <c>true</c>; otherwise, throws an exception.</value>
  /// <exception cref="InvalidOperationException">Thrown when accessing Error on a success result, preventing unsafe access.</exception>
  /// <remarks>
  /// <para>
  /// This property provides access to detailed error information including error type, code, message,
  /// and optional exception context. It should only be accessed after verifying IsFailure or within
  /// the failure branch of a Match operation.
  /// </para>
  /// <para>
  /// Error information usage: The ResultError contains structured information suitable for logging,
  /// user communication, HTTP response mapping, and debugging. Different error types map to
  /// appropriate HTTP status codes for API responses.
  /// </para>
  /// <para>
  /// Safety considerations: Direct access to Error on success results throws an exception
  /// to maintain type safety and prevent programming errors.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Safe error access pattern
  /// if (result.IsFailure) {
  ///   var error = result.Error; // Safe access
  ///   _logger.LogError("Operation failed: {ErrorCode} - {Message}", error.Code, error.Message);
  /// }
  /// 
  /// // Pattern matching for error handling
  /// result.Match(
  ///   onSuccess: value => ProcessSuccess(value),
  ///   onFailure: error => error.Type switch {
  ///     ErrorType.Validation => HandleValidationError(error),
  ///     ErrorType.NotFound => HandleNotFoundError(error),
  ///     _ => HandleGenericError(error)
  ///   }
  /// );
  /// 
  /// // Error logging with TapError
  /// result.TapError(error => {
  ///   _logger.LogError(error.Exception, "Service call failed: {Code}", error.Code);
  ///   _metrics.IncrementErrorCounter(error.Type.ToString());
  /// });
  /// </code>
  /// </example>
  public ResultError Error => IsFailure ? _Error : throw new InvalidOperationException("Cannot access Error of a success result");

  /// <summary>
  /// Transforms the success value using the specified function while preserving failure states, implementing the Functor pattern.
  /// </summary>
  /// <typeparam name="TResult">The type of the transformed success value.</typeparam>
  /// <param name="mapper">The transformation function to apply to the success value.</param>
  /// <returns>A new Result with the transformed value if successful, or the original error if this is a failure.</returns>
  /// <exception cref="ArgumentNullException">Thrown when mapper is null.</exception>
  /// <remarks>
  /// <para>
  /// The Map operation implements the Functor pattern, allowing transformation of success values
  /// while automatically preserving error states. This enables building processing pipelines
  /// where transformations only occur on successful results.
  /// </para>
  /// <para>
  /// Transformation behavior: If this result is successful, the mapper function is applied to the value
  /// and a new successful result is created with the transformed value. If this result is a failure,
  /// the mapper is not executed and the error is propagated unchanged.
  /// </para>
  /// <para>
  /// Performance considerations: Map operations are lazy and only execute the transformation
  /// for successful results. Failed results bypass the transformation entirely, providing
  /// efficient error propagation through processing chains.
  /// </para>
  /// <para>
  /// Chaining operations: Map can be chained with other Result operations (Map, Bind, Tap)
  /// to create fluent processing pipelines that handle errors automatically.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic transformation
  /// var result = Result.Success(5)
  ///   .Map(x => x * 2); // Result.Success(10)
  /// 
  /// var failure = Result.Failure&lt;int&gt;("Error")
  ///   .Map(x => x * 2); // Still Result.Failure&lt;int&gt;("Error") - mapper not called
  /// 
  /// // Complex transformations with objects
  /// var userResult = GetUser(userId)
  ///   .Map(user => new UserDto {
  ///     Id = user.Id,
  ///     Name = user.FullName,
  ///     Email = user.Email
  ///   });
  /// 
  /// // Chaining multiple transformations
  /// var processingResult = LoadRawData()
  ///   .Map(data => ParseData(data))
  ///   .Map(parsed => ValidateStructure(parsed))
  ///   .Map(validated => EnrichData(validated));
  /// 
  /// // Type transformations in processing pipelines
  /// var reportResult = GetOrderData(orderId)
  ///   .Map(order => CalculateMetrics(order))        // Order -> OrderMetrics
  ///   .Map(metrics => GenerateCharts(metrics))      // OrderMetrics -> ChartData
  ///   .Map(charts => CreateReport(charts));         // ChartData -> Report
  /// 
  /// // Conditional transformations
  /// var discountResult = GetCustomer(customerId)
  ///   .Map(customer => customer.IsPremium 
  ///     ? ApplyPremiumDiscount(customer) 
  ///     : ApplyStandardDiscount(customer));
  /// 
  /// // Error handling with Map in service layers
  /// public Result&lt;OrderSummaryDto&gt; GetOrderSummary(int orderId) {
  ///   return _orderRepository.GetById(orderId)
  ///     .Map(order => new OrderSummaryDto {
  ///       Id = order.Id,
  ///       Total = order.Items.Sum(i => i.Price * i.Quantity),
  ///       Status = order.Status.ToString(),
  ///       CreatedAt = order.CreatedAt
  ///     });
  /// }
  /// </code>
  /// </example>
  public Result<TResult> Map<TResult>(Func<T, TResult> mapper) {
    ArgumentNullException.ThrowIfNull(mapper);

    return IsSuccess ? Result.Success(mapper(_Value)) : Result.Failure<TResult>(_Error);
  }

  /// <summary>
  /// Chains another Result-returning operation if this result is successful, implementing the Monad pattern for composable error handling.
  /// </summary>
  /// <typeparam name="TResult">The type of the next operation's success value.</typeparam>
  /// <param name="binder">The function that takes the success value and returns another Result.</param>
  /// <returns>The result of the binder function if this is successful, or the original error propagated unchanged.</returns>
  /// <exception cref="ArgumentNullException">Thrown when binder is null.</exception>
  /// <remarks>
  /// <para>
  /// The Bind operation implements the Monad pattern, enabling the chaining of operations that may fail.
  /// Unlike Map, which transforms values, Bind chains operations that themselves return Results,
  /// preventing nested Result structures and enabling fluent error handling.
  /// </para>
  /// <para>
  /// Chaining behavior: If this result is successful, the binder function is called with the success value
  /// and its Result is returned. If this result is a failure, the binder is not executed and the error
  /// is propagated. This creates a "railway-oriented" programming model where errors short-circuit the chain.
  /// </para>
  /// <para>
  /// Error propagation: Bind ensures that the first failure in a chain becomes the final result,
  /// allowing early termination of processing pipelines when errors occur. This eliminates
  /// the need for explicit error checking at each step.
  /// </para>
  /// <para>
  /// Use cases: Bind is ideal for validation chains, service call sequences, database operations,
  /// and any scenario where subsequent operations depend on the success of previous ones.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic chaining with validation
  /// var result = Result.Success(5)
  ///   .Bind(x => x > 0 
  ///     ? Result.Success(x * 2) 
  ///     : Result.Failure&lt;int&gt;("Must be positive"));
  /// 
  /// // Service operation chaining
  /// var userCreationResult = ValidateUserRequest(request)
  ///   .Bind(validRequest => CheckEmailUniqueness(validRequest.Email))
  ///   .Bind(email => CreateUserAccount(validRequest, email))
  ///   .Bind(account => SendWelcomeEmail(account))
  ///   .Bind(account => GenerateApiKey(account));
  /// 
  /// // Database operation chain
  /// var orderProcessingResult = await ValidateOrder(orderRequest)
  ///   .Bind(async order => await ReserveInventory(order))
  ///   .Bind(async reservation => await ProcessPayment(reservation))
  ///   .Bind(async payment => await CreateShipment(payment))
  ///   .Bind(async shipment => await UpdateOrderStatus(shipment));
  /// 
  /// // Multi-step validation with early termination
  /// public Result&lt;ProcessedOrder&gt; ProcessOrder(RawOrderData rawData) {
  ///   return ValidateOrderFormat(rawData)
  ///     .Bind(formatted => ValidateCustomerExists(formatted.CustomerId))
  ///     .Bind(customer => ValidateProductAvailability(formatted.Items))
  ///     .Bind(items => CalculatePricing(formatted, items))
  ///     .Bind(pricing => ApplyDiscounts(pricing, customer))
  ///     .Bind(finalPricing => CreateProcessedOrder(formatted, finalPricing));
  /// }
  /// 
  /// // Conditional logic with Bind
  /// var processingResult = GetUserRole(userId)
  ///   .Bind(role => role == UserRole.Admin 
  ///     ? ProcessAdminRequest(request)
  ///     : ProcessUserRequest(request, role));
  /// 
  /// // Error recovery patterns
  /// var withFallbackResult = PrimaryOperation(data)
  ///   .Bind(result => result.IsSuccess 
  ///     ? Result.Success(result) 
  ///     : FallbackOperation(data));
  /// 
  /// // Complex business logic chaining
  /// public async Task&lt;Result&lt;InvoiceDto&gt;&gt; GenerateInvoiceAsync(int orderId) {
  ///   return await GetOrder(orderId)
  ///     .Bind(order => ValidateOrderForInvoicing(order))
  ///     .Bind(async validOrder => await CalculateTaxes(validOrder))
  ///     .Bind(async taxInfo => await GenerateInvoiceNumber(taxInfo))
  ///     .Bind(async invoice => await SaveInvoice(invoice))
  ///     .Map(invoice => MapToDto(invoice));
  /// }
  /// </code>
  /// </example>
  public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder) {
    ArgumentNullException.ThrowIfNull(binder);

    return IsSuccess ? binder(_Value) : Result.Failure<TResult>(_Error);
  }

  /// <summary>
  /// Executes one of two functions based on the result state and returns the result, providing exhaustive pattern matching for Result handling.
  /// </summary>
  /// <typeparam name="TResult">The type of the value returned by both functions.</typeparam>
  /// <param name="onSuccess">The function to execute with the success value when the result is successful.</param>
  /// <param name="onFailure">The function to execute with the error information when the result is a failure.</param>
  /// <returns>The result of executing either the success or failure function.</returns>
  /// <exception cref="ArgumentNullException">Thrown when onSuccess or onFailure is null.</exception>
  /// <remarks>
  /// <para>
  /// The Match method provides exhaustive pattern matching for Result types, ensuring that both
  /// success and failure cases are explicitly handled. This eliminates the possibility of
  /// unhandled states and provides a functional programming approach to result processing.
  /// </para>
  /// <para>
  /// Pattern matching behavior: Exactly one of the two functions will be executed based on the
  /// result state. The appropriate function receives the relevant data (value or error) and
  /// its return value becomes the final result of the Match operation.
  /// </para>
  /// <para>
  /// Type safety: Match ensures that all possible result states are handled at compile time,
  /// preventing runtime errors from unhandled cases. Both functions must return the same type,
  /// ensuring consistent handling regardless of the result state.
  /// </para>
  /// <para>
  /// Use cases: Match is ideal for final result processing, API response generation, user interface
  /// updates, logging, and any scenario where different actions are needed for success vs failure.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic pattern matching
  /// var message = result.Match(
  ///   onSuccess: value => $"Success: {value}",
  ///   onFailure: error => $"Error: {error.Message}"
  /// );
  /// 
  /// // HTTP API response generation
  /// return result.Match(
  ///   onSuccess: data => Ok(data),
  ///   onFailure: error => error.Type switch {
  ///     ErrorType.Validation => BadRequest(error.ToProblemDetails()),
  ///     ErrorType.NotFound => NotFound(error.ToProblemDetails()),
  ///     ErrorType.Authorization => Forbid(),
  ///     _ => Problem(error.ToProblemDetails())
  ///   }
  /// );
  /// 
  /// // Complex business logic handling
  /// var finalResult = processResult.Match(
  ///   onSuccess: processedData => {
  ///     _logger.LogInformation("Processing completed successfully");
  ///     _metrics.IncrementSuccessCounter();
  ///     return new ProcessingReport { 
  ///       Success = true, 
  ///       Data = processedData,
  ///       ProcessedAt = DateTime.UtcNow
  ///     };
  ///   },
  ///   onFailure: error => {
  ///     _logger.LogError("Processing failed: {Error}", error);
  ///     _metrics.IncrementErrorCounter(error.Type.ToString());
  ///     _alertService.SendAlert($"Processing failed: {error.Code}");
  ///     return new ProcessingReport { 
  ///       Success = false, 
  ///       ErrorMessage = error.Message,
  ///       ProcessedAt = DateTime.UtcNow
  ///     };
  ///   }
  /// );
  /// 
  /// // User interface updates
  /// var uiState = userResult.Match(
  ///   onSuccess: user => new UiState {
  ///     IsLoading = false,
  ///     User = user,
  ///     ErrorMessage = null
  ///   },
  ///   onFailure: error => new UiState {
  ///     IsLoading = false,
  ///     User = null,
  ///     ErrorMessage = error.Message
  ///   }
  /// );
  /// 
  /// // Async operations with Match
  /// var response = await orderResult.Match(
  ///   onSuccess: async order => {
  ///     await _notificationService.SendOrderConfirmation(order);
  ///     return new ApiResponse { Success = true, Data = order };
  ///   },
  ///   onFailure: async error => {
  ///     await _auditService.LogFailure("OrderCreation", error);
  ///     return new ApiResponse { Success = false, Error = error.Message };
  ///   }
  /// );
  /// 
  /// // Data transformation with Match
  /// var dto = entityResult.Match(
  ///   onSuccess: entity => _mapper.Map&lt;EntityDto&gt;(entity),
  ///   onFailure: error => new EntityDto { 
  ///     IsValid = false, 
  ///     ValidationErrors = error.ValidationErrors?.ToList() ?? new()
  ///   }
  /// );
  /// </code>
  /// </example>
  public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure) {
    ArgumentNullException.ThrowIfNull(onSuccess);
    ArgumentNullException.ThrowIfNull(onFailure);

    return IsSuccess ? onSuccess(_Value) : onFailure(_Error);
  }

  /// <summary>
  /// Executes a side effect action if the result is successful without changing the result, enabling debugging and logging in processing chains.
  /// </summary>
  /// <param name="action">The action to execute with the success value for side effects like logging or notifications.</param>
  /// <returns>The same result instance unchanged, enabling method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  /// <remarks>
  /// <para>
  /// The Tap method provides a way to perform side effects (logging, notifications, debugging)
  /// on successful results without modifying the result itself. This is essential for building
  /// observable processing pipelines where you need to inspect intermediate states.
  /// </para>
  /// <para>
  /// Side effect execution: The action is only executed for successful results. Failure results
  /// pass through unchanged without executing the action, maintaining the error propagation chain.
  /// </para>
  /// <para>
  /// Immutability preservation: Tap returns the exact same result instance, ensuring that the
  /// processing chain continues with the original result state. The action should not modify
  /// the result or its contained value.
  /// </para>
  /// <para>
  /// Common use cases: Logging successful operations, sending notifications, updating metrics,
  /// debugging intermediate values, caching results, and triggering side effects that don't
  /// affect the main processing flow.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic logging in processing chain
  /// var result = ProcessData(input)
  ///   .Tap(data => _logger.LogInformation("Data processed successfully: {Count} items", data.Count))
  ///   .Map(data => TransformData(data))
  ///   .Tap(transformed => _logger.LogDebug("Data transformed: {Size} bytes", transformed.Size));
  /// 
  /// // Metrics and monitoring
  /// var processingResult = AnalyzeData(rawData)
  ///   .Tap(analysis => {
  ///     _metrics.RecordProcessingTime(stopwatch.ElapsedMilliseconds);
  ///     _metrics.IncrementSuccessCounter("DataAnalysis");
  ///   })
  ///   .Map(analysis => GenerateReport(analysis));
  /// 
  /// // Notification side effects
  /// var orderResult = CreateOrder(request)
  ///   .Tap(order => _eventBus.Publish(new OrderCreated(order.Id)))
  ///   .Tap(order => _emailService.SendOrderConfirmation(order.CustomerEmail))
  ///   .Map(order => _mapper.Map&lt;OrderDto&gt;(order));
  /// 
  /// // Debugging and development
  /// var debugResult = ComplexCalculation(input)
  ///   .Tap(intermediate => Console.WriteLine($"Intermediate result: {intermediate}"))
  ///   .Map(intermediate => FinalTransformation(intermediate))
  ///   .Tap(final => Console.WriteLine($"Final result: {final}"));
  /// 
  /// // Caching successful results
  /// var cachedResult = ExpensiveOperation(key)
  ///   .Tap(result => _cache.Set(key, result, TimeSpan.FromMinutes(30)))
  ///   .Map(result => PostProcessResult(result));
  /// 
  /// // Multiple side effects in sequence
  /// var multiTapResult = ProcessOrder(orderData)
  ///   .Tap(order => _auditLogger.LogOrderCreation(order))
  ///   .Tap(order => _analytics.TrackOrderValue(order.Total))
  ///   .Tap(order => _inventoryService.ReserveItems(order.Items))
  ///   .Map(order => CalculateShipping(order));
  /// 
  /// // Conditional side effects
  /// var conditionalTapResult = GetUserData(userId)
  ///   .Tap(user => {
  ///     if (user.IsVip) {
  ///       _vipService.NotifyVipAccess(user);
  ///     }
  ///   })
  ///   .Map(user => EnrichUserData(user));
  /// 
  /// // Performance monitoring
  /// var monitoredResult = DatabaseQuery(query)
  ///   .Tap(data => {
  ///     var executionTime = stopwatch.ElapsedMilliseconds;
  ///     if (executionTime > SlowQueryThreshold) {
  ///       _logger.LogWarning("Slow query detected: {Duration}ms", executionTime);
  ///     }
  ///   });
  /// </code>
  /// </example>
  public Result<T> Tap(Action<T> action) {
    ArgumentNullException.ThrowIfNull(action);

    if (IsSuccess) {
      action(_Value);
    }

    return this;
  }

  /// <summary>
  /// Executes a side effect action if the result is a failure without changing the result, enabling error logging and monitoring in processing chains.
  /// </summary>
  /// <param name="action">The action to execute with the error information for side effects like logging or alerting.</param>
  /// <returns>The same result instance unchanged, enabling method chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  /// <remarks>
  /// <para>
  /// The TapError method provides a way to perform side effects (logging, alerting, metrics)
  /// on failed results without modifying the result itself. This is essential for building
  /// observable error handling where you need to react to failures without disrupting error propagation.
  /// </para>
  /// <para>
  /// Side effect execution: The action is only executed for failed results. Successful results
  /// pass through unchanged without executing the action, maintaining the success propagation chain.
  /// </para>
  /// <para>
  /// Error handling patterns: TapError enables centralized error handling, logging, monitoring,
  /// and alerting without affecting the main error propagation flow. This allows for comprehensive
  /// error tracking while preserving the functional error handling model.
  /// </para>
  /// <para>
  /// Common use cases: Error logging, sending alerts, updating error metrics, triggering
  /// error recovery processes, notifying monitoring systems, and debugging failure scenarios.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic error logging in processing chain
  /// var result = ProcessData(input)
  ///   .TapError(error => _logger.LogError("Data processing failed: {ErrorCode} - {Message}", 
  ///     error.Code, error.Message))
  ///   .Map(data => TransformData(data))
  ///   .TapError(error => _logger.LogError("Data transformation failed: {Error}", error));
  /// 
  /// // Error metrics and monitoring
  /// var monitoredResult = AnalyzeData(rawData)
  ///   .TapError(error => {
  ///     _metrics.IncrementErrorCounter("DataAnalysis", error.Type.ToString());
  ///     _healthChecker.ReportFailure("DataAnalysisService");
  ///   });
  /// 
  /// // Error alerting and notifications
  /// var alertedResult = CriticalOperation(data)
  ///   .TapError(error => {
  ///     if (error.Type == ErrorType.Database) {
  ///       _alertService.SendCriticalAlert("Database operation failed", error);
  ///     }
  ///   });
  /// 
  /// // Exception context preservation
  /// var contextPreservedResult = DatabaseOperation(query)
  ///   .TapError(error => {
  ///     if (error.Exception != null) {
  ///       _telemetry.TrackException(error.Exception, new Dictionary&lt;string, string&gt; {
  ///         ["ErrorCode"] = error.Code,
  ///         ["Operation"] = "DatabaseQuery"
  ///       });
  ///     }
  ///   });
  /// 
  /// // Conditional error handling
  /// var conditionalErrorResult = ProcessPayment(paymentInfo)
  ///   .TapError(error => {
  ///     switch (error.Type) {
  ///       case ErrorType.Authentication:
  ///         _auditService.LogSecurityIncident(error);
  ///         break;
  ///       case ErrorType.BusinessRule:
  ///         _notificationService.NotifyCustomer(paymentInfo.CustomerId, error.Message);
  ///         break;
  ///       case ErrorType.ServiceUnavailable:
  ///         _circuitBreaker.RecordFailure();
  ///         break;
  ///     }
  ///   });
  /// 
  /// // Error recovery trigger
  /// var recoveryTriggeredResult = PrimaryService(request)
  ///   .TapError(error => {
  ///     _logger.LogWarning("Primary service failed, triggering fallback: {Error}", error);
  ///     _fallbackService.NotifyFailover(error);
  ///   })
  ///   .Bind(_ => FallbackService(request)); // Continue with fallback
  /// 
  /// // Debugging and development error tracking
  /// var debugErrorResult = ComplexOperation(input)
  ///   .TapError(error => {
  ///     Console.WriteLine($"Operation failed at step: {error.Code}");
  ///     if (error.ValidationErrors?.Any() == true) {
  ///       foreach (var validationError in error.ValidationErrors) {
  ///         Console.WriteLine($"  - {validationError.PropertyName}: {validationError.ErrorMessage}");
  ///       }
  ///     }
  ///   });
  /// 
  /// // Comprehensive error handling chain
  /// var comprehensiveResult = OrderProcessingPipeline(orderRequest)
  ///   .TapError(error => _logger.LogError("Order processing failed: {Error}", error))
  ///   .TapError(error => _metrics.RecordOrderFailure(error.Type))
  ///   .TapError(error => _auditService.LogOrderFailure(orderRequest.OrderId, error))
  ///   .TapError(error => _customerService.NotifyOrderFailure(orderRequest.CustomerId, error));
  /// </code>
  /// </example>
  public Result<T> TapError(Action<ResultError> action) {
    ArgumentNullException.ThrowIfNull(action);

    if (IsFailure) {
      action(_Error);
    }

    return this;
  }

  /// <summary>
  /// Determines whether the specified <see cref="Result{T}"/> is equal to this instance by comparing both the state and content.
  /// </summary>
  /// <param name="other">The other result to compare with this instance.</param>
  /// <returns><c>true</c> if the results are equal in both state and content; otherwise, <c>false</c>.</returns>
  /// <remarks>
  /// <para>
  /// Equality comparison follows value semantics for Result types. Two results are considered equal
  /// if they have the same success/failure state and their contents (value or error) are equal.
  /// </para>
  /// <para>
  /// Comparison logic: First compares the success state, then compares the appropriate content
  /// using the default equality comparer for the value type or the ResultError equality implementation.
  /// </para>
  /// <para>
  /// Performance considerations: Comparison short-circuits on state mismatch and uses efficient
  /// default equality comparers for content comparison.
  /// </para>
  /// </remarks>
  public bool Equals(Result<T> other) {
    if (IsSuccess != other.IsSuccess) {
      return false;
    }

    return IsSuccess
      ? EqualityComparer<T>.Default.Equals(_Value, other._Value)
      : _Error.Equals(other._Error);
  }

  /// <summary>
  /// Determines whether the specified object is equal to this instance by type checking and value comparison.
  /// </summary>
  /// <param name="obj">The object to compare with this instance.</param>
  /// <returns><c>true</c> if the specified object is a Result of the same type and is equal to this instance; otherwise, <c>false</c>.</returns>
  /// <remarks>
  /// <para>
  /// This override provides standard object equality semantics for Result types, enabling use
  /// in collections, dictionaries, and other scenarios requiring object equality.
  /// </para>
  /// <para>
  /// Type safety: Performs type checking to ensure the compared object is a Result of the same
  /// generic type before delegating to the strongly-typed Equals method.
  /// </para>
  /// </remarks>
  public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

  /// <summary>
  /// Returns a hash code for this instance based on the state and content, enabling use in hash-based collections.
  /// </summary>
  /// <returns>A hash code that represents this instance consistently across multiple calls.</returns>
  /// <remarks>
  /// <para>
  /// Hash code calculation combines the success state with either the value hash or error hash
  /// to provide consistent hashing for equal instances. This enables Result types to be used
  /// as keys in dictionaries and items in hash sets.
  /// </para>
  /// <para>
  /// Consistency guarantee: Equal Result instances will always produce the same hash code,
  /// satisfying the requirements of GetHashCode implementations.
  /// </para>
  /// </remarks>
  public override int GetHashCode() => IsSuccess
      ? HashCode.Combine(IsSuccess, _Value)
      : HashCode.Combine(IsSuccess, _Error);

  /// <summary>
  /// Returns a string representation of this result showing the state and content for debugging and logging purposes.
  /// </summary>
  /// <returns>A formatted string indicating whether this is a success or failure result with the relevant content.</returns>
  /// <remarks>
  /// <para>
  /// String representation provides clear, readable output for debugging, logging, and development
  /// scenarios. The format clearly indicates the result state and includes the relevant content.
  /// </para>
  /// <para>
  /// Format specification: Success results show "Success(value)" and failure results show "Failure(error)".
  /// This format is consistent with functional programming conventions and provides immediate clarity.
  /// </para>
  /// </remarks>
  public override string ToString() => IsSuccess
      ? $"Success({_Value})"
      : $"Failure({_Error})";

  /// <summary>
  /// Determines whether two <see cref="Result{T}"/> instances are equal using value semantics.
  /// </summary>
  /// <param name="left">The first result to compare.</param>
  /// <param name="right">The second result to compare.</param>
  /// <returns><c>true</c> if the results are equal; otherwise, <c>false</c>.</returns>
  /// <remarks>
  /// <para>
  /// This operator enables natural equality comparisons between Result instances using the == operator.
  /// It delegates to the Equals method to ensure consistent equality semantics.
  /// </para>
  /// </remarks>
  public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

  /// <summary>
  /// Determines whether two <see cref="Result{T}"/> instances are not equal using value semantics.
  /// </summary>
  /// <param name="left">The first result to compare.</param>
  /// <param name="right">The second result to compare.</param>
  /// <returns><c>true</c> if the results are not equal; otherwise, <c>false</c>.</returns>
  /// <remarks>
  /// <para>
  /// This operator enables natural inequality comparisons between Result instances using the != operator.
  /// It provides the logical inverse of the == operator for complete comparison support.
  /// </para>
  /// </remarks>
  public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

  /// <summary>
  /// Implicitly converts a value to a successful result, enabling natural value-to-Result conversions.
  /// </summary>
  /// <param name="value">The value to wrap in a successful result.</param>
  /// <returns>A successful Result containing the specified value.</returns>
  /// <remarks>
  /// <para>
  /// This implicit conversion enables natural, fluent syntax when working with Result types.
  /// Values can be directly assigned to Result variables or returned from methods without
  /// explicit Result.Success() calls in many scenarios.
  /// </para>
  /// <para>
  /// Usage patterns: This conversion is particularly useful in method returns, assignments,
  /// and LINQ expressions where the success path is the common case.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Direct assignment (implicit conversion)
  /// Result&lt;int&gt; result = 42; // Same as Result.Success(42)
  /// 
  /// // Method return (implicit conversion)
  /// public Result&lt;string&gt; GetUserName(int userId) {
  ///   var user = _repository.GetUser(userId);
  ///   if (user == null) {
  ///     return ResultError.NotFound("USER_NOT_FOUND", "User not found");
  ///   }
  ///   return user.Name; // Implicit conversion to Result&lt;string&gt;
  /// }
  /// 
  /// // LINQ with implicit conversion
  /// var results = values.Select(v => ProcessValue(v) ? v : null)
  ///   .Where(v => v != null)
  ///   .Select(v => (Result&lt;int&gt;)v); // Implicit conversion in Select
  /// </code>
  /// </example>
  public static implicit operator Result<T>(T value) => Result.Success(value);

  /// <summary>
  /// Implicitly converts an error to a failed result, enabling natural error-to-Result conversions.
  /// </summary>
  /// <param name="error">The error to wrap in a failed result.</param>
  /// <returns>A failed Result containing the specified error.</returns>
  /// <remarks>
  /// <para>
  /// This implicit conversion enables natural, fluent syntax when working with error scenarios.
  /// ResultError instances can be directly assigned to Result variables or returned from methods
  /// without explicit Result.Failure() calls.
  /// </para>
  /// <para>
  /// Error handling patterns: This conversion is particularly useful for method returns,
  /// error propagation, and scenarios where error creation and result conversion should be seamless.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Direct assignment (implicit conversion)
  /// Result&lt;string&gt; result = ResultError.NotFound("NOT_FOUND", "Item not found");
  /// 
  /// // Method return with error (implicit conversion)
  /// public Result&lt;User&gt; GetUser(int userId) {
  ///   if (userId &lt;= 0) {
  ///     return ResultError.Validation("INVALID_ID", "User ID must be positive");
  ///   }
  ///   
  ///   var user = _repository.GetUser(userId);
  ///   return user ?? ResultError.NotFound("USER_NOT_FOUND", "User not found");
  /// }
  /// 
  /// // Error propagation in chains
  /// public Result&lt;ProcessedData&gt; ProcessChain(RawData data) {
  ///   var validationError = ValidateData(data);
  ///   if (validationError != null) {
  ///     return validationError; // Implicit conversion from ResultError
  ///   }
  ///   
  ///   return ProcessValidData(data);
  /// }
  /// </code>
  /// </example>
  public static implicit operator Result<T>(ResultError error) => Result.Failure<T>(error);
}
