namespace NSI.Domains;

/// <summary>
/// Marker interface identifying a domain type that participates in a Table‑Per‑Type (TPT) inheritance mapping strategy.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface on root or derived entities when configuring Entity Framework Core TPT mappings.
/// It provides an explicit semantic intent without imposing behavior.
/// </para>
/// </remarks>
public interface ITptStrategy { }
