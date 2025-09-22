using System.Text;

namespace NSI.Core.Results;

/// <summary>
/// Provides high-performance helper extension methods to format exceptions into concise, HTTP-friendly text blocks with controlled verbosity.
/// </summary>
/// <remarks>
/// <para>
/// This formatter addresses common issues with exception logging and HTTP problem details
/// by providing controlled exception chain traversal and stack trace sanitization. It ensures
/// consistent formatting across the application while maintaining security and readability.
/// </para>
/// <para>
/// Key features and capabilities:
/// <list type="bullet">
///   <item><description>Controlled nesting depth: Prevents excessive output from deeply nested exception chains</description></item>
///   <item><description>Stack trace sanitization: Removes absolute file paths and extracts only relevant information</description></item>
///   <item><description>HTTP-friendly format: Produces compact output suitable for API responses and logging</description></item>
///   <item><description>Security-aware: Removes sensitive path information while preserving debugging context</description></item>
///   <item><description>Performance optimized: Uses StringBuilder and efficient string operations</description></item>
/// </list>
/// </para>
/// <para>
/// Usage scenarios include HTTP problem details formatting, structured logging output,
/// API error responses, and debugging information sanitization for production environments.
/// </para>
/// <para>
/// Thread safety: All methods are static and stateless, making them thread-safe for concurrent use.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic exception formatting for HTTP responses
/// try {
///   await ProcessDataAsync();
/// }
/// catch (Exception ex) {
///   var formatted = ex.FormatForHttp();
///   return Problem(
///     detail: formatted,
///     status: 500,
///     title: "Internal Server Error"
///   );
/// }
/// 
/// // Structured logging with formatted exceptions
/// public class OrderService {
///   private readonly ILogger&lt;OrderService&gt; _logger;
///   
///   public async Task&lt;Result&lt;Order&gt;&gt; ProcessOrderAsync(CreateOrderRequest request) {
///     try {
///       var order = await CreateOrderAsync(request);
///       return Result.Success(order);
///     }
///     catch (Exception ex) {
///       var formattedException = ex.FormatForHttp();
///       _logger.LogError("Order processing failed: {Exception}", formattedException);
///       
///       return Result.Failure&lt;Order&gt;(new ResultError(
///         ErrorType.Generic, 
///         "ORDER_PROCESSING_FAILED", 
///         "Order processing encountered an unexpected error",
///         ex
///       ));
///     }
///   }
/// }
/// 
/// // Integration with Result pattern
/// public static class ResultErrorExtensions {
///   public static ResultError WithFormattedException(this ResultError error) {
///     if (error.Exception != null) {
///       var formatted = error.Exception.FormatForHttp();
///       return new ResultError(error.Type, error.Code, formatted, error.Exception);
///     }
///     return error;
///   }
/// }
/// 
/// // Example output format:
/// // [ArgumentException] Value cannot be null or empty
/// // Stack: ProcessOrder(CreateOrderRequest request) in OrderService.cs:line 42
/// //   [ValidationException] Order validation failed
/// //     [InvalidOperationException] Customer not found
/// </code>
/// </example>
/// <seealso cref="Exception"/>
/// <seealso cref="ResultError"/>
/// <seealso cref="ErrorType"/>
public static class ExceptionFormatter {

  private const int StackPrefixLength = 3;
  private const int MaxExceptionNestingLevel = 5;

  /// <summary>
  /// Formats an <see cref="Exception"/> and its inner exception chain into a compact, single string suitable for HTTP problem details or structured logging.
  /// </summary>
  /// <param name="ex">The source exception to format. Cannot be null.</param>
  /// <returns>
  /// A formatted multi-line string containing the exception chain with type names, messages, and sanitized stack trace information.
  /// The output is trimmed and ready for inclusion in HTTP responses or log entries.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
  /// <remarks>
  /// <para>
  /// This method provides controlled formatting of exception chains with the following characteristics:
  /// <list type="bullet">
  ///   <item><description>Depth limiting: Traverses up to 5 levels of inner exceptions to prevent excessive output</description></item>
  ///   <item><description>Stack trace extraction: Includes only the first (most relevant) stack frame from the root exception</description></item>
  ///   <item><description>Path sanitization: Removes absolute file paths, keeping only filenames for security and portability</description></item>
  ///   <item><description>Indentation structure: Uses 2-space indentation per nesting level for readability</description></item>
  ///   <item><description>Consistent formatting: Each exception shows [Type] Message format with optional stack information</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The formatted output is designed to be human-readable while remaining compact enough for
  /// HTTP problem details, API responses, and structured logging scenarios. It balances
  /// debugging information with security considerations by removing sensitive path details.
  /// </para>
  /// <para>
  /// Performance considerations: Uses StringBuilder for efficient string building and processes
  /// only the first stack frame to minimize overhead. The method is optimized for production
  /// logging scenarios where performance matters.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Example with nested exceptions
  /// try {
  ///   // This throws ArgumentException -> ValidationException -> InvalidOperationException
  ///   ValidateAndProcessData(null);
  /// }
  /// catch (Exception ex) {
  ///   var formatted = ex.FormatForHttp();
  ///   Console.WriteLine(formatted);
  ///   
  ///   // Output:
  ///   // [ArgumentException] Value cannot be null or empty (Parameter 'data')
  ///   // Stack: ValidateAndProcessData(String data) in DataProcessor.cs:line 15
  ///   //   [ValidationException] Data validation failed
  ///   //     [InvalidOperationException] Processing state is invalid
  /// }
  /// 
  /// // Example with HTTP Problem Details
  /// app.UseExceptionHandler(builder => {
  ///   builder.Run(async context => {
  ///     var exception = context.Features.Get&lt;IExceptionHandlerFeature&gt;()?.Error;
  ///     if (exception != null) {
  ///       var problemDetails = new ProblemDetails {
  ///         Status = 500,
  ///         Title = "An error occurred",
  ///         Detail = exception.FormatForHttp(),
  ///         Instance = context.Request.Path
  ///       };
  ///       
  ///       await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
  ///     }
  ///   });
  /// });
  /// 
  /// // Example with custom exception handling
  /// public class ApiController {
  ///   protected IActionResult HandleException(Exception ex, string operation) {
  ///     var formatted = ex.FormatForHttp();
  ///     _logger.LogError("Operation {Operation} failed: {Exception}", operation, formatted);
  ///     
  ///     return ex switch {
  ///       ArgumentException => BadRequest(formatted),
  ///       InvalidOperationException => Conflict(formatted),
  ///       _ => Problem(detail: formatted, status: 500)
  ///     };
  ///   }
  /// }
  /// </code>
  /// </example>
  public static string FormatForHttp(this Exception ex) {
    ArgumentNullException.ThrowIfNull(ex);

    var sb = new StringBuilder();
    var currentException = ex;

    for (var depth = 0; currentException != null && depth < MaxExceptionNestingLevel; depth++) {
      var indent = new string(' ', depth * 2);

      // Format: [ExceptionType] Message
      sb.Append(indent)
        .Append('[').Append(currentException.GetType().Name).Append("] ")
        .AppendLine(currentException.Message ?? string.Empty);

      // Include stack trace only for the root exception (depth 0)
      if (depth == 0 && !string.IsNullOrEmpty(currentException.StackTrace)) {
        var relevantStack = ExtractRelevantStackTrace(currentException.StackTrace);
        if (!string.IsNullOrEmpty(relevantStack)) {
          sb.Append(indent)
            .Append("Stack: ")
            .AppendLine(relevantStack);
        }
      }

      currentException = currentException.InnerException;
    }

    return sb.ToString().TrimEnd();
  }

  /// <summary>
  /// Extracts the most relevant stack trace line from a complete stack trace string.
  /// </summary>
  /// <param name="stackTrace">The complete stack trace string from an exception.</param>
  /// <returns>
  /// A single line containing the first stack frame with "at " prefix removed and file paths sanitized,
  /// or null if no relevant information can be extracted.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method processes stack traces to extract only the most relevant debugging information
  /// while removing noise and security-sensitive details. It performs the following operations:
  /// <list type="bullet">
  ///   <item><description>Extracts only the first line of the stack trace (most relevant frame)</description></item>
  ///   <item><description>Removes the "at " prefix common in .NET stack traces</description></item>
  ///   <item><description>Delegates path sanitization to CleanFilePath for security</description></item>
  ///   <item><description>Handles edge cases like empty or malformed stack traces</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// The goal is to provide enough information for debugging while keeping the output
  /// concise and suitable for production logging and HTTP responses.
  /// </para>
  /// </remarks>
  private static string? ExtractRelevantStackTrace(string stackTrace) {
    // Extract only the first line (most relevant stack frame)
    var firstNewLine = stackTrace.IndexOf('\n', StringComparison.Ordinal);
    var firstLine = (firstNewLine >= 0 ? stackTrace[..firstNewLine] : stackTrace).Trim();

    // Remove the "at " prefix that .NET adds to stack trace lines
    if (firstLine.StartsWith("at ", StringComparison.Ordinal) && firstLine.Length > StackPrefixLength) {
      firstLine = firstLine[StackPrefixLength..];
    }

    return SanitizeFilePath(firstLine);
  }

  /// <summary>
  /// Sanitizes file paths in stack trace lines by removing directory information and keeping only filenames.
  /// </summary>
  /// <param name="stackTraceLine">A single stack trace line that may contain file path information.</param>
  /// <returns>
  /// The stack trace line with absolute file paths replaced by filenames only, or the original line if no path information is found.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method addresses security and portability concerns by removing sensitive path information
  /// from stack traces while preserving debugging context. It performs the following transformations:
  /// <list type="bullet">
  ///   <item><description>Locates file path information after " in " token in stack trace lines</description></item>
  ///   <item><description>Extracts method and parameter information (left of " in ")</description></item>
  ///   <item><description>Replaces full file paths with just the filename (right of last path separator)</description></item>
  ///   <item><description>Handles both Windows (\) and Unix (/) path separators</description></item>
  ///   <item><description>Preserves line number information when present</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example transformation:
  /// "MyMethod(int param) in C:\Projects\MyApp\Services\UserService.cs:line 42"
  /// becomes:
  /// "MyMethod(int param) in UserService.cs:line 42"
  /// </para>
  /// <para>
  /// This sanitization helps prevent information leakage about server directory structures
  /// while maintaining the essential debugging information needed for troubleshooting.
  /// </para>
  /// </remarks>
  private static string? SanitizeFilePath(string? stackTraceLine) {
    if (string.IsNullOrEmpty(stackTraceLine)) {
      return stackTraceLine;
    }

    // Look for " in " token that precedes file path information in .NET stack traces
    const string InToken = " in ";
    var inIndex = stackTraceLine.LastIndexOf(InToken, StringComparison.Ordinal);
    if (inIndex == -1) {
      return stackTraceLine; // No file path information found
    }

    // Split into method part and path part
    var methodPart = stackTraceLine[..inIndex];
    var pathPart = stackTraceLine[(inIndex + InToken.Length)..];

    // Find the last path separator (Windows \ or Unix /)
    var lastBackslash = pathPart.LastIndexOf('\\');
    var lastSlash = pathPart.LastIndexOf('/');
    var lastSeparatorIndex = Math.Max(lastBackslash, lastSlash);

    // Extract filename if a path separator was found
    if (lastSeparatorIndex != -1 && lastSeparatorIndex + 1 < pathPart.Length) {
      var fileName = pathPart[(lastSeparatorIndex + 1)..];
      return $"{methodPart}{InToken}{fileName}";
    }

    // No path separator found, return original line
    return stackTraceLine;
  }
}
