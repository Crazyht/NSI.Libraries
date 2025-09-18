using Microsoft.Extensions.Logging;
using NSI.Core.Identity;

namespace NSI.AspNetCore.Identity;

/// <summary>
/// Hybrid <see cref="IUserAccessor"/> resolving interactive principal first then falling back to daemon user.
/// </summary>
/// <remarks>
/// <para>
/// Enables components to obtain a <see cref="UserInfo"/> whether code executes inside an HTTP request
/// pipeline (authenticated user present) or in a non-interactive/background context where only a
/// system (daemon) identity is available.
/// </para>
/// <para>Semantics:
/// <list type="bullet">
///   <item><description>Primary source: <see cref="HttpContextUserAccessor"/>; fallback: <see cref="DaemonUserAccessor"/>.</description></item>
///   <item><description>Only <see cref="UnauthorizedAccessException"/> / <see cref="InvalidOperationException"/> trigger fallback.</description></item>
///   <item><description>Never returns <c>null</c>; always a valid <see cref="UserInfo"/> or throws for unexpected errors.</description></item>
///   <item><description>Propagation: Exceptions other than the allowed fallback set bubble up unchanged.</description></item>
/// </list>
/// </para>
/// <para>Guidelines:
/// <list type="bullet">
///   <item><description>Register as <c>Scoped</c> (same lifetime as underlying HTTP accessor) or <c>Singleton</c> if both dependencies support it.</description></item>
///   <item><description>Use in services that operate across both request &amp; background execution paths.</description></item>
///   <item><description>Log at <c>Debug</c> level on fallback only (avoids noise for normal flows).</description></item>
///   <item><description>Do not use for security decisions that must distinguish interactive vs system users—inspect the returned user object.</description></item>
/// </list>
/// </para>
/// <para>Thread-safety: The accessor is immutable; underlying dependencies must honor their own lifetimes.
/// Safe for concurrent calls.</para>
/// <para>Performance: Single try/catch; no allocations except when logging fallback &amp; constructing
/// daemon user (already cached by <see cref="DaemonUserAccessor"/>). Uses <see cref="ValueTask{TResult}"/>
/// to minimize allocations when underlying accessors complete synchronously.</para>
/// </remarks>
public class HybridUserAccessor(
  HttpContextUserAccessor httpContextUserAccessor,
  DaemonUserAccessor daemonUserAccessor,
  ILogger<HybridUserAccessor>? logger = null): IUserAccessor {
  private readonly HttpContextUserAccessor _HttpContextUserAccessor = httpContextUserAccessor
    ?? throw new ArgumentNullException(nameof(httpContextUserAccessor));
  private readonly DaemonUserAccessor _DaemonUserAccessor = daemonUserAccessor
    ?? throw new ArgumentNullException(nameof(daemonUserAccessor));
  private readonly ILogger<HybridUserAccessor>? _Logger = logger;

  /// <inheritdoc />
  public async ValueTask<UserInfo> GetCurrentUserInfoAsync() {
    try {
      return await _HttpContextUserAccessor.GetCurrentUserInfoAsync().ConfigureAwait(false);
    } catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException) {
      _Logger?.LogFallbackToDaemonUser(ex);
      return await _DaemonUserAccessor.GetCurrentUserInfoAsync().ConfigureAwait(false);
    }
  }
}
