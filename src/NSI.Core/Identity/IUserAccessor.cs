namespace NSI.Core.Identity {
  /// <summary>
  /// Defines an abstraction for accessing information about the current user.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This interface provides a standard way to obtain user identity information in a context-independent manner.
  /// By abstracting user access logic, application components can work with user information without being
  /// tightly coupled to the specific authentication mechanism or user source.
  /// </para>
  /// <para>
  /// The implementation of this interface may vary based on execution context:
  /// <list type="bullet">
  ///   <item><description>In web applications, it might extract user details from HTTP context or authentication tokens</description></item>
  ///   <item><description>In background services, it could use <see cref="DaemonUserAccessor"/> to provide system user credentials</description></item>
  ///   <item><description>In testing scenarios, it could provide mock user information</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// This interface is particularly important for audit tracking functionality, as it enables 
  /// automatic attribution of database operations to specific users.
  /// </para>
  /// </remarks>
  /// <example>
  /// Registering implementations in dependency injection:
  /// <code>
  /// // For web applications, using current HTTP context
  /// services.AddScoped&lt;IUserAccessor, HttpContextUserAccessor&gt;();
  /// 
  /// // For background services, using system account
  /// services.AddSingleton&lt;IUserAccessor, DaemonUserAccessor&gt;();
  /// </code>
  /// 
  /// Using in service classes:
  /// <code>
  /// public class AuditService 
  /// {
  ///     private readonly IUserAccessor _userAccessor;
  ///     
  ///     public AuditService(IUserAccessor userAccessor)
  ///     {
  ///         _userAccessor = userAccessor;
  ///     }
  ///     
  ///     public async Task RecordAction(string actionType)
  ///     {
  ///         var user = await _userAccessor.GetCurrentUserInfoAsync();
  ///         // Record that user.Id performed actionType
  ///     }
  /// }
  /// </code>
  /// </example>
  public interface IUserAccessor {
    /// <summary>
    /// Gets information about the current user asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the current <see cref="UserInfo"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves identity information for the user associated with the current execution context.
    /// The returned <see cref="UserInfo"/> contains the user's ID, username, and display name, which can
    /// be used for authentication, authorization, personalization, and audit tracking.
    /// </para>
    /// <para>
    /// This method is designed to be asynchronous to support implementations that may need to retrieve
    /// user information from external sources such as identity providers or user databases.
    /// </para>
    /// </remarks>
    public ValueTask<UserInfo> GetCurrentUserInfoAsync();
  }
}
