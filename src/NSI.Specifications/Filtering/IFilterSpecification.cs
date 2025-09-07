using NSI.Specifications.Abstractions;

namespace NSI.Specifications.Filtering;

/// <summary>
/// Marker interface for filter specifications.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IFilterSpecification<T>: ISpecification<T> { }
