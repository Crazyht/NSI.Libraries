using Microsoft.Extensions.Logging;
using NSI.Core.Identity;

namespace NSI.AspNetCore.Identity;
/// <summary>
/// A hybrid implementation of <see cref="IUserAccessor"/> that attempts to retrieve user information
/// from the HTTP context first and falls back to a daemon user if that fails.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides a seamless transition between interactive web requests and
/// non-interactive operations by first trying to get the user from the HTTP context,
/// and if that fails (e.g., no authenticated user or outside HTTP context), using a daemon
/// user as a fallback.
/// </para>
/// <para>
/// This approach is particularly useful for:
/// <list type="bullet">
///   <item><description>Components that may be used in both web and non-web contexts</description></item>
///   <item><description>Services that need to handle both authenticated and unauthenticated requests</description></item>
///   <item><description>Operations that might be triggered by either users or background processes</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Registration in service collection:
/// <code>
/// // Register both required services
/// services.AddHttpContextAccessor();
/// services.Configure&lt;ServiceUserSettings&gt;(configuration.GetSection("ServiceUser"));
/// 
/// // Register the hybrid user accessor
/// services.AddScoped&lt;IUserAccessor, HybridUserAccessor&gt;();
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the <see cref="HybridUserAccessor"/> class.
/// </remarks>
/// <param name="httpContextUserAccessor">The HTTP context-based user accessor.</param>
/// <param name="daemonUserAccessor">The daemon user accessor for fallback.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public class HybridUserAccessor(
    HttpContextUserAccessor httpContextUserAccessor,
    DaemonUserAccessor daemonUserAccessor,
    ILogger<HybridUserAccessor>? logger = null): IUserAccessor {
  private readonly HttpContextUserAccessor _HttpContextUserAccessor = httpContextUserAccessor ??
        throw new ArgumentNullException(nameof(httpContextUserAccessor));
  private readonly DaemonUserAccessor _DaemonUserAccessor = daemonUserAccessor ??
        throw new ArgumentNullException(nameof(daemonUserAccessor));
  private readonly ILogger<HybridUserAccessor>? _Logger = logger;

  /// <summary>
  /// Gets information about the current user, attempting HTTP context first and falling back to daemon user.
  /// </summary>
  /// <returns>A <see cref="ValueTask{TResult}"/> containing the current <see cref="UserInfo"/>.</returns>
  /// <remarks>
  /// <para>
  /// This method first attempts to retrieve the user from the current HTTP context. If that fails
  /// because there is no authenticated user or no HTTP context is available, it falls back to
  /// the daemon user.
  /// </para>
  /// <para>
  /// This approach ensures that operations always have a valid user context for audit tracking
  /// and other authorization purposes, regardless of how they were initiated.
  /// </para>
  /// </remarks>
  public async ValueTask<UserInfo> GetCurrentUserInfoAsync() {
    try {
      // First try to get the user from HTTP context
      return await _HttpContextUserAccessor.GetCurrentUserInfoAsync();
    } catch (Exception ex) when (ex is UnauthorizedAccessException ||
                                 ex is InvalidOperationException) {
      // Log the fallback if a logger is available
      Log.FallingBackToDaemonUser(_Logger, ex);

      // Fall back to the daemon user
      return await _DaemonUserAccessor.GetCurrentUserInfoAsync();
    }
  }

  private static class Log {
    private static readonly Action<ILogger, Exception?> LogFallingBackToDaemonUser =
        LoggerMessage.Define(LogLevel.Debug, new EventId(0, nameof(FallingBackToDaemonUser)),
        "Falling back to daemon user because HTTP context user is not available.");

    public static void FallingBackToDaemonUser(ILogger? logger, Exception? exception) {
      if (logger is null) {
        return;
      }
      LogFallingBackToDaemonUser(logger, exception);
    }
  }
}
