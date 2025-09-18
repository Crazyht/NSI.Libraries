using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSI.Core.Identity;
using NSI.Domains;

namespace NSI.AspNetCore.Identity;

/// <summary>
/// HTTP-context based <see cref="IUserAccessor"/> extracting identity from claims principal.
/// </summary>
/// <remarks>
/// <para>
/// Resolves the current authenticated user from <see cref="HttpContext.User"/>. Intended for ASP.NET
/// Core request pipelines where authentication middleware populates a claims principal.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Throws <see cref="UnauthorizedAccessException"/> when no authenticated user.</description></item>
///   <item><description>Accepts multiple fallback claim types for Id, Username, Display name.</description></item>
///   <item><description>Returns cached data per invocation (no ambient storage).</description></item>
///   <item><description>Completes synchronously and wraps result in <see cref="ValueTask{TResult}"/>.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Register as Scoped to align with request lifetime.</description></item>
///   <item><description>Augment claims at authentication time for richer user metadata.</description></item>
///   <item><description>Wrap accessor in decorators if additional caching / auditing required.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: Stateless aside from injected accessor; safe within request scope. Not intended
/// for cross-request reuse beyond DI lifetime semantics.</para>
/// <para>Performance: O(n) in number of claims (linear scans over small arrays of preferred types).
/// No heap allocations on hot path besides role array (if present) and resulting <see cref="UserInfo"/>.</para>
/// </remarks>
public class HttpContextUserAccessor: IUserAccessor {
  private static readonly string[] IdClaimTypes = ["sub", ClaimTypes.NameIdentifier];
  private static readonly string[] UsernameClaimTypes = [ClaimTypes.Name, "name", ClaimTypes.Email];
  private static readonly string[] DisplayNameClaimTypes = ["given_name", ClaimTypes.GivenName, ClaimTypes.Name];

  private readonly IHttpContextAccessor _HttpContextAccessor;

  /// <summary>Creates accessor using provided HTTP context accessor.</summary>
  /// <exception cref="ArgumentNullException">When <paramref name="httpContextAccessor"/> is null.</exception>
  public HttpContextUserAccessor(IHttpContextAccessor httpContextAccessor) {
    ArgumentNullException.ThrowIfNull(httpContextAccessor);
    _HttpContextAccessor = httpContextAccessor;
  }

  /// <inheritdoc />
  public ValueTask<UserInfo> GetCurrentUserInfoAsync() {
    var principal = _HttpContextAccessor.HttpContext?.User;
    if (principal?.Identity?.IsAuthenticated != true) {
      throw new UnauthorizedAccessException(
        "No authenticated user found in the current HTTP context.");
    }

    var userId = GetUserId(principal);
    var username = GetFirstNonEmpty(principal, UsernameClaimTypes) ?? "Unknown";
    var displayName = GetFirstNonEmpty(principal, DisplayNameClaimTypes) ?? username;
    var roles = GetRoles(principal);

    var info = new UserInfo(new UserId(userId), username, displayName, true, roles);
    return ValueTask.FromResult(info);
  }

  private static Guid GetUserId(ClaimsPrincipal user) {
    var raw = GetFirstNonEmpty(user, IdClaimTypes) ?? string.Empty;
    if (!Guid.TryParse(raw, CultureInfo.InvariantCulture, out var id)) {
      throw new UnauthorizedAccessException("User ID claim not found or invalid.");
    }
    return id;
  }

  private static string? GetFirstNonEmpty(ClaimsPrincipal user, string[] claimTypes) {
    foreach (var type in claimTypes) {
      var value = user.FindFirst(type)?.Value;
      if (!string.IsNullOrWhiteSpace(value)) {
        return value;
      }
    }
    return null;
  }

  private static string[] GetRoles(ClaimsPrincipal user) {
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    return roles.Length == 0 ? [] : roles;
  }
}
