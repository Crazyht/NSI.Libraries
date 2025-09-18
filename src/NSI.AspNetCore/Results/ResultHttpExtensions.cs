using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSI.Core.Results;

namespace NSI.AspNetCore.Results;

/// <summary>
/// HTTP response helpers for <see cref="Result{T}"/> integrating RFC 7807 problem details.
/// </summary>
/// <remarks>
/// <para>
/// Provides consistent conversion from the domain <see cref="Result{T}"/> pattern to minimal API
/// <see cref="IResult"/> responses. Success values map to JSON payloads (2xx codes). Failures map
/// to RFC 7807 compliant <see cref="ProblemDetails"/> with standardized status, title and type URI.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description><c>Success</c> => JSON body with configurable success status (default 200).</description></item>
///   <item><description><c>Failure</c> => ProblemDetails JSON with extensions: <c>code</c>, optional <c>validationErrors</c>.</description></item>
///   <item><description>Validation errors supply structured field errors (never flattened).</description></item>
///   <item><description>Exception details intentionally minimal (type only) – stack traces omitted.</description></item>
/// </list>
/// </para>
/// <para>Performance:
/// <list type="bullet">
///   <item><description>Zero allocations on success beyond serializer output.</description></item>
///   <item><description>Failure path creates a single <see cref="ProblemDetails"/> and small arrays for validation.</description></item>
///   <item><description>Switch-based mapping avoids dictionary lookups / boxing.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Static and stateless; safe for concurrent use.</para>
/// </remarks>
public static class ResultHttpExtensions {
  // RFC 7807 base (canonical specification)
  private static readonly Uri Rfc7807 = new("https://datatracker.ietf.org/doc/html/rfc7807");

  // Problem type anchors (re-using RFC 7235 / HTTP spec anchors where practical)
  private static readonly Uri BadRequestUri = new(Rfc7807, "#section-3.1"); // generic client error fallback
  private static readonly Uri UnauthorizedUri = new("https://datatracker.ietf.org/doc/html/rfc7235#section-3.1");
  private static readonly Uri ForbiddenUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3");
  private static readonly Uri NotFoundUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4");
  private static readonly Uri ConflictUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8");
  private static readonly Uri UnprocessableEntityUri = new("https://datatracker.ietf.org/doc/html/rfc4918#section-11.2");
  private static readonly Uri TooManyRequestsUri = new("https://datatracker.ietf.org/doc/html/rfc6585#section-4");
  private static readonly Uri ServiceUnavailableUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4");
  private static readonly Uri RequestTimeoutUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.7");
  private static readonly Uri InternalServerErrorUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1");
  private static readonly Uri BadGatewayUri = new("https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.3");

  /// <summary>Converts a <see cref="Result{T}"/> to an HTTP response.</summary>
  public static IResult ToHttpResponse<T>(this Result<T> result, int successStatusCode = 200)
    => result.IsSuccess
      ? Microsoft.AspNetCore.Http.Results.Json(result.Value, statusCode: successStatusCode)
      : result.Error.ToProblemDetails();

  /// <summary>Converts a success <see cref="Result{T}"/> to 201 Created (or failure ProblemDetails).</summary>
  public static IResult ToCreatedResponse<T>(this Result<T> result, Uri? locationUri = null) {
    if (result.IsSuccess) {
      return Microsoft.AspNetCore.Http.Results.Created(locationUri ?? new Uri(string.Empty, UriKind.Relative), result.Value);
    }
    return result.Error.ToProblemDetails();
  }

  /// <summary>Converts a success <see cref="Result{T}"/> to 201 Created using a relative/absolute path.</summary>
  public static IResult ToCreatedResponse<T>(this Result<T> result, string? location = null) {
    if (result.IsSuccess) {
      var uri = string.IsNullOrEmpty(location)
        ? new Uri(string.Empty, UriKind.Relative)
        : new Uri(location, UriKind.RelativeOrAbsolute);
      return Microsoft.AspNetCore.Http.Results.Created(uri, result.Value);
    }
    return result.Error.ToProblemDetails();
  }

  /// <summary>Converts to 200 OK (or failure ProblemDetails).</summary>
  public static IResult ToUpdatedResponse<T>(this Result<T> result)
    => result.ToHttpResponse(StatusCodes.Status200OK);

  /// <summary>Converts to 204 NoContent (or failure ProblemDetails).</summary>
  public static IResult ToDeletedResponse<T>(this Result<T> result)
    => result.IsSuccess ? Microsoft.AspNetCore.Http.Results.NoContent() : result.Error.ToProblemDetails();

  /// <summary>Creates RFC 7807 ProblemDetails response from a <see cref="ResultError"/>.</summary>
  public static IResult ToProblemDetails(this ResultError error) {
    // Map essentials in a single switch for cache friendliness
    var (status, uri, title) = Map(error.Type);

    var problem = new ProblemDetails {
      Type = uri.ToString(),
      Title = title,
      Status = status,
      Detail = error.Message,
      Instance = null
    };

    problem.Extensions["code"] = error.Code;

    if (error.HasValidationErrors && error.ValidationErrors is not null) {
      problem.Extensions["validationErrors"] = error.ValidationErrors
        .Select(ve => new {
          ve.PropertyName,
          ve.ErrorCode,
          ve.ErrorMessage,
          ve.ExpectedValue
        })
        .ToArray();
    }

    if (error.Exception is not null) {
      problem.Extensions["exceptionType"] = error.Exception.GetType().Name;
    }

    return Microsoft.AspNetCore.Http.Results.Problem(
      detail: problem.Detail,
      instance: problem.Instance,
      statusCode: problem.Status,
      title: problem.Title,
      type: problem.Type,
      extensions: problem.Extensions);
  }

  // Central mapping: returns (StatusCode, TypeUri, Title)
  private static (int Status, Uri TypeUri, string Title) Map(ErrorType type)
    => type switch {
      ErrorType.Validation => (StatusCodes.Status400BadRequest, BadRequestUri, "Validation Failed"),
      ErrorType.Authentication => (StatusCodes.Status401Unauthorized, UnauthorizedUri, "Authentication Required"),
      ErrorType.Authorization => (StatusCodes.Status403Forbidden, ForbiddenUri, "Access Forbidden"),
      ErrorType.NotFound => (StatusCodes.Status404NotFound, NotFoundUri, "Resource Not Found"),
      ErrorType.Conflict => (StatusCodes.Status409Conflict, ConflictUri, "Resource Conflict"),
      ErrorType.BusinessRule => (StatusCodes.Status422UnprocessableEntity, UnprocessableEntityUri, "Business Rule Violation"),
      _ => MapServerOrTransient(type)
    };

  private static (int, Uri, string) MapServerOrTransient(ErrorType type)
    => type switch {
      ErrorType.RateLimit => (StatusCodes.Status429TooManyRequests, TooManyRequestsUri, "Rate Limit Exceeded"),
      ErrorType.ServiceUnavailable => (StatusCodes.Status503ServiceUnavailable, ServiceUnavailableUri, "Service Unavailable"),
      ErrorType.Timeout => (StatusCodes.Status408RequestTimeout, RequestTimeoutUri, "Request Timeout"),
      ErrorType.Database => (StatusCodes.Status500InternalServerError, InternalServerErrorUri, "Database Error"),
      ErrorType.Network => (StatusCodes.Status502BadGateway, BadGatewayUri, "Network Error"),
      ErrorType.Generic => (StatusCodes.Status500InternalServerError, InternalServerErrorUri, "An Error Occurred"),
      _ => (StatusCodes.Status500InternalServerError, InternalServerErrorUri, "Internal Server Error")
    };
}
