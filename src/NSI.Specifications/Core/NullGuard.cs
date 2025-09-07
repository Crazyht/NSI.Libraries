using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NSI.Specifications.Core;

/// <summary>
/// Builds null-safe predicate expressions from a member access path and a terminal predicate.
/// </summary>
public static class NullGuard {
  /// <summary>
  /// Creates a null-safe predicate guarding every nullable reference on the provided path.
  /// </summary>
  /// <typeparam name="T">Root entity type.</typeparam>
  /// <typeparam name="TMember">Leaf member type.</typeparam>
  /// <param name="path">Member access path (e.g. <c>x =&gt; x.Address.Country.Name</c>).</param>
  /// <param name="predicate">Predicate applied to the leaf member.</param>
  /// <returns>Composed expression safe against intermediate nulls.</returns>
  public static Expression<Func<T, bool>> Safe<T, TMember>(Expression<Func<T, TMember>> path, Expression<Func<TMember, bool>> predicate) {
    ArgumentNullException.ThrowIfNull(path);
    ArgumentNullException.ThrowIfNull(predicate);
  var parameter = path.Parameters[0];
  var memberChain = ExtractChain(path.Body);
  if (memberChain.Count == 0) {
      var direct = ParameterReplacer.Replace(predicate.Body, predicate.Parameters[0], parameter);
      return Expression.Lambda<Func<T, bool>>(direct, parameter);
    }

  var current = (Expression)parameter;
  var nullChecks = new List<Expression>();
    foreach (var member in memberChain) {
      current = Expression.MakeMemberAccess(current, member);
      var memberType = member.GetMemberType();
      if (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null) {
        nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null, current.Type)));
      }
    }

  var leafAccess = current;
  var predicateBody = new LeafRebinder(predicate.Parameters[0], leafAccess).Visit(predicate.Body)!;
  var body = predicateBody;
    for (var i = nullChecks.Count - 1; i >= 0; i--) {
      body = Expression.AndAlso(nullChecks[i], body);
    }
    return Expression.Lambda<Func<T, bool>>(body, parameter);
  }

  private static List<System.Reflection.MemberInfo> ExtractChain(Expression body) {
    var result = new List<System.Reflection.MemberInfo>();
    var current = Strip(body);
    while (current is MemberExpression m) {
      result.Insert(0, m.Member);
      current = m.Expression is null ? null : Strip(m.Expression);
    }
    return result;
  }

  private static Expression Strip(Expression expr) {
    while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked) {
      expr = ((UnaryExpression)expr).Operand;
    }
    return expr;
  }

  private sealed class LeafRebinder(ParameterExpression source, Expression target) : ExpressionVisitor {
    private readonly ParameterExpression _Source = source;
    private readonly Expression _Target = target;
    protected override Expression VisitParameter(ParameterExpression node) => node == _Source ? _Target : base.VisitParameter(node);
  }
}

internal static class MemberInfoExtensions {
  public static Type GetMemberType(this System.Reflection.MemberInfo member) => member switch {
    System.Reflection.PropertyInfo p => p.PropertyType,
    System.Reflection.FieldInfo f => f.FieldType,
    _ => typeof(object)
  };
}
