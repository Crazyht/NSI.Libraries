using NSI.Domains;

namespace NSI.Core.Identity;

/// <summary>
/// Represents user information included in authentication responses and user identity contexts.
/// </summary>
/// <param name="Id">The unique identifier for the user account.</param>
/// <param name="Email">The user's email address for authentication and communication.</param>
/// <param name="FullName">The user's full display name for UI presentation.</param>
/// <param name="IsActive">Flag indicating whether the user account is currently active and accessible.</param>
/// <param name="Roles">Collection of role names assigned to the user for authorization decisions.</param>
/// <remarks>
/// <para>
/// This record encapsulates essential user identity information that is safe to share with
/// client applications and services without exposing sensitive authentication details such as
/// password hashes, security tokens, or internal system identifiers.
/// </para>
/// <para>
/// Key characteristics and usage patterns:
/// <list type="bullet">
///   <item><description>Immutable data structure - all properties are read-only after construction</description></item>
///   <item><description>Thread-safe by design due to immutable nature</description></item>
///   <item><description>Suitable for serialization to JSON for API responses</description></item>
///   <item><description>Used in authentication flows, user profile displays, and authorization decisions</description></item>
///   <item><description>Contains only public-facing user information, no sensitive data</description></item>
/// </list>
/// </para>
/// <para>
/// Security considerations: This record should only contain information that is safe to expose
/// to client applications. Never include sensitive data such as password hashes, security tokens,
/// internal system IDs, or personally identifiable information that should remain private.
/// </para>
/// <para>
/// Performance: As an immutable record, instances can be safely cached and shared across
/// multiple threads without synchronization concerns. The record implements value equality,
/// making it suitable for comparison operations and use in collections.
/// </para>
/// </remarks>
/// <example>
/// Creating a UserInfo instance:
/// <code>
/// var user = new UserInfo(
///   Id: new UserId(Guid.Parse("12345678-1234-5678-9abc-123456789012")),
///   Email: "john.doe@company.com",
///   FullName: "John Doe",
///   IsActive: true,
///   Roles: ["User", "Manager"]
/// );
/// </code>
/// 
/// Using in authentication context:
/// <code>
/// public class AuthenticationService {
///   public async Task&lt;Result&lt;UserInfo&gt;&gt; AuthenticateAsync(string email, string password) {
///     // Validate credentials (implementation details omitted)
///     if (credentialsValid) {
///       return Result.Success(new UserInfo(
///         Id: retrievedUserId,
///         Email: email,
///         FullName: retrievedFullName,
///         IsActive: userAccount.IsActive,
///         Roles: userAccount.Roles.Select(r => r.Name).ToList()
///       ));
///     }
///     return Result.Failure&lt;UserInfo&gt;(AuthenticationErrors.InvalidCredentials);
///   }
/// }
/// </code>
/// 
/// Using in authorization decisions:
/// <code>
/// public bool HasRole(UserInfo user, string requiredRole) {
///   return user.IsActive &amp;&amp; user.Roles.Contains(requiredRole);
/// }
/// 
/// public bool CanAccessResource(UserInfo user) {
///   return user.IsActive &amp;&amp; (
///     user.Roles.Contains("Admin") || 
///     user.Roles.Contains("Manager")
///   );
/// }
/// </code>
/// 
/// API response serialization:
/// <code>
/// [HttpGet("profile")]
/// public async Task&lt;ActionResult&lt;UserInfo&gt;&gt; GetUserProfile() {
///   var user = await _userAccessor.GetCurrentUserInfoAsync();
///   return Ok(user); // Automatically serialized to JSON
/// }
/// </code>
/// </example>
public sealed record UserInfo(
  UserId Id,
  string Email,
  string FullName,
  bool IsActive,
  IList<string> Roles
);
