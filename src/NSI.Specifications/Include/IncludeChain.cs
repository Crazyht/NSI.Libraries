using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NSI.Specifications.Include;

internal sealed class IncludeChain<TRoot>(IReadOnlyList<LambdaExpression> steps) : IIncludeChain<TRoot>
{
    public IReadOnlyList<LambdaExpression> Steps { get; } = steps ?? throw new ArgumentNullException(nameof(steps));
    public Type RootType => typeof(TRoot);
}
