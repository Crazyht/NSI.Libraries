using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Utility to extract member chain from an access path expression.
/// </summary>
internal static class MemberChainExtractor {
  public static System.Collections.Generic.List<System.Reflection.MemberInfo> Extract(Expression body) {
  var result = new System.Collections.Generic.List<System.Reflection.MemberInfo>();
    var current = Strip(body);
    while (current is MemberExpression m) {
      result.Add(m.Member);
      current = m.Expression is null ? null : Strip(m.Expression);
    }
    return result;
  }
  private static Expression Strip(Expression expr) {
    while (expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) {
      expr = ((UnaryExpression)expr).Operand;
    }
    return expr;
  }
}
