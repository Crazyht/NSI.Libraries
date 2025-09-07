namespace NSI.Domains.Audit;

/// <summary>
/// Marker interface for auditable models with creation and modification tracking.
///
/// Arguments:
/// - CreatedOn (DateTime?): creation timestamp.
/// - CreatedBy (UserId?): creator identifier.
/// - ModifiedOn (DateTime?): last modification timestamp.
/// - ModifiedBy (UserId?): last modifier identifier.
/// </summary>
public interface IAuditableModel {

  /// <summary>UTC date/time when the model was created.</summary>
  /// <value>DateTime?, creation timestamp.</value>
  public DateTime? CreatedOn { get; }

  /// <summary>Identifier of the user who created the model.</summary>
  /// <value>UserId?, creator's identifier.</value>
  public UserId? CreatedBy { get; }

  /// <summary>UTC date/time when the model was last modified.</summary>
  /// <value>DateTime?, modification timestamp.</value>
  public DateTime? ModifiedOn { get; }

  /// <summary>Identifier of the user who last modified the model.</summary>
  /// <value>UserId?, modifier's identifier.</value>
  public UserId? ModifiedBy { get; }

}
