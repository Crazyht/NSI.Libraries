using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSI.Core.Identity;
using NSI.Domains;

namespace NSI.AspNetCore.Identity;
/// <summary>
/// Implementation of <see cref="IUserAccessor"/> that retrieves user information from the current HTTP context.
/// </summary>
/// <remarks>
/// <para>
/// This class extracts user information from the current HTTP request's claims principal, making it suitable
/// for web applications where users are authenticated through ASP.NET Core's authentication system.
/// </para>
/// <para>
/// It requires the user to be authenticated and have specific claims:
/// <list type="bullet">
///   <item><description>A claim with type "sub" or "nameidentifier" for the user ID</description></item>
///   <item><description>A claim with type "name" or "email" for the username</description></item>
///   <item><description>A claim with type "given_name" or "name" for the display name</description></item>
/// </list>
/// </para>
/// <para>
/// This implementation should be registered as a scoped service to align with the HTTP request lifetime.
/// </para>
/// </remarks>
/// <example>
/// Registration in service collection:
/// <code>
/// // Register in ASP.NET Core dependency injection
/// services.AddHttpContextAccessor();
/// services.AddScoped&lt;IUserAccessor, HttpContextUserAccessor&gt;();
/// </code>
/// </example>
public class HttpContextUserAccessor: IUserAccessor {
  private readonly IHttpContextAccessor _HttpContextAccessor;

  /// <summary>
  /// Initializes a new instance of the <see cref="HttpContextUserAccessor"/> class.
  /// </summary>
  /// <param name="httpContextAccessor">The HTTP context accessor service.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpContextAccessor"/> is null.</exception>
  public HttpContextUserAccessor(IHttpContextAccessor httpContextAccessor) {
    ArgumentNullException.ThrowIfNull(httpContextAccessor);
    _HttpContextAccessor = httpContextAccessor;
  }

  /// <summary>
  /// Gets information about the current user from the HTTP context asynchronously.
  /// </summary>
  /// <returns>A <see cref="ValueTask{TResult}"/> containing the current <see cref="UserInfo"/>.</returns>
  /// <exception cref="UnauthorizedAccessException">Thrown when there is no authenticated user in the current HTTP context.</exception>
  /// <remarks>
  /// <para>
  /// This method extracts user identity information from claims in the current HTTP context.
  /// It requires the user to be authenticated and have the necessary claims to identify the user.
  /// </para>
  /// <para>
  /// The implementation completes synchronously but returns a ValueTask to match the
  /// contract of <see cref="IUserAccessor.GetCurrentUserInfoAsync"/>.
  /// </para>
  /// </remarks>
  public ValueTask<UserInfo> GetCurrentUserInfoAsync() {
    var user = _HttpContextAccessor.HttpContext?.User;

    if (user == null || user.Identity?.IsAuthenticated != true) {
      throw new UnauthorizedAccessException("No authenticated user found in the current HTTP context.");
    }

    var userId = GetUserId(user);
    var username = GetUsername(user);
    var displayName = GetDisplayName(user, username);
    var roles = GetRoles(user);

    var userInfo = new UserInfo(
        new UserId(userId),
        username,
        displayName,
        true,
        roles
    );

    return ValueTask.FromResult(userInfo);
  }

  private static Guid GetUserId(ClaimsPrincipal user) {
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ??
                      user.FindFirst("sub");

    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, CultureInfo.InvariantCulture, out var userId)) {
      throw new UnauthorizedAccessException("User ID claim not found or invalid.");
    }

    return userId;
  }

  private static string GetUsername(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Name)?.Value ??
             user.FindFirst("name")?.Value ??
             user.FindFirst(ClaimTypes.Email)?.Value ??
             "Unknown";

  private static string GetDisplayName(ClaimsPrincipal user, string username) => user.FindFirst("given_name")?.Value ??
             user.FindFirst(ClaimTypes.GivenName)?.Value ??
             user.FindFirst(ClaimTypes.Name)?.Value ??
             username;

  private static string[] GetRoles(ClaimsPrincipal user) {
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    return roles.Length > 0 ? roles : [];
  }
}
