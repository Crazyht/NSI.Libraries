using NSI.Core.Validation;
using NSI.Core.Validation.Abstractions;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Tests.Validation.Rules {
  /// <summary>
  /// Tests for the <see cref="CustomRule{T}"/> and <see cref="AsyncCustomRule{T}"/> validation rules.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests verify custom validation logic execution including
  /// lambda expressions and context access.
  /// </para>
  /// </remarks>
  public sealed class CustomRuleTests {
    private sealed class TestModel {
      public string? Password { get; set; }
      public string? PasswordConfirmation { get; set; }
      public DateTime StartDate { get; set; }
      public DateTime EndDate { get; set; }
    }

    [Fact]
    public void CustomRule_WithValidData_ShouldReturnNoErrors() {
      var rule = new CustomRule<TestModel>((model, _) => {
        if (model.StartDate > model.EndDate) {
          return [
            new ValidationError(
              "INVALID_DATE_RANGE",
              "Start date must be before end date.",
              null,
              null
            )
          ];
        }
        return [];
      });

      var model = new TestModel {
        StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc)
      };
      var context = ValidationContext.Empty();

      var errors = rule.Validate(model, context);

      Assert.Empty(errors);
    }

    [Fact]
    public void CustomRule_WithInvalidData_ShouldReturnErrors() {
      var rule = new CustomRule<TestModel>((model, _) => {
        if (model.StartDate > model.EndDate) {
          return [
            new ValidationError(
              "INVALID_DATE_RANGE",
              "Start date must be before end date.",
              null,
              null
            )
          ];
        }
        return [];
      });

      var model = new TestModel {
        StartDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
      };
      var context = ValidationContext.Empty();

      var errors = rule.Validate(model, context).ToList();

      Assert.Single(errors);
      Assert.Equal("INVALID_DATE_RANGE", errors[0].ErrorCode);
      Assert.Null(errors[0].PropertyName);
    }

    [Fact]
    public void CustomRule_WithContextAccess_ShouldUseContextData() {
      var rule = new CustomRule<TestModel>((model, context) => {
        if (context.Items.TryGetValue("MinPasswordLength", out var minLength) &&
            minLength is int length &&
            model.Password?.Length < length) {
          return [
            new ValidationError(
              "PASSWORD_TOO_SHORT",
              $"Password must be at least {length} characters.",
              "Password",
              length
            )
          ];
        }
        return [];
      });

      var model = new TestModel { Password = "123" };
      var context = ValidationContext.Empty();
      context.Items["MinPasswordLength"] = 8;

      var errors = rule.Validate(model, context).ToList();

      Assert.Single(errors);
      Assert.Equal("PASSWORD_TOO_SHORT", errors[0].ErrorCode);
      Assert.Equal(8, errors[0].ExpectedValue);
    }

    [Fact]
    public async Task AsyncCustomRule_WithAsyncValidation_ShouldExecuteProperly() {
      var rule = new AsyncCustomRule<TestModel>(async (model, _, ct) => {
        await Task.Delay(10, ct);

        if (model.Password != model.PasswordConfirmation) {
          return [
            new ValidationError(
              "PASSWORD_MISMATCH",
              "Passwords do not match.",
              null,
              null
            )
          ];
        }
        return [];
      });

      var model = new TestModel {
        Password = "password123",
        PasswordConfirmation = "different"
      };
      var context = ValidationContext.Empty();

      var errors = (await rule.ValidateAsync(model, context)).ToList();

      Assert.Single(errors);
      Assert.Equal("PASSWORD_MISMATCH", errors[0].ErrorCode);
    }

    [Fact]
    public void Constructor_WithNullValidateFunc_ShouldThrowArgumentNullException() {
      var exception = Assert.Throws<ArgumentNullException>(
        () => new CustomRule<TestModel>(null!)
      );

      Assert.Equal("validateFunc", exception.ParamName);
    }

    [Fact]
    public void AsyncConstructor_WithNullValidateFunc_ShouldThrowArgumentNullException() {
      var exception = Assert.Throws<ArgumentNullException>(
        () => new AsyncCustomRule<TestModel>(null!)
      );

      Assert.Equal("validateFunc", exception.ParamName);
    }
  }
}
