using System;
using System.Linq.Expressions;
using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Core;

/// <summary>
/// Base class implementing common combinators for specifications.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public abstract class Specification<T>: ISpecification<T> {
  /// <inheritdoc />
  public abstract Expression<Func<T, bool>> ToExpression();

  /// <inheritdoc />
  public bool IsSatisfiedBy(T candidate) => ToExpression().Compile()(candidate);
}
