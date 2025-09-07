using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace NSI.Specifications.Include;

internal static class IncludeExpressionHelper
{
    private static readonly MethodInfo IncludeOpenGeneric = GetIncludeOpenGeneric();
    private static readonly MethodInfo ThenIncludeRefOpenGeneric = GetThenIncludeRefOpenGeneric();
    private static readonly MethodInfo ThenIncludeCollOpenGeneric = GetThenIncludeCollectionOpenGeneric();

    private static MethodInfo GetIncludeOpenGeneric()
    {
        static bool IsQueryableParam(MethodInfo m)
        {
            var p0 = m.GetParameters()[0].ParameterType;
            return p0.IsGenericType && p0.GetGenericTypeDefinition() == typeof(IQueryable<>);
        }

        return typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include))
            .Where(m => m.IsGenericMethodDefinition)
            .Where(m => m.GetGenericArguments().Length == 2)
            .Where(m => m.GetParameters().Length == 2)
            .First(IsQueryableParam);
    }

    private static MethodInfo GetThenIncludeRefOpenGeneric()
    {
        static bool IsIncludable(MethodInfo m)
        {
            var p0 = m.GetParameters()[0].ParameterType;
            return p0.IsGenericType && p0.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<,>);
        }

        static bool IsReferencePrev(MethodInfo m)
        {
            var prevParam = m.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            var isEnumerable = prevParam.IsGenericType && prevParam.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>);
            return !isEnumerable;
        }

        return typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Where(m => m.IsGenericMethodDefinition)
            .Where(m => m.GetGenericArguments().Length == 3)
            .Where(m => m.GetParameters().Length == 2)
            .Where(IsIncludable)
            .First(IsReferencePrev);
    }

    private static MethodInfo GetThenIncludeCollectionOpenGeneric()
    {
        static bool IsIncludable(MethodInfo m)
        {
            var p0 = m.GetParameters()[0].ParameterType;
            return p0.IsGenericType && p0.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<,>);
        }

        static bool IsCollectionPrev(MethodInfo m)
        {
            var prevParam = m.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            return prevParam.IsGenericType && prevParam.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>);
        }

        return typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Where(m => m.IsGenericMethodDefinition)
            .Where(m => m.GetGenericArguments().Length == 3)
            .Where(m => m.GetParameters().Length == 2)
            .Where(IsIncludable)
            .First(IsCollectionPrev);
    }

    public static IQueryable<T> Apply<T>(IQueryable<T> source, IIncludeSpecification<T> spec) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(spec);

        var current = source;
        foreach (var steps in spec.Chains.Select(c => c.Steps))
        {
            if (steps.Count == 0)
            {
                continue;
            }

            // Begin with Include and thread the result through ThenInclude via reflection
            var firstPropType = steps[0].Body.Type;
            var includeMethod = IncludeOpenGeneric.MakeGenericMethod(typeof(T), firstPropType);
            var state = includeMethod.Invoke(null, [current, steps[0]])!;
            state = ApplyThenIncludes(typeof(T), state, steps);
            current = (IQueryable<T>)state;
        }

        foreach (var path in spec.StringPaths)
        {
            current = EntityFrameworkQueryableExtensions.Include(current, path);
        }
        return current;
    }

    private static object ApplyThenIncludes(Type rootType, object state, System.Collections.Generic.IReadOnlyList<System.Linq.Expressions.LambdaExpression> steps)
    {
        for (var i = 1; i < steps.Count; i++)
        {
            var prevType = steps[i - 1].Body.Type;
            var enumerableIface = prevType == typeof(string)
                ? null
                : Array.Find(prevType.GetInterfaces(), it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>));
            var isCollection = enumerableIface is not null;
            var prevElementType = isCollection ? enumerableIface!.GetGenericArguments()[0] : prevType;
            var propType = steps[i].Body.Type;

            var thenMethodOpen = isCollection ? ThenIncludeCollOpenGeneric : ThenIncludeRefOpenGeneric;
            var thenMethod = thenMethodOpen.MakeGenericMethod(rootType, prevElementType, propType);
            state = thenMethod.Invoke(null, [state, steps[i]])!;
        }
        return state;
    }
}
