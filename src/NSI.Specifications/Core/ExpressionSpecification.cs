using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Wraps a raw expression inside a specification.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
internal sealed class ExpressionSpecification<T>(Expression<System.Func<T, bool>> expression)
: Specification<T> {
  private readonly Expression<System.Func<T, bool>> _Expression = expression ?? throw new System.ArgumentNullException(nameof(expression));
  public override Expression<System.Func<T, bool>> ToExpression() => _Expression;
}
