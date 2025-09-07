using System;
using System.Linq.Expressions;

namespace NSI.Specifications.Abstractions;

/// <summary>
/// Describes a boolean rule over an entity of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Entity type evaluated by the specification.</typeparam>
public interface ISpecification<T> {
  /// <summary>
  /// Converts this specification into its <see cref="Expression{TDelegate}"/> representation.
  /// </summary>
  /// <returns>Expression returning <see langword="true"/> when the entity satisfies the rule.</returns>
  public Expression<Func<T, bool>> ToExpression();

  /// <summary>
  /// Evaluates the specification against a concrete instance.
  /// </summary>
  /// <param name="candidate">Entity instance.</param>
  /// <returns><see langword="true"/> when satisfied.</returns>
  public bool IsSatisfiedBy(T candidate);
}
