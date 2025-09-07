using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using NSI.Core.Validation.Abstractions;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Validation;

/// <summary>
/// Provides fluent API extensions for building validators.
/// </summary>
/// <remarks>
/// <para>
/// This class extends the <see cref="Validator{T}"/> with a fluent API that makes it
/// easier to add common validation rules without directly instantiating rule classes.
/// </para>
/// <para>
/// The fluent API improves readability and reduces boilerplate when creating validators.
/// Methods can be chained to build complex validation pipelines with minimal code.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var validator = new Validator&lt;User&gt;()
///   .Required(u => u.Email)
///   .Email(u => u.Email)
///   .StringLength(u => u.Name, 2, 50)
///   .Must(
///     u => u.AcceptedTerms,
///     "TERMS_REQUIRED",
///     "You must accept the terms and conditions."
///   );
///   
/// var result = validator.Validate(user);
/// </code>
/// </para>
/// </remarks>
public static class ValidatorBuilderExtensions {
  /// <summary>
  /// Adds a required field validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the required property.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This rule checks that a property value is not null, empty, or whitespace.
  /// For string properties, it verifies the string is not empty or whitespace.
  /// For reference types, it ensures the value is not null.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// validator.Required(user => user.Email);
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> Required<T>(
    this Validator<T> validator,
    Expression<Func<T, object?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);

    return validator.AddRule(new RequiredRule<T>(propertyExpression));
  }

  /// <summary>
  /// Adds an email validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the email property.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This rule validates that a string property contains a syntactically valid email address.
  /// Note that it doesn't check if the email actually exists, only that it has a valid format.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// validator.Email(user => user.Email);
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> Email<T>(
    this Validator<T> validator,
    Expression<Func<T, string?>> propertyExpression) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);

    return validator.AddRule(new EmailRule<T>(propertyExpression));
  }

  /// <summary>
  /// Adds a string length validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the string property.</param>
  /// <param name="minLength">Minimum allowed length (inclusive).</param>
  /// <param name="maxLength">Maximum allowed length (inclusive).</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="minLength"/> is negative or when <paramref name="maxLength"/> 
  /// is less than <paramref name="minLength"/>.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This rule verifies that a string property's length falls within the specified bounds.
  /// Both minimum and maximum values are inclusive. Null strings are ignored (to validate
  /// that a string is not null, use the <see cref="Required{T}"/> method).
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// // Username must be between 3 and 50 characters
  /// validator.StringLength(user => user.Username, 3, 50);
  /// 
  /// // Description can't exceed 1000 characters (no minimum)
  /// validator.StringLength(product => product.Description, 0, 1000);
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> StringLength<T>(
    this Validator<T> validator,
    Expression<Func<T, string?>> propertyExpression,
    int minLength = 0,
    int maxLength = int.MaxValue) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentOutOfRangeException.ThrowIfNegative(minLength);
    ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);

    return validator.AddRule(new StringLengthRule<T>(propertyExpression, minLength, maxLength));
  }

  /// <summary>
  /// Adds a range validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <typeparam name="TValue">The type of the property value.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the property.</param>
  /// <param name="min">Minimum allowed value (inclusive).</param>
  /// <param name="max">Maximum allowed value (inclusive).</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This rule validates that a numeric property falls within the specified inclusive range.
  /// It works with any type that implements <see cref="IComparable{T}"/>, making it
  /// suitable for various numeric types as well as other comparable types like DateTime.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// // Age must be between 18 and 120
  /// validator.Range(user => user.Age, 18, 120);
  /// 
  /// // Price must be between 0.01 and 9999.99
  /// validator.Range(product => product.Price, 0.01m, 9999.99m);
  /// 
  /// // Date must be in the future but not more than 1 year away
  /// var today = DateTime.Today;
  /// validator.Range(event => event.Date, today.AddDays(1), today.AddYears(1));
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> Range<T, TValue>(
    this Validator<T> validator,
    Expression<Func<T, TValue>> propertyExpression,
    TValue min,
    TValue max) where TValue : IComparable<TValue> {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);

    if (min.CompareTo(max) > 0) {
      throw new ArgumentOutOfRangeException(nameof(max), "Maximum value must be greater than or equal to minimum value");
    }

    return validator.AddRule(new RangeRule<T, TValue>(propertyExpression, min, max));
  }

  /// <summary>
  /// Adds a uniqueness validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <typeparam name="TValue">The type of the property value.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the property.</param>
  /// <param name="existsCheck">
  /// Function to check if value exists. Should return true if the value exists 
  /// (meaning validation fails) or false if the value is unique (validation passes).
  /// </param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when any parameter is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This rule validates that a property value is unique in the system, typically by
  /// performing an asynchronous lookup against a database or other data store.
  /// </para>
  /// <para>
  /// The <paramref name="existsCheck"/> function should access the appropriate repository
  /// through the provided <see cref="IServiceProvider"/>.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// validator.Unique(
  ///   user => user.Email,
  ///   async (services, email, ct) => {
  ///     var repository = services.GetRequiredService&lt;IUserRepository&gt;();
  ///     return await repository.EmailExistsAsync(email, ct);
  ///   }
  /// );
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> Unique<T, TValue>(
    this Validator<T> validator,
    Expression<Func<T, TValue>> propertyExpression,
    Func<IServiceProvider, TValue, CancellationToken, Task<bool>> existsCheck) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);
    ArgumentNullException.ThrowIfNull(existsCheck);

    return validator.AddAsyncRule(new UniqueRule<T, TValue>(propertyExpression, existsCheck));
  }

  /// <summary>
  /// Adds a custom validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="validateFunc">The validation function that implements the rule logic.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="validateFunc"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method allows creating custom validation rules using lambda expressions,
  /// without needing to create separate rule classes for simple validations.
  /// </para>
  /// <para>
  /// The validation function should accept the object instance and validation context,
  /// and return a collection of validation errors (or an empty collection if valid).
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// validator.Custom((user, context) => {
  ///   var errors = new List&lt;IValidationError&gt;();
  ///   
  ///   if (user.Password != user.ConfirmPassword) {
  ///     errors.Add(new ValidationError(
  ///       "PASSWORD_MISMATCH",
  ///       "Passwords do not match",
  ///       "ConfirmPassword"
  ///     ));
  ///   }
  ///   
  ///   return errors;
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> Custom<T>(
    this Validator<T> validator,
    Func<T, IValidationContext, IEnumerable<IValidationError>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(validateFunc);

    return validator.AddRule(new CustomRule<T>(validateFunc));
  }

  /// <summary>
  /// Adds a custom validation rule with a simple predicate.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="predicate">The validation predicate that determines if the rule passes.</param>
  /// <param name="errorCode">The error code to use when validation fails.</param>
  /// <param name="errorMessage">The error message to use when validation fails.</param>
  /// <param name="propertyName">Optional property name to associate with the error.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="predicate"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="errorCode"/> or <paramref name="errorMessage"/> is null or whitespace.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method provides a simpler way to add custom validation rules when you only
  /// need a boolean condition and a single error message. It's useful for business rules
  /// that can be expressed as a simple predicate.
  /// </para>
  /// <para>
  /// The predicate should return true if validation passes, false if it fails.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// // Validate that user has accepted terms
  /// validator.Must(
  ///   user => user.HasAcceptedTerms,
  ///   "TERMS_REQUIRED",
  ///   "You must accept the terms and conditions"
  /// );
  /// 
  /// // Validate that discount is applicable
  /// validator.Must(
  ///   order => order.Items.Count >= 3 || order.Total >= 100,
  ///   "DISCOUNT_INVALID",
  ///   "Discount requires 3+ items or total over $100",
  ///   "DiscountCode"
  /// );
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> Must<T>(
    this Validator<T> validator,
    Func<T, bool> predicate,
    string errorCode,
    string errorMessage,
    string? propertyName = null) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

    return validator.AddRule(new CustomRule<T>((instance, _) => {
      if (!predicate(instance)) {
        return [new ValidationError(errorCode, errorMessage, propertyName)];
      }
      return [];
    }));
  }

  /// <summary>
  /// Adds an async custom validation rule.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="validateFunc">The async validation function that implements the rule logic.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="validateFunc"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method allows creating asynchronous custom validation rules using lambda expressions,
  /// without needing to create separate rule classes for simple validations.
  /// </para>
  /// <para>
  /// The validation function should accept the object instance, validation context, and
  /// cancellation token, and return a task resolving to a collection of validation errors
  /// (or an empty collection if valid).
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// validator.CustomAsync(async (user, context, ct) => {
  ///   var userService = context.ServiceProvider.GetRequiredService&lt;IUserService&gt;();
  ///   var isReserved = await userService.IsReservedUsernameAsync(user.Username, ct);
  ///   
  ///   if (isReserved) {
  ///     return new[] { new ValidationError(
  ///       "USERNAME_RESERVED",
  ///       "This username is reserved",
  ///       nameof(user.Username)
  ///     )};
  ///   }
  ///   
  ///   return Enumerable.Empty&lt;IValidationError&gt;();
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  [SuppressMessage("Minor Code Smell", "S4261:Methods should be named according to their synchronicities", Justification = "Rule is async not method itself.")]
  [SuppressMessage("Minor Code Smell", "S4017:Refactor this method to remove the nested type argument", Justification = "The nested type argument is required for the validation logic.")]
  public static Validator<T> CustomAsync<T>(
    this Validator<T> validator,
    Func<T, IValidationContext, CancellationToken, Task<IEnumerable<IValidationError>>> validateFunc) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(validateFunc);

    return validator.AddAsyncRule(new AsyncCustomRule<T>(validateFunc));
  }

  /// <summary>
  /// Adds an async validation rule with a simple predicate.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="predicate">The async validation predicate that determines if the rule passes.</param>
  /// <param name="errorCode">The error code to use when validation fails.</param>
  /// <param name="errorMessage">The error message to use when validation fails.</param>
  /// <param name="propertyName">Optional property name to associate with the error.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="predicate"/> is null.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="errorCode"/> or <paramref name="errorMessage"/> is null or whitespace.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method provides a simpler way to add asynchronous validation rules when you only
  /// need a boolean condition and a single error message. It's useful for business rules
  /// that require asynchronous operations like database queries or API calls.
  /// </para>
  /// <para>
  /// The predicate should return true if validation passes, false if it fails.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// // Check if email is already registered
  /// validator.MustAsync(
  ///   async (user, ct) => {
  ///     var userRepo = scope.ServiceProvider.GetRequiredService&lt;IUserRepository&gt;();
  ///     return !await userRepo.EmailExistsAsync(user.Email, ct);
  ///   },
  ///   "EMAIL_TAKEN",
  ///   "This email is already registered",
  ///   nameof(User.Email)
  /// );
  /// </code>
  /// </para>
  /// </remarks>
  [SuppressMessage("Minor Code Smell", "S4261:Methods should be named according to their synchronicities", Justification = "Rule is async not method itself.")]
  public static Validator<T> MustAsync<T>(
    this Validator<T> validator,
    Func<T, CancellationToken, Task<bool>> predicate,
    string errorCode,
    string errorMessage,
    string? propertyName = null) {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
    ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

    return validator.AddAsyncRule(new AsyncCustomRule<T>(async (instance, _, ct) => {
      if (!await predicate(instance, ct)) {
        return [new ValidationError(errorCode, errorMessage, propertyName)];
      }
      return [];
    }));
  }

  /// <summary>
  /// Adds validation for a nested object property.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <typeparam name="TProperty">The type of the nested property.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the nested property.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method adds validation for a nested object property by applying the registered
  /// validator for the property's type. It adds both synchronous and asynchronous validation
  /// rules to ensure complete validation coverage.
  /// </para>
  /// <para>
  /// The method will:
  /// <list type="bullet">
  ///   <item><description>Skip validation if the property is null</description></item>
  ///   <item><description>Resolve the appropriate validator for the property type</description></item>
  ///   <item><description>Apply validation to the nested object</description></item>
  ///   <item><description>Prefix property names in validation errors with the parent property path</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// // Validate a user's address using the registered Address validator
  /// validator.ValidateNested(user => user.Address);
  /// 
  /// // Validate a user's profile using the registered UserProfile validator
  /// validator.ValidateNested(user => user.Profile);
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> ValidateNested<T, TProperty>(
    this Validator<T> validator,
    Expression<Func<T, TProperty?>> propertyExpression)
    where TProperty : class {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);

    return validator
      .AddRule(new NestedValidationRule<T, TProperty>(propertyExpression))
      .AddAsyncRule(new NestedAsyncValidationRule<T, TProperty>(propertyExpression));
  }

  /// <summary>
  /// Adds validation for each item in a collection property.
  /// </summary>
  /// <typeparam name="T">The type being validated.</typeparam>
  /// <typeparam name="TItem">The type of items in the collection.</typeparam>
  /// <param name="validator">The validator instance.</param>
  /// <param name="propertyExpression">Expression selecting the collection property.</param>
  /// <returns>The validator instance for method chaining.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="validator"/> or <paramref name="propertyExpression"/> is null.
  /// </exception>
  /// <remarks>
  /// <para>
  /// This method adds validation for each item in a collection property by applying the
  /// registered validator for the item type. It validates each non-null item using the
  /// appropriate validator.
  /// </para>
  /// <para>
  /// The method will:
  /// <list type="bullet">
  ///   <item><description>Skip validation if the collection property is null</description></item>
  ///   <item><description>Skip null items in the collection</description></item>
  ///   <item><description>Resolve the appropriate validator for the item type</description></item>
  ///   <item><description>Apply validation to each item</description></item>
  ///   <item><description>Prefix property names in errors with the collection property name and index</description></item>
  /// </list>
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// // Validate all addresses in a user's address collection
  /// validator.ValidateEach(user => user.Addresses);
  /// 
  /// // Validate all line items in an order
  /// validator.ValidateEach(order => order.LineItems);
  /// </code>
  /// </para>
  /// </remarks>
  public static Validator<T> ValidateEach<T, TItem>(
    this Validator<T> validator,
    Expression<Func<T, IEnumerable<TItem>?>> propertyExpression)
    where TItem : class {
    ArgumentNullException.ThrowIfNull(validator);
    ArgumentNullException.ThrowIfNull(propertyExpression);

    return validator.AddRule(new CollectionValidationRule<T, TItem>(propertyExpression));
  }
}
