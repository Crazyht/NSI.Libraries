using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSI.Core.Results;

namespace NSI.AspNetCore.Results {
  /// <summary>
  /// Provides extension methods for converting Result{T} objects to HTTP responses in minimal APIs.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These extensions follow RFC 7807 (Problem Details for HTTP APIs) when converting
  /// failure results to HTTP responses. They provide a consistent way to handle
  /// both successful and failed operations in minimal APIs.
  /// </para>
  /// <para>
  /// Supported conversions:
  /// <list type="bullet">
  ///   <item><description>Success results to appropriate 2xx responses</description></item>
  ///   <item><description>Failure results to RFC 7807 ProblemDetails responses</description></item>
  ///   <item><description>Validation errors to structured error responses</description></item>
  /// </list>
  /// </para>
  /// </remarks>
  public static class ResultHttpExtensions {
    // RFC 7807 Problem Type URIs - Constants to avoid hardcoded strings
    private static readonly Uri BaseUri = new("https://tools.ietf.org/html/rfc7235");
    private static readonly Uri BadRequestUri = new(BaseUri, "#section-6.5.1");
    private static readonly Uri UnauthorizedUri = new(BaseUri, "#section-3.1");
    private static readonly Uri ForbiddenUri = new(BaseUri, "#section-6.5.3");
    private static readonly Uri NotFoundUri = new(BaseUri, "#section-6.5.4");
    private static readonly Uri ConflictUri = new(BaseUri, "#section-6.5.8");
    private static readonly Uri UnprocessableEntityUri = new(BaseUri, "#section-11.2");
    private static readonly Uri TooManyRequestsUri = new(BaseUri, "#section-4");
    private static readonly Uri ServiceUnavailableUri = new(BaseUri, "#section-6.6.4");
    private static readonly Uri RequestTimeoutUri = new(BaseUri, "#section-6.5.7");
    private static readonly Uri InternalServerErrorUri = new(BaseUri, "#section-6.6.1");
    private static readonly Uri BadGatewayUri = new(BaseUri, "#section-6.6.1");

    /// <summary>
    /// Converts a Result{T} to an HTTP response with appropriate status codes.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">The status code to return on success (default: 200 OK).</param>
    /// <returns>An IResult representing the HTTP response.</returns>
    /// <remarks>
    /// <para>
    /// Success responses return the value directly with the specified status code.
    /// Failure responses are converted to RFC 7807 ProblemDetails with appropriate status codes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapGet("/users/{id}", async (int id, IUserService userService) => {
    ///   var result = await userService.GetUserByIdAsync(id);
    ///   return result.ToHttpResponse();
    /// });
    /// </code>
    /// </example>
    public static IResult ToHttpResponse<T>(this Result<T> result, int successStatusCode = 200)
      => result.IsSuccess
        ? Microsoft.AspNetCore.Http.Results.Json(result.Value, statusCode: successStatusCode)
        : result.Error.ToProblemDetails();

    /// <summary>
    /// Converts a Result{T} to an HTTP response for creation operations (201 Created).
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="locationUri">The location URI for the created resource.</param>
    /// <returns>An IResult representing the HTTP response.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/users", async (CreateUserRequest request, IUserService userService) => {
    ///   var result = await userService.CreateUserAsync(request);
    ///   return result.ToCreatedResponse(new Uri($"/users/{result.Value?.Id}", UriKind.Relative));
    /// });
    /// </code>
    /// </example>
    public static IResult ToCreatedResponse<T>(this Result<T> result, Uri? locationUri = null) {
      if (result.IsSuccess) {
        return Microsoft.AspNetCore.Http.Results.Created(
          locationUri ?? new Uri(string.Empty, UriKind.Relative),
          result.Value);
      }

      return result.Error.ToProblemDetails();
    }

    /// <summary>
    /// Converts a Result{T} to an HTTP response for creation operations (201 Created).
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The location string for the created resource.</param>
    /// <returns>An IResult representing the HTTP response.</returns>
    /// <exception cref="ArgumentException">Thrown when location is not a valid URI format.</exception>
    /// <example>
    /// <code>
    /// app.MapPost("/users", async (CreateUserRequest request, IUserService userService) => {
    ///   var result = await userService.CreateUserAsync(request);
    ///   return result.ToCreatedResponse($"/users/{result.Value?.Id}");
    /// });
    /// </code>
    /// </example>
    public static IResult ToCreatedResponse<T>(this Result<T> result, string? location = null) {
      if (result.IsSuccess) {
        var locationUri = string.IsNullOrEmpty(location)
          ? new Uri(string.Empty, UriKind.Relative)
          : new Uri(location, UriKind.RelativeOrAbsolute);
        return Microsoft.AspNetCore.Http.Results.Created(locationUri, result.Value);
      }

      return result.Error.ToProblemDetails();
    }

    /// <summary>
    /// Converts a Result{T} to an HTTP response for update operations.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IResult representing the HTTP response (200 OK or ProblemDetails).</returns>
    /// <example>
    /// <code>
    /// app.MapPut("/users/{id}", async (int id, UpdateUserRequest request, IUserService userService) => {
    ///   var result = await userService.UpdateUserAsync(id, request);
    ///   return result.ToUpdatedResponse();
    /// });
    /// </code>
    /// </example>
    public static IResult ToUpdatedResponse<T>(this Result<T> result)
      => result.ToHttpResponse(successStatusCode: 200);

    /// <summary>
    /// Converts a Result{T} to an HTTP response for delete operations.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An IResult representing the HTTP response (204 No Content or ProblemDetails).</returns>
    /// <example>
    /// <code>
    /// app.MapDelete("/users/{id}", async (int id, IUserService userService) => {
    ///   var result = await userService.DeleteUserAsync(id);
    ///   return result.ToDeletedResponse();
    /// });
    /// </code>
    /// </example>
    public static IResult ToDeletedResponse<T>(this Result<T> result)
      => result.IsSuccess
        ? Microsoft.AspNetCore.Http.Results.NoContent()
        : result.Error.ToProblemDetails();

    /// <summary>
    /// Converts a ResultError to an RFC 7807 ProblemDetails HTTP response.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>An IResult representing the ProblemDetails response.</returns>
    /// <example>
    /// <code>
    /// var error = ResultError.NotFound("USER_NOT_FOUND", "User with specified ID was not found");
    /// return error.ToProblemDetails();
    /// </code>
    /// </example>
    public static IResult ToProblemDetails(this ResultError error) {
      var statusCode = GetStatusCodeForErrorType(error.Type);
      var problemTypeUri = GetProblemTypeUriForErrorType(error.Type);
      var title = GetTitleForErrorType(error.Type);

      var problemDetails = new ProblemDetails {
        Type = problemTypeUri.ToString(),
        Title = title,
        Status = statusCode,
        Detail = error.Message,
        Instance = null
      };

      // Add error code as extension data
      problemDetails.Extensions["code"] = error.Code;

      // Add validation errors if present
      if (error.HasValidationErrors) {
        problemDetails.Extensions["validationErrors"] = error.ValidationErrors!
          .Select(ve => new {
            ve.PropertyName,
            ve.ErrorCode,
            ve.ErrorMessage,
            ve.ExpectedValue
          })
          .ToArray();
      }

      // Add exception details in development environment
      if (error.Exception is not null) {
        problemDetails.Extensions["exceptionType"] = error.Exception.GetType().Name;
        // Note: Only include stack trace in development environments
        // This should be controlled by environment checks in a real application
      }

      return Microsoft.AspNetCore.Http.Results.Problem(
        detail: problemDetails.Detail,
        instance: problemDetails.Instance,
        statusCode: problemDetails.Status,
        title: problemDetails.Title,
        type: problemDetails.Type,
        extensions: problemDetails.Extensions
      );
    }

    /// <summary>
    /// Maps ErrorType to appropriate HTTP status codes following REST conventions.
    /// </summary>
    /// <param name="errorType">The error type to map.</param>
    /// <returns>The corresponding HTTP status code.</returns>
    private static int GetStatusCodeForErrorType(ErrorType errorType)
      => errorType switch {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Authentication => StatusCodes.Status401Unauthorized,
        ErrorType.Authorization => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => GetExtendedStatusCodeForErrorType(errorType)
      };

    /// <summary>
    /// Maps additional ErrorType values to HTTP status codes to reduce complexity.
    /// </summary>
    /// <param name="errorType">The error type to map.</param>
    /// <returns>The corresponding HTTP status code.</returns>
    private static int GetExtendedStatusCodeForErrorType(ErrorType errorType)
      => errorType switch {
        ErrorType.RateLimit => StatusCodes.Status429TooManyRequests,
        ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorType.Timeout => StatusCodes.Status408RequestTimeout,
        ErrorType.Database => StatusCodes.Status500InternalServerError,
        ErrorType.Network => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
      };

    /// <summary>
    /// Gets the RFC 7807 problem type URI for the specified error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The problem type URI.</returns>
    private static Uri GetProblemTypeUriForErrorType(ErrorType errorType)
      => errorType switch {
        ErrorType.Validation => BadRequestUri,
        ErrorType.Authentication => UnauthorizedUri,
        ErrorType.Authorization => ForbiddenUri,
        ErrorType.NotFound => NotFoundUri,
        ErrorType.Conflict => ConflictUri,
        ErrorType.BusinessRule => UnprocessableEntityUri,
        _ => GetExtendedProblemTypeUriForErrorType(errorType)
      };

    /// <summary>
    /// Gets additional problem type URIs to reduce complexity.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The problem type URI.</returns>
    private static Uri GetExtendedProblemTypeUriForErrorType(ErrorType errorType)
      => errorType switch {
        ErrorType.RateLimit => TooManyRequestsUri,
        ErrorType.ServiceUnavailable => ServiceUnavailableUri,
        ErrorType.Timeout => RequestTimeoutUri,
        ErrorType.Database => InternalServerErrorUri,
        ErrorType.Network => BadGatewayUri,
        _ => InternalServerErrorUri
      };

    /// <summary>
    /// Gets the default title for the specified error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The default title.</returns>
    private static string GetTitleForErrorType(ErrorType errorType)
      => errorType switch {
        ErrorType.Validation => "Validation Failed",
        ErrorType.Authentication => "Authentication Required",
        ErrorType.Authorization => "Access Forbidden",
        ErrorType.NotFound => "Resource Not Found",
        ErrorType.Conflict => "Resource Conflict",
        ErrorType.BusinessRule => "Business Rule Violation",
        _ => GetExtendedTitleForErrorType(errorType)
      };

    /// <summary>
    /// Gets additional titles to reduce complexity.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The default title.</returns>
    private static string GetExtendedTitleForErrorType(ErrorType errorType)
      => errorType switch {
        ErrorType.RateLimit => "Rate Limit Exceeded",
        ErrorType.ServiceUnavailable => "Service Unavailable",
        ErrorType.Timeout => "Request Timeout",
        ErrorType.Database => "Database Error",
        ErrorType.Network => "Network Error",
        ErrorType.Generic => "An Error Occurred",
        _ => "Internal Server Error"
      };
  }
}
