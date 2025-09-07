using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Replaces a target parameter inside an expression tree.
/// </summary>
internal sealed class ParameterReplacer : ExpressionVisitor {
  private readonly ParameterExpression _Source;
  private readonly ParameterExpression _Target;

  private ParameterReplacer(ParameterExpression source, ParameterExpression target) {
    _Source = source;
    _Target = target;
  }

  public static Expression Replace(Expression body, ParameterExpression source, ParameterExpression target) => new ParameterReplacer(source, target).Visit(body)!;

  protected override Expression VisitParameter(ParameterExpression node) {
    if (node == _Source) {
      return _Target;
    }
    return base.VisitParameter(node);
  }
}
