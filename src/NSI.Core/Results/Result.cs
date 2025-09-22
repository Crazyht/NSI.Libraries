using System.Diagnostics.CodeAnalysis;

namespace NSI.Core.Results;

/// <summary>
/// Provides static factory methods and utility functions for creating and combining Result instances in functional programming scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This static class serves as the primary entry point for creating Result instances and performing
/// operations on collections of Results. It implements common functional programming patterns
/// for error handling, providing a clean alternative to exception-based error handling in business logic.
/// </para>
/// <para>
/// Key capabilities and design principles:
/// <list type="bullet">
///   <item><description>Factory methods: Convenient creation of success and failure results with type inference</description></item>
///   <item><description>Exception integration: Safe bridging between exception-based and Result-based code</description></item>
///   <item><description>Collection operations: Combining multiple results with fail-fast semantics</description></item>
///   <item><description>Async support: Full support for asynchronous operations with proper exception handling</description></item>
///   <item><description>Performance optimization: Zero-allocation success paths and efficient error propagation</description></item>
/// </list>
/// </para>
/// <para>
/// Design patterns and architectural integration:
/// <list type="bullet">
///   <item><description>Railway-oriented programming: Enables chaining operations with automatic error propagation</description></item>
///   <item><description>Functional composition: Supports map, bind, and other functional operations through extension methods</description></item>
///   <item><description>API consistency: Provides uniform error handling across service layers and domain boundaries</description></item>
///   <item><description>HTTP integration: Seamless conversion to HTTP problem details and status codes</description></item>
/// </list>
/// </para>
/// <para>
/// Thread safety: All static methods are thread-safe and can be called concurrently from multiple threads.
/// The Result instances themselves are immutable value types, ensuring thread safety in concurrent scenarios.
/// </para>
/// <para>
/// Performance considerations: Success results have minimal memory overhead as they store values directly.
/// Failure results carry error information but avoid exception stack trace overhead unless explicitly needed.
/// The Try methods provide safe exception boundaries without performance penalties for success cases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic factory method usage
/// var successResult = Result.Success(42);
/// var failureResult = Result.Failure&lt;int&gt;("Invalid input provided");
/// 
/// // Exception integration with Try pattern
/// var parseResult = Result.Try(() => int.Parse("invalid"));
/// var fileResult = await Result.TryAsync(() => File.ReadAllTextAsync("config.json"));
/// 
/// // Combining multiple results
/// var user = Result.Success(new User { Id = 1, Name = "John" });
/// var permissions = Result.Success(new[] { "read", "write" });
/// var settings = Result.Success(new Settings { Theme = "dark" });
/// 
/// var combined = Result.Combine(user, permissions, settings);
/// if (combined.IsSuccess) {
///   var (userData, userPermissions, userSettings) = combined.Value;
///   // All operations succeeded
/// }
/// 
/// // Service layer integration
/// public class UserService {
///   public async Task&lt;Result&lt;User&gt;&gt; CreateUserAsync(CreateUserRequest request) {
///     var validationResult = await ValidateRequestAsync(request);
///     if (validationResult.IsFailure) {
///       return Result.Failure&lt;User&gt;(validationResult.Error);
///     }
/// 
///     return await Result.TryAsync(async () => {
///       var user = new User { Email = request.Email, Name = request.Name };
///       await _repository.CreateAsync(user);
///       return user;
///     });
///   }
/// }
/// 
/// // API controller integration
/// [ApiController]
/// public class UsersController {
///   [HttpPost]
///   public async Task&lt;IActionResult&gt; CreateUser(CreateUserRequest request) {
///     var result = await _userService.CreateUserAsync(request);
///     return result.Match(
///       onSuccess: user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, user),
///       onFailure: error => error.Type switch {
///         ErrorType.Validation => BadRequest(error.ToProblemDetails()),
///         ErrorType.Conflict => Conflict(error.ToProblemDetails()),
///         _ => Problem(error.ToProblemDetails())
///       }
///     );
///   }
/// }
/// </code>
/// </example>
/// <seealso cref="Result{T}"/>
/// <seealso cref="ResultError"/>
/// <seealso cref="ErrorType"/>
public static class Result {

  /// <summary>
  /// Creates a successful result with the specified value, ensuring type safety and null validation for reference types.
  /// </summary>
  /// <typeparam name="T">The type of the success value.</typeparam>
  /// <param name="value">The success value that represents the successful operation outcome.</param>
  /// <returns>A successful Result containing the specified value.</returns>
  /// <exception cref="ArgumentNullException">Thrown when value is null for reference types, ensuring contract safety.</exception>
  /// <remarks>
  /// <para>
  /// This factory method creates a successful Result instance with compile-time type safety.
  /// For reference types, null values are rejected to maintain the integrity of the success contract.
  /// For value types, all values including default values are accepted.
  /// </para>
  /// <para>
  /// Performance characteristics: This method has zero allocation overhead and provides
  /// optimal performance for success scenarios. The validation is performed only for reference types.
  /// </para>
  /// <para>
  /// Usage patterns: Prefer this method over direct constructor usage for consistent error handling
  /// and better IDE support through type inference.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Basic value types
  /// var intResult = Result.Success(42);
  /// var boolResult = Result.Success(true);
  /// var dateResult = Result.Success(DateTime.Now);
  /// 
  /// // Reference types
  /// var stringResult = Result.Success("Hello, World!");
  /// var userResult = Result.Success(new User { Id = 1, Name = "John" });
  /// var listResult = Result.Success(new List&lt;int&gt; { 1, 2, 3 });
  /// 
  /// // Nullable value types
  /// int? nullableInt = 42;
  /// var nullableResult = Result.Success(nullableInt);
  /// 
  /// // Chaining with other operations
  /// var processingResult = Result.Success(rawData)
  ///   .Map(data => ProcessData(data))
  ///   .Bind(processed => ValidateProcessed(processed))
  ///   .Tap(result => _logger.LogInformation("Processing completed: {Result}", result));
  /// 
  /// // Service method return
  /// public Result&lt;Customer&gt; FindCustomer(int customerId) {
  ///   var customer = _repository.Find(customerId);
  ///   return customer != null 
  ///     ? Result.Success(customer)
  ///     : Result.Failure&lt;Customer&gt;(ResultError.NotFound("CUSTOMER_NOT_FOUND", $"Customer {customerId} not found"));
  /// }
  /// </code>
  /// </example>
  public static Result<T> Success<T>(T value) => new(value);

  /// <summary>
  /// Creates a failed result with the specified error information, providing detailed failure context and type categorization.
  /// </summary>
  /// <typeparam name="T">The type that the result would contain on success.</typeparam>
  /// <param name="error">The error describing the failure with type, code, message, and optional additional context.</param>
  /// <returns>A failed Result containing the specified error information.</returns>
  /// <exception cref="ArgumentNullException">Thrown when error is null, ensuring error information is always available.</exception>
  /// <remarks>
  /// <para>
  /// This factory method creates a failed Result instance with comprehensive error information.
  /// The error parameter should contain sufficient detail for debugging, logging, and user communication.
  /// </para>
  /// <para>
  /// Error categorization: Use appropriate ErrorType values to enable proper HTTP status code mapping
  /// and consistent error handling across different layers of the application.
  /// </para>
  /// <para>
  /// Performance characteristics: Failure results carry error metadata but avoid exception stack traces
  /// unless explicitly included, providing better performance than traditional exception handling.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Typed error creation with factory methods
  /// var notFoundResult = Result.Failure&lt;User&gt;(
  ///   ResultError.NotFound("USER_NOT_FOUND", "User with ID 123 was not found"));
  /// 
  /// var validationResult = Result.Failure&lt;Order&gt;(
  ///   ResultError.Validation("INVALID_ORDER", "Order validation failed", validationErrors));
  /// 
  /// var authResult = Result.Failure&lt;UserProfile&gt;(
  ///   ResultError.Unauthorized("TOKEN_EXPIRED", "Authentication token has expired"));
  /// 
  /// // Business rule violations
  /// var businessResult = Result.Failure&lt;Transaction&gt;(
  ///   ResultError.BusinessRule("INSUFFICIENT_BALANCE", "Account balance insufficient for transaction"));
  /// 
  /// // Service integration errors
  /// var serviceResult = Result.Failure&lt;PaymentResponse&gt;(
  ///   ResultError.ServiceUnavailable("PAYMENT_API_DOWN", "Payment processing service is unavailable"));
  /// 
  /// // Custom error with exception context
  /// var dbResult = Result.Failure&lt;Customer&gt;(new ResultError(
  ///   ErrorType.Database, 
  ///   "DB_CONNECTION_ERROR", 
  ///   "Database connection failed during customer lookup",
  ///   sqlException));
  /// 
  /// // Conditional failure in service methods
  /// public Result&lt;Product&gt; GetProduct(int productId) {
  ///   if (productId &lt;= 0) {
  ///     return Result.Failure&lt;Product&gt;(
  ///       ResultError.Validation("INVALID_PRODUCT_ID", "Product ID must be positive"));
  ///   }
  /// 
  ///   var product = _repository.Find(productId);
  ///   return product != null 
  ///     ? Result.Success(product)
  ///     : Result.Failure&lt;Product&gt;(
  ///         ResultError.NotFound("PRODUCT_NOT_FOUND", $"Product {productId} not found"));
  /// }
  /// </code>
  /// </example>
  public static Result<T> Failure<T>(ResultError error) => new(error);

  /// <summary>
  /// Creates a failed result with a generic error message, providing a convenient overload for simple failure scenarios.
  /// </summary>
  /// <typeparam name="T">The type that the result would contain on success.</typeparam>
  /// <param name="message">The error message describing the failure reason.</param>
  /// <returns>A failed Result containing a generic error with the specified message.</returns>
  /// <exception cref="ArgumentException">Thrown when message is null, empty, or contains only whitespace.</exception>
  /// <remarks>
  /// <para>
  /// This convenience method creates a generic error with the provided message. For production code,
  /// prefer using the typed error factory methods (ResultError.NotFound, ResultError.Validation, etc.)
  /// to enable proper error categorization and HTTP status code mapping.
  /// </para>
  /// <para>
  /// Use cases: Suitable for prototyping, testing, or simple scenarios where detailed error categorization
  /// is not required. For production applications, consider migrating to more specific error types.
  /// </para>
  /// <para>
  /// Error classification: All errors created by this method have ErrorType.Generic and code "GENERIC",
  /// which maps to HTTP 500 Internal Server Error in API scenarios.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Simple failure scenarios
  /// var parseResult = Result.Failure&lt;int&gt;("Unable to parse input as integer");
  /// var configResult = Result.Failure&lt;Configuration&gt;("Configuration file not found");
  /// 
  /// // Conditional failures in validation
  /// public Result&lt;User&gt; CreateUser(string email, string name) {
  ///   if (string.IsNullOrWhiteSpace(email)) {
  ///     return Result.Failure&lt;User&gt;("Email address is required");
  ///   }
  /// 
  ///   if (string.IsNullOrWhiteSpace(name)) {
  ///     return Result.Failure&lt;User&gt;("Name is required");
  ///   }
  /// 
  ///   return Result.Success(new User { Email = email, Name = name });
  /// }
  /// 
  /// // Testing scenarios
  /// [Test]
  /// public void ValidateUser_WithEmptyName_ShouldReturnFailure() {
  ///   var result = ValidateUser("", "john@example.com");
  ///   
  ///   Assert.True(result.IsFailure);
  ///   Assert.Equal("Name cannot be empty", result.Error.Message);
  /// }
  /// 
  /// // Temporary implementations during development
  /// public Result&lt;ReportData&gt; GenerateReport(ReportRequest request) {
  ///   // TODO: Implement proper report generation with specific error types
  ///   return Result.Failure&lt;ReportData&gt;("Report generation not yet implemented");
  /// }
  /// </code>
  /// </example>
  public static Result<T> Failure<T>(string message) {
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    return new(new ResultError(ErrorType.Generic, "GENERIC", message));
  }

  /// <summary>
  /// Executes the specified operation and wraps the result or exception in a Result, providing safe exception boundaries for functional programming.
  /// </summary>
  /// <typeparam name="T">The type of the operation result.</typeparam>
  /// <param name="operation">The operation to execute that may throw exceptions.</param>
  /// <returns>A successful Result if the operation succeeds, or a failed Result containing exception details if an exception is thrown.</returns>
  /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
  /// <remarks>
  /// <para>
  /// This method provides a safe boundary between exception-based code and Result-based functional programming.
  /// It catches all exceptions to maintain the Result contract, converting them to structured error information.
  /// </para>
  /// <para>
  /// Exception handling strategy: All exceptions are caught and converted to ResultError instances with
  /// ErrorType.Generic and code "EXCEPTION". The original exception is preserved in the Exception property
  /// for debugging and logging purposes.
  /// </para>
  /// <para>
  /// Performance considerations: Success cases have minimal overhead. Exception cases avoid stack trace
  /// overhead while preserving essential debugging information. Use this method at service boundaries
  /// to convert exception-based APIs to Result-based patterns.
  /// </para>
  /// <para>
  /// Thread safety: The operation is executed synchronously on the calling thread. For async operations,
  /// use TryAsync instead.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Parsing operations
  /// var parseResult = Result.Try(() => int.Parse("42")); // Success(42)
  /// var invalidParse = Result.Try(() => int.Parse("invalid")); // Failure with FormatException
  /// 
  /// // File operations
  /// var fileResult = Result.Try(() => File.ReadAllText("config.json"));
  /// if (fileResult.IsSuccess) {
  ///   var content = fileResult.Value;
  ///   // Process file content
  /// }
  /// 
  /// // Database operations
  /// var dbResult = Result.Try(() => {
  ///   using var connection = new SqlConnection(connectionString);
  ///   connection.Open();
  ///   using var command = new SqlCommand("SELECT COUNT(*) FROM Users", connection);
  ///   return (int)command.ExecuteScalar();
  /// });
  /// 
  /// // JSON serialization
  /// var serializeResult = Result.Try(() => JsonSerializer.Serialize(complexObject));
  /// var deserializeResult = Result.Try(() => JsonSerializer.Deserialize&lt;User&gt;(jsonString));
  /// 
  /// // Service integration with external APIs
  /// public class ExternalApiClient {
  ///   public Result&lt;ApiResponse&gt; CallExternalService(ApiRequest request) {
  ///     return Result.Try(() => {
  ///       using var client = new HttpClient();
  ///       var json = JsonSerializer.Serialize(request);
  ///       var content = new StringContent(json, Encoding.UTF8, "application/json");
  ///       
  ///       var response = client.PostAsync("https://api.external.com/endpoint", content).Result;
  ///       response.EnsureSuccessStatusCode();
  ///       
  ///       var responseJson = response.Content.ReadAsStringAsync().Result;
  ///       return JsonSerializer.Deserialize&lt;ApiResponse&gt;(responseJson);
  ///     });
  ///   }
  /// }
  /// 
  /// // Chaining with other Result operations
  /// var processingResult = Result.Try(() => LoadRawData())
  ///   .Bind(data => ValidateData(data))
  ///   .Map(validData => ProcessData(validData))
  ///   .TapError(error => _logger.LogError("Processing failed: {Error}", error));
  /// </code>
  /// </example>
  [SuppressMessage(
    "Major Code Smell",
    "S2221:\"Exception\" should not be caught when not required by called methods",
    Justification = "Result.Try pattern intentionally catches all exceptions to convert them to Result failures, providing safe boundaries between exception-based and Result-based code")]
  public static Result<T> Try<T>(Func<T> operation) {
    ArgumentNullException.ThrowIfNull(operation);

    try {
      return Success(operation());
    } catch (Exception ex) {
      return Failure<T>(new ResultError(ErrorType.Generic, "EXCEPTION", ex.Message, ex));
    }
  }

  /// <summary>
  /// Executes the specified async operation and wraps the result or exception in a Result, providing safe exception boundaries for asynchronous functional programming.
  /// </summary>
  /// <typeparam name="T">The type of the operation result.</typeparam>
  /// <param name="operation">The async operation to execute that may throw exceptions.</param>
  /// <returns>A task that represents the async operation, containing a successful Result if the operation succeeds, or a failed Result if an exception is thrown.</returns>
  /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
  /// <remarks>
  /// <para>
  /// This method provides async-aware safe boundaries between exception-based code and Result-based functional programming.
  /// It properly handles async exceptions and maintains the Result contract across async boundaries.
  /// </para>
  /// <para>
  /// Exception handling strategy: All exceptions, including async exceptions, are caught and converted to ResultError instances.
  /// Cancellation exceptions are preserved with their original type for proper cancellation handling.
  /// </para>
  /// <para>
  /// Performance considerations: Uses ConfigureAwait(false) to avoid deadlocks and improve performance in library code.
  /// Success cases have minimal async overhead. Exception cases preserve debugging information efficiently.
  /// </para>
  /// <para>
  /// Thread safety and cancellation: The operation executes on the appropriate async context. For cancellation support,
  /// pass CancellationToken through the operation closure or use dedicated overloads.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Async file operations
  /// var fileResult = await Result.TryAsync(() => File.ReadAllTextAsync("config.json"));
  /// var writeResult = await Result.TryAsync(() => File.WriteAllTextAsync("output.txt", content));
  /// 
  /// // HTTP client operations
  /// var httpResult = await Result.TryAsync(async () => {
  ///   using var client = new HttpClient();
  ///   var response = await client.GetAsync("https://api.example.com/data");
  ///   response.EnsureSuccessStatusCode();
  ///   return await response.Content.ReadAsStringAsync();
  /// });
  /// 
  /// // Database operations with Entity Framework
  /// var dbResult = await Result.TryAsync(async () => {
  ///   return await _context.Users
  ///     .Where(u => u.IsActive)
  ///     .ToListAsync();
  /// });
  /// 
  /// // External API calls with timeout
  /// var apiResult = await Result.TryAsync(async () => {
  ///   using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
  ///   using var client = new HttpClient();
  ///   
  ///   var response = await client.GetAsync("https://slow-api.com/data", cts.Token);
  ///   return await response.Content.ReadFromJsonAsync&lt;ApiResponse&gt;(cts.Token);
  /// });
  /// 
  /// // Service layer with multiple async operations
  /// public class UserService {
  ///   public async Task&lt;Result&lt;UserProfile&gt;&gt; CreateUserProfileAsync(CreateUserRequest request) {
  ///     return await Result.TryAsync(async () => {
  ///       // Validate user doesn't exist
  ///       var existingUser = await _userRepository.FindByEmailAsync(request.Email);
  ///       if (existingUser != null) {
  ///         throw new InvalidOperationException("User already exists");
  ///       }
  /// 
  ///       // Create user
  ///       var user = new User { Email = request.Email, Name = request.Name };
  ///       await _userRepository.CreateAsync(user);
  /// 
  ///       // Create profile
  ///       var profile = new UserProfile { UserId = user.Id, Bio = request.Bio };
  ///       await _profileRepository.CreateAsync(profile);
  /// 
  ///       return profile;
  ///     });
  ///   }
  /// }
  /// 
  /// // Chaining async operations
  /// var result = await Result.TryAsync(() => LoadUserDataAsync(userId))
  ///   .ContinueWith(async task => {
  ///     var loadResult = task.Result;
  ///     if (loadResult.IsFailure) return loadResult;
  ///     
  ///     return await Result.TryAsync(() => EnrichUserDataAsync(loadResult.Value));
  ///   })
  ///   .Unwrap();
  /// </code>
  /// </example>
  [SuppressMessage(
    "Major Code Smell",
    "S2221:\"Exception\" should not be caught when not required by called methods",
    Justification = "Result.TryAsync pattern intentionally catches all exceptions to convert them to Result failures, providing safe boundaries between exception-based and Result-based async code")]
  public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation) {
    ArgumentNullException.ThrowIfNull(operation);

    try {
      var result = await operation().ConfigureAwait(false);
      return Success(result);
    } catch (Exception ex) {
      return Failure<T>(new ResultError(ErrorType.Generic, "EXCEPTION", ex.Message, ex));
    }
  }

  /// <summary>
  /// Combines multiple results into a single result containing an array of values using fail-fast semantics for efficient batch processing.
  /// </summary>
  /// <typeparam name="T">The type of the result values.</typeparam>
  /// <param name="results">The results to combine into a single array result.</param>
  /// <returns>A successful Result containing all values if all inputs are successful, or the first failure encountered.</returns>
  /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
  /// <remarks>
  /// <para>
  /// This method implements fail-fast semantics: if any input result is a failure, the combination stops
  /// and returns the first failure encountered. This provides efficient short-circuiting for batch operations
  /// where all inputs must be successful for the operation to proceed.
  /// </para>
  /// <para>
  /// Performance characteristics: The method iterates through results in order and stops at the first failure.
  /// Memory allocation is minimized by pre-sizing the result array. For large collections, consider using
  /// the IEnumerable overload for better memory efficiency.
  /// </para>
  /// <para>
  /// Use cases: Ideal for validating multiple inputs, combining service call results, or batch processing
  /// operations where partial success is not acceptable.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Combining validation results
  /// var nameValidation = ValidateName(user.Name);
  /// var emailValidation = ValidateEmail(user.Email);
  /// var phoneValidation = ValidatePhone(user.Phone);
  /// 
  /// var allValidations = Result.Combine(nameValidation, emailValidation, phoneValidation);
  /// if (allValidations.IsSuccess) {
  ///   // All validations passed, proceed with user creation
  ///   var validatedData = allValidations.Value; // [name, email, phone]
  /// }
  /// 
  /// // Combining service calls
  /// var userResult = await GetUserAsync(userId);
  /// var ordersResult = await GetUserOrdersAsync(userId);
  /// var preferencesResult = await GetUserPreferencesAsync(userId);
  /// 
  /// var combinedData = Result.Combine(userResult, ordersResult, preferencesResult);
  /// if (combinedData.IsSuccess) {
  ///   var (user, orders, preferences) = combinedData.Value;
  ///   return new UserDashboard { User = user, Orders = orders, Preferences = preferences };
  /// }
  /// 
  /// // Batch processing with early exit
  /// public Result&lt;ProcessedItem[]&gt; ProcessBatch(Item[] items) {
  ///   var results = items.Select(item => ProcessSingleItem(item)).ToArray();
  ///   return Result.Combine(results); // Fails fast if any item processing fails
  /// }
  /// 
  /// // Configuration validation
  /// public class ConfigurationValidator {
  ///   public Result&lt;ValidatedConfig&gt; ValidateConfiguration(RawConfig config) {
  ///     var dbValidation = ValidateDatabaseConfig(config.Database);
  ///     var authValidation = ValidateAuthConfig(config.Auth);
  ///     var loggingValidation = ValidateLoggingConfig(config.Logging);
  /// 
  ///     var allConfigs = Result.Combine(dbValidation, authValidation, loggingValidation);
  ///     return allConfigs.Map(configs => new ValidatedConfig {
  ///       Database = configs[0],
  ///       Auth = configs[1],
  ///       Logging = configs[2]
  ///     });
  ///   }
  /// }
  /// 
  /// // Parallel processing with combination
  /// public async Task&lt;Result&lt;Report[]&gt;&gt; GenerateReportsAsync(ReportRequest[] requests) {
  ///   var tasks = requests.Select(async request => 
  ///     await Result.TryAsync(() => GenerateSingleReportAsync(request))).ToArray();
  /// 
  ///   var results = await Task.WhenAll(tasks);
  ///   return Result.Combine(results); // Combine all parallel results
  /// }
  /// </code>
  /// </example>
  public static Result<T[]> Combine<T>(params Result<T>[] results) {
    ArgumentNullException.ThrowIfNull(results);

    var values = new T[results.Length];

    for (var i = 0; i < results.Length; i++) {
      if (results[i].IsFailure) {
        return Failure<T[]>(results[i].Error);
      }
      values[i] = results[i].Value;
    }

    return Success(values);
  }

  /// <summary>
  /// Combines multiple results from an enumerable into a single result containing a list of values using fail-fast semantics for memory-efficient batch processing.
  /// </summary>
  /// <typeparam name="T">The type of the result values.</typeparam>
  /// <param name="results">The enumerable of results to combine into a single list result.</param>
  /// <returns>A successful Result containing all values if all inputs are successful, or the first failure encountered.</returns>
  /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
  /// <remarks>
  /// <para>
  /// This method provides memory-efficient combination for large collections by using enumerable iteration
  /// instead of pre-allocating arrays. It implements fail-fast semantics: iteration stops at the first failure,
  /// making it suitable for processing large datasets where early termination is beneficial.
  /// </para>
  /// <para>
  /// Performance characteristics: Uses List&lt;T&gt; for dynamic growth, avoiding upfront memory allocation.
  /// Enumeration is lazy until the first failure is encountered. For small, known-size collections,
  /// the array overload may provide better performance.
  /// </para>
  /// <para>
  /// Memory considerations: The result list grows dynamically and will contain all successful values
  /// up to the point of failure. This makes it suitable for streaming scenarios or unknown collection sizes.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Processing large collections
  /// public Result&lt;List&lt;ProcessedRecord&gt;&gt; ProcessRecords(IEnumerable&lt;RawRecord&gt; records) {
  ///   var processedResults = records.Select(record => ProcessSingleRecord(record));
  ///   return Result.Combine(processedResults); // Memory-efficient for large collections
  /// }
  /// 
  /// // Streaming validation
  /// public Result&lt;List&lt;ValidUser&gt;&gt; ValidateUsers(IAsyncEnumerable&lt;User&gt; users) {
  ///   var validationResults = users.Select(user => ValidateUser(user));
  ///   return Result.Combine(validationResults);
  /// }
  /// 
  /// // LINQ integration with Result combination
  /// var fileResults = Directory.EnumerateFiles("*.json")
  ///   .Select(filePath => Result.Try(() => File.ReadAllText(filePath)))
  ///   .Select(contentResult => contentResult.Bind(content => 
  ///     Result.Try(() => JsonSerializer.Deserialize&lt;ConfigSection&gt;(content))));
  /// 
  /// var combinedConfigs = Result.Combine(fileResults);
  /// if (combinedConfigs.IsSuccess) {
  ///   // All configuration files were successfully loaded and parsed
  ///   var allConfigs = combinedConfigs.Value;
  /// }
  /// 
  /// // Database batch operations
  /// public class BatchProcessor {
  ///   public async Task&lt;Result&lt;List&lt;SavedEntity&gt;&gt;&gt; SaveBatchAsync(IEnumerable&lt;Entity&gt; entities) {
  ///     var saveResults = entities.Select(async entity => 
  ///       await Result.TryAsync(() => _repository.SaveAsync(entity)));
  /// 
  ///     var completedResults = await Task.WhenAll(saveResults);
  ///     return Result.Combine(completedResults);
  ///   }
  /// }
  /// 
  /// // Functional pipeline with combination
  /// var processingPipeline = inputData
  ///   .Select(item => Result.Try(() => TransformItem(item)))
  ///   .Select(result => result.Bind(item => ValidateTransformedItem(item)))
  ///   .Select(result => result.Map(item => EnrichItem(item)));
  /// 
  /// var finalResult = Result.Combine(processingPipeline)
  ///   .Map(enrichedItems => new ProcessingReport {
  ///     Items = enrichedItems,
  ///     ProcessedCount = enrichedItems.Count,
  ///     ProcessedAt = DateTime.UtcNow
  ///   });
  /// 
  /// // Error handling with early termination
  /// public Result&lt;List&lt;ImportedRecord&gt;&gt; ImportRecords(IEnumerable&lt;string&gt; csvLines) {
  ///   var parseResults = csvLines
  ///     .Skip(1) // Skip header
  ///     .Select((line, index) => Result.Try(() => ParseCsvLine(line))
  ///       .TapError(error => _logger.LogError("Failed to parse line {LineNumber}: {Error}", index + 2, error)));
  /// 
  ///   return Result.Combine(parseResults); // Stops at first parsing error
  /// }
  /// </code>
  /// </example>
  public static Result<List<T>> Combine<T>(IEnumerable<Result<T>> results) {
    ArgumentNullException.ThrowIfNull(results);

    var values = new List<T>();

    foreach (var result in results) {
      if (result.IsFailure) {
        return Failure<List<T>>(result.Error);
      }
      values.Add(result.Value);
    }

    return Success(values);
  }
}
