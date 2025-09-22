namespace NSI.Core.Mediator.Abstractions;

/// <summary>
/// Marker interface for fire-and-forget notifications that may be handled by multiple handlers.
/// </summary>
/// <remarks>
/// <para>
/// Notifications represent events that have occurred in the system and may be of interest
/// to multiple parts of the application. They follow a publish-subscribe pattern where
/// multiple handlers can react to the same notification.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
///   <item><description>Fire-and-forget: The sender doesn't wait for completion</description></item>
///   <item><description>Multiple handlers: Zero or more handlers can process the notification</description></item>
///   <item><description>No return value: Notifications don't return meaningful data</description></item>
///   <item><description>Resilient: Failure of one handler doesn't affect others</description></item>
/// </list>
/// </para>
/// <para>
/// Notifications are processed by <see cref="IMediator.DispatchAsync{TNotification}"/> in a 
/// fire-and-forget manner, where multiple <see cref="IRequestHandler{TRequest, TResponse}"/> 
/// implementations can handle the same notification independently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record UserCreatedNotification(Guid UserId, string Email, DateTime CreatedAt): INotification;
/// public record OrderCompletedNotification(Guid OrderId, Guid CustomerId, decimal Amount): INotification;
/// public record PaymentFailedNotification(Guid PaymentId, string Reason): INotification;
/// 
/// // Multiple handlers can process the same notification
/// public class SendWelcomeEmailHandler: IRequestHandler&lt;UserCreatedNotification, Unit&gt; {
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(UserCreatedNotification request, CancellationToken cancellationToken) {
///     // Send welcome email logic
///     await emailService.SendWelcomeEmailAsync(request.Email, cancellationToken);
///     return Result.Success(Unit.Value);
///   }
/// }
/// 
/// public class CreateUserProfileHandler: IRequestHandler&lt;UserCreatedNotification, Unit&gt; {
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(UserCreatedNotification request, CancellationToken cancellationToken) {
///     // Create user profile logic
///     await profileService.CreateProfileAsync(request.UserId, cancellationToken);
///     return Result.Success(Unit.Value);
///   }
/// }
/// 
/// public class UpdateAnalyticsHandler: IRequestHandler&lt;UserCreatedNotification, Unit&gt; {
///   public async Task&lt;Result&lt;Unit&gt;&gt; HandleAsync(UserCreatedNotification request, CancellationToken cancellationToken) {
///     // Update analytics logic
///     await analyticsService.TrackUserCreationAsync(request.UserId, request.CreatedAt, cancellationToken);
///     return Result.Success(Unit.Value);
///   }
/// }
/// 
/// // Usage in mediator
/// await mediator.DispatchAsync(new UserCreatedNotification(user.Id, user.Email, DateTime.UtcNow));
/// </code>
/// </example>
/// <seealso cref="IRequest{TResponse}"/>
/// <seealso cref="IMediator"/>
/// <seealso cref="Unit"/>
public interface INotification: IRequest<Unit> { }
