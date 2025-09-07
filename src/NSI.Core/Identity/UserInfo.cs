using NSI.Domains;

namespace NSI.Core.Identity {
  /// <summary>
  /// User information included in authentication responses.
  /// </summary>
  /// <param name="Id">Unique user identifier as string.</param>
  /// <param name="Email">User's email address.</param>
  /// <param name="FullName">User's display name for UI.</param>
  /// <param name="IsActive">Indicates if the user account is active.</param>
  /// <param name="Roles">List of user roles for client-side authorization.</param>
  /// <remarks>
  /// This model contains safe user information that can be shared with
  /// the client application without exposing sensitive data.
  /// </remarks>
  public record UserInfo(
    UserId Id,
    string Email,
    string FullName,
    bool IsActive,
    IList<string> Roles
  );
}
