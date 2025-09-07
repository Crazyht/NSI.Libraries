using NSI.Core.Results;

namespace NSI.Core.Mediator.Abstractions {
  /// <summary>
  /// Represents the next step in the request processing pipeline.
  /// </summary>
  /// <typeparam name="TResponse">The response type.</typeparam>
  /// <returns>A task containing the result of the next step in the pipeline.</returns>
  /// <remarks>
  /// <para>
  /// This delegate is used in the decorator pattern to enable chaining of request processors.
  /// Each decorator receives this delegate to invoke the next step in the pipeline,
  /// allowing for pre- and post-processing logic around the core handler execution.
  /// </para>
  /// <para>
  /// The pipeline typically flows: Decorator1 -> Decorator2 -> ... -> Core Handler
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public async Task&lt;Result&lt;TResponse&gt;&gt; HandleAsync(
  ///   TRequest request, 
  ///   RequestHandlerFunction&lt;TResponse&gt; continuation, 
  ///   CancellationToken cancellationToken) {
  ///   
  ///   // Pre-processing logic
  ///   _logger.LogInformation("Processing {RequestType}", typeof(TRequest).Name);
  ///   
  ///   // Call next in pipeline
  ///   var result = await continuation();
  ///   
  ///   // Post-processing logic
  ///   _logger.LogInformation("Completed {RequestType}: {Success}", typeof(TRequest).Name, result.IsSuccess);
  ///   
  ///   return result;
  /// }
  /// </code>
  /// </example>
  public delegate Task<Result<TResponse>> RequestHandlerFunction<TResponse>();
}
