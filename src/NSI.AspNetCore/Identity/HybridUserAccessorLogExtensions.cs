using Microsoft.Extensions.Logging;

namespace NSI.AspNetCore.Identity;

/// <summary>
/// LoggerMessage source-generated logging for <see cref="HybridUserAccessor"/>.
/// </summary>
internal static class HybridUserAccessorLogExtensions {
  private static readonly Action<ILogger, Exception?> FallbackLog = LoggerMessage.Define(
    LogLevel.Debug,
    new EventId(21, "HybridUserAccessorFallback"),
    "Falling back to daemon user because HTTP context user is not available.");

  internal static void LogFallbackToDaemonUser(this ILogger logger, Exception exception) {
    if (!logger.IsEnabled(LogLevel.Debug)) {
      return;
    }
    FallbackLog(logger, exception);
  }
}
