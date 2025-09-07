# GitHub Copilot Instructions - .NET Development Standards

Version: 1.3 (May 2025)

## Table of Contents

1. [Quick Reference](#quick-reference)
2. [Core Principles](#core-principles)
3. [C# Standards](#csharp-standards)
   - [Naming Conventions](#naming-conventions)
   - [Code Style](#code-style)
   - [XML Documentation](#xml-documentation)
   - [Exception Handling](#exception-handling)
4. [Testing Standards](#testing-standards)
   - [Test Structure](#test-structure)
   - [Test Coverage Requirements](#test-coverage-requirements)
   - [Test Documentation](#test-documentation)
5. [Code Quality & Analyzer Compliance](#code-quality--analyzer-compliance)
6. [Logging Standards](#logging-standards)
7. [Code Formatting (.editorconfig)](#code-formatting-editorconfig)
8. [Automated Tools & Validation](#automated-tools--validation)
9. [Troubleshooting & FAQ](#troubleshooting--faq)

---

## Quick Reference

### 🔤 Language Requirement

**ALL documentation (XML Comments, test descriptions) MUST be in ENGLISH**

### 📝 Documentation Coverage

- ✅ All public members must be documented
- ✅ All test classes must have XML documentation
- ✅ Complex private methods should be documented

### 🧪 Testing Requirements

- ✅ Every method needs: Happy path + Error cases + Boundary tests
- ✅ Test naming: `MethodName_Scenario_ExpectedBehavior`
- ✅ No "Arrange/Act/Assert" comments - structure should be self-evident

### 📐 Formatting Basics

- Indentation: 2 spaces (no tabs committed)
- Line endings: LF (Unix)
- Max line length: 100 characters
- Files must end with a newline (CI enforces)
- Trailing whitespace is trimmed (except in Markdown as configured)
- Brace placement: no explicit rule enforced in `.editorconfig` – keep the existing style of the touched file (do not reflow braces arbitrarily). When adding new code prefer the dominant style in that directory.

### 🔍 Analyzer Compliance

- ✅ Use LoggerMessage source generators for all logging
- ✅ Catch specific exceptions or justify generic catches
- ✅ No direct ILogger calls (CA1848)

---

## Core Principles

### 1. **Consistency First**

All code should look like it was written by a single developer. Use these standards without exception.

### 2. **Self-Documenting Code**

- Clear naming > Comments
- Structure > Explanation
- Examples > Descriptions

### 3. **Fail Fast**

- Validate inputs early
- Throw specific exceptions
- Never swallow errors

### 4. **Test Everything**

- No code without tests
- Test behavior, not implementation
- Both positive and negative cases

---

## C# Standards

### Naming Conventions

| Element                   | Convention   | Example                           |
| ------------------------- | ------------ | --------------------------------- |
| **Classes, Records**      | PascalCase   | `UserService`, `OrderRecord`      |
| **Interfaces**            | IPascalCase  | `IRepository`, `IUserService`     |
| **Methods, Properties**   | PascalCase   | `GetUserById()`, `FirstName`      |
| **Parameters, Variables** | camelCase    | `userId`, `itemCount`             |
| **Private Fields**        | \_PascalCase | `_UserName`, `_OrderService`      |
| **Constants**             | PascalCase   | `MaxRetryCount`, `DefaultTimeout` |
| **Generic Parameters**    | TPascalCase  | `TEntity`, `TKey`                 |

### Code Style

#### Braces and Indentation

No global brace rule is enforced by `.editorconfig`. Follow the existing local style of the file you modify. Avoid mixing styles within the same file. (Earlier guidance about forcing K&R has been removed to reflect current configuration.)

#### Base Type / Interface List Spacing

Style: NO space before the colon `:`, exactly one space after it, and one space after each comma.

```csharp
public sealed class SpecificationTranslationTests: IDisposable, IAsyncDisposable {
  // ...
}
```

Disallowed examples:

```csharp
public sealed class SpecificationTranslationTests : IDisposable { }    // space before colon (not allowed)
public sealed class SpecificationTranslationTests:IDisposable { }      // missing space after colon
public sealed class SpecificationTranslationTests: IDisposable,IAsyncDisposable { } // missing space after comma
```

#### Expression-Bodied Members

```csharp
// ✅ Prefer for simple members
public string FullName => $"{_FirstName} {_LastName}";
public void LogMessage(string msg) => _Logger.LogMessage(msg);

// ❌ Avoid for complex logic
public decimal CalculateTotal() => Items.Where(i => i.IsActive)
                                        .Sum(i => i.Price * i.Quantity * (1 - i.Discount));
```

#### Argument Validation

```csharp
public class UserService {
  public void CreateUser(string email, UserProfile profile) {
    // ✅ Validate at method start
    ArgumentException.ThrowIfNullOrEmpty(email);
    ArgumentNullException.ThrowIfNull(profile);

    // Method implementation...
  }

  public void UpdateAge(int age) {
    // ✅ Use specific validation methods
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(age);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(age, 150);

    _Age = age;
  }
}
```

### XML Documentation

#### Basic Structure

```csharp
/// <summary>
/// Brief description ending with a period.
/// </summary>
/// <param name="paramName">Parameter description.</param>
/// <returns>Return value description.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
/// <remarks>
/// <para>
/// Additional details in separate paragraphs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = MyMethod("value");
/// </code>
/// </example>
```

#### Complete Class Example

```csharp
/// <summary>
/// Manages user authentication and session handling.
/// </summary>
/// <remarks>
/// <para>
/// This service implements JWT-based authentication with refresh token support.
/// It integrates with the identity provider for user validation.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Automatic token refresh</description></item>
///   <item><description>Multi-factor authentication support</description></item>
///   <item><description>Session timeout handling</description></item>
/// </list>
/// </para>
/// </remarks>
public class AuthenticationService : IAuthenticationService {
  private readonly ITokenService _TokenService;
  private readonly IUserRepository _UserRepository;

  /// <summary>
  /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
  /// </summary>
  /// <param name="tokenService">Service for JWT token operations.</param>
  /// <param name="userRepository">Repository for user data access.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when any required service is <see langword="null"/>.
  /// </exception>
  public AuthenticationService(ITokenService tokenService, IUserRepository userRepository) {
    _TokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    _UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
  }
}
```

### Exception Handling

#### Do's and Don'ts

```csharp
// ✅ GOOD - Specific exception handling
try {
  await ProcessDataAsync(data);
}
catch (ValidationException ex) {
  _Logger.LogProcessingValidationFailed(data.Id, ex);
  return ValidationResult.Failed(ex.Message);
}
catch (DatabaseException ex) {
  _Logger.LogProcessingDatabaseError(ex);
  throw new ServiceException("Unable to process data", ex);
}

// ❌ BAD - Generic catch
try {
  await ProcessDataAsync(data);
}
catch (Exception ex) {
  // Never catch generic Exception without justification
  return null; // Never swallow exceptions
}
```

---

## Testing Standards

### Test Structure

#### The Given-When-Then Pattern (Without Comments)

```csharp
[Fact]
public async Task SendEmail_WithValidRecipient_ShouldReturnSuccess() {
  // Setup test data and mocks
  var emailService = new EmailService(_mockSmtpClient.Object);
  var email = new Email {
    To = "user@example.com",
    Subject = "Test",
    Body = "Test email"
  };

  _mockSmtpClient
    .Setup(x => x.SendAsync(It.IsAny<MailMessage>()))
    .ReturnsAsync(true);

  // Execute the method
  var result = await emailService.SendAsync(email);

  // Verify the outcome
  Assert.True(result.IsSuccess);
  Assert.Null(result.ErrorMessage);
  _mockSmtpClient.Verify(x => x.SendAsync(It.IsAny<MailMessage>()), Times.Once);
}
```

### Test Coverage Requirements

#### 1. Happy Path (Positive Cases) - MANDATORY

```csharp
[Fact]
public void Calculate_WithStandardInputs_ShouldReturnExpectedResult() {
  var calculator = new TaxCalculator();

  var result = calculator.Calculate(income: 50000, taxRate: 0.25m);

  Assert.Equal(12500m, result);
}
```

#### 2. Error Cases - MANDATORY

```csharp
[Fact]
public void Calculate_WithNegativeIncome_ShouldThrowArgumentException() {
  var calculator = new TaxCalculator();

  var exception = Assert.Throws<ArgumentOutOfRangeException>(
    () => calculator.Calculate(income: -1000, taxRate: 0.25m)
  );

  Assert.Equal("income", exception.ParamName);
  Assert.Contains("Income must be non-negative", exception.Message);
}
```

#### 3. Boundary Cases - MANDATORY

```csharp
[Theory]
[InlineData(0, 0.25, 0)]                    // Minimum boundary
[InlineData(decimal.MaxValue, 0.01, null)] // Maximum boundary - should handle overflow
[InlineData(100000, 0, 0)]                  // Zero tax rate
[InlineData(100000, 1, 100000)]             // 100% tax rate
public void Calculate_WithBoundaryValues_ShouldHandleCorrectly(
  decimal income,
  decimal taxRate,
  decimal? expected) {
  var calculator = new TaxCalculator();

  if (expected.HasValue) {
    var result = calculator.Calculate(income, taxRate);
    Assert.Equal(expected.Value, result);
  } else {
    Assert.Throws<OverflowException>(
      () => calculator.Calculate(income, taxRate)
    );
  }
}
```

#### 4. Edge Cases - MANDATORY

Every method must have tests for:

- **Null inputs** (where applicable)
- **Empty collections** (for methods accepting IEnumerable)
- **Maximum/minimum values** for numeric parameters
- **Invalid states** for stateful objects
- **Concurrent access** for thread-safe classes

### Test Documentation

```csharp
/// <summary>
/// Tests for the <see cref="OrderService"/> business logic.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify order processing workflows including validation,
/// inventory checks, payment processing, and notification sending.
/// </para>
/// <para>
/// Test coverage includes:
/// <list type="bullet">
///   <item><description>Order creation and validation</description></item>
///   <item><description>Inventory availability checks</description></item>
///   <item><description>Payment processing integration</description></item>
///   <item><description>Error handling and rollback scenarios</description></item>
/// </list>
/// </para>
/// </remarks>
public class OrderServiceTests {
  // Test implementation...
}
```

---

## Code Quality & Analyzer Compliance

### Analyzer Integration Requirements

All generated code must pass without warnings from the configured analyzers:

- **Microsoft.CodeAnalysis.NetAnalyzers**
- **SonarAnalyzer.CSharp**
- **Microsoft.CodeAnalysis.Analyzers**

### Common Analyzer Issues & Solutions

#### 1. **CA1848 - Use LoggerMessage Delegates**

```csharp
// ❌ BAD - Direct ILogger calls
_logger.LogDebug("Processing {RequestType}", requestType);
_logger.LogError(ex, "Failed to process {RequestType}", requestType);

// ✅ GOOD - Use LoggerMessage source generators
[LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = "Processing {RequestType}")]
public static partial void LogProcessingRequest(this ILogger logger, string requestType);

_logger.LogProcessingRequest(requestType);
```

#### 2. **S2221 - Exception Catching**

```csharp
// ❌ BAD - Generic Exception catch without justification
try {
  await ProcessAsync();
}
catch (Exception ex) {
  // This will trigger S2221
}

// ✅ GOOD - Specific exceptions
try {
  await ProcessAsync();
}
catch (ValidationException ex) {
  // Handle validation errors
}
catch (DatabaseException ex) {
  // Handle database errors
}

// ✅ ACCEPTABLE - Generic catch with proper suppression
[SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught",
  Justification = "Need to catch all exceptions to prevent notification handler failures from affecting others.")]
public async Task ProcessNotificationAsync() {
  try {
    await ProcessAsync();
  }
  catch (Exception ex) {
    _logger.LogNotificationProcessingError(ex);
    // Don't rethrow - fire-and-forget pattern
  }
}
```

### SuppressMessage Usage Guidelines

#### When to Suppress

- **Legitimate architectural patterns** (e.g., catching all exceptions in notification handlers)
- **Performance-critical code** where the rule doesn't apply
- **Framework integration** where specific patterns are required

#### How to Suppress Properly

```csharp
[SuppressMessage("Category", "RuleId:Rule Description",
  Justification = "Clear explanation of why this rule doesn't apply here.")]
```

**Common legitimate suppressions:**

```csharp
// Fire-and-forget patterns
[SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught",
  Justification = "We want to catch all exceptions to prevent one handler from affecting others.")]

// Defensive programming
[SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught",
  Justification = "We want to catch all exceptions during service resolution to gracefully degrade functionality.")]

// Legacy interop
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
  Justification = "Third-party library can throw various exception types that we cannot predict.")]
```

---

## Logging Standards

### LoggerMessage Source Generators

#### Always Use LoggerMessage for Performance

```csharp
// ✅ Create extension class for your logger methods
public static partial class ILoggerExtensions {

  [LoggerMessage(
    EventId = 1001,
    EventName = "UserCreated",
    Level = LogLevel.Information,
    Message = "User {UserId} created successfully with email {Email}"
  )]
  public static partial void LogUserCreated(this ILogger logger, string userId, string email);

  [LoggerMessage(
    EventId = 1002,
    EventName = "UserCreationFailed",
    Level = LogLevel.Error,
    Message = "Failed to create user with email {Email}"
  )]
  public static partial void LogUserCreationFailed(this ILogger logger, string email, Exception ex);

  [LoggerMessage(
    EventId = 1003,
    EventName = "ValidationFailed",
    Level = LogLevel.Warning,
    Message = "Validation failed for {RequestType}: {ValidationErrors}"
  )]
  public static partial void LogValidationFailed(this ILogger logger, string requestType, string validationErrors);
}
```

#### Usage in Services

```csharp
public class UserService {
  private readonly ILogger<UserService> _logger;

  public async Task<Result<User>> CreateUserAsync(CreateUserRequest request) {
    try {
      var user = await _repository.CreateAsync(request);
      _logger.LogUserCreated(user.Id, user.Email);
      return Result.Success(user);
    }
    catch (ValidationException ex) {
      _logger.LogUserCreationFailed(request.Email, ex);
      return Result.Failure<User>(ex.Message);
    }
  }
}
```

#### EventId Organization

Organize EventIds by feature/component:

```csharp
// User Management: 1000-1099
// Order Processing: 1100-1199
// Payment Processing: 1200-1299
// Notification System: 1300-1399
// Authentication: 1400-1499
```

---

## Code Formatting (.editorconfig)

Below is a distilled view of the active `.editorconfig` entries that affect day‑to‑day authoring. (The real file contains the full, exhaustive ruleset for analyzers and naming.)

```ini
[*]
indent_style = space
indent_size = 2
end_of_line = lf
trim_trailing_whitespace = true
insert_final_newline = true
charset = utf-8
max_line_length = 100

[*.md]
trim_trailing_whitespace = false

[*.cs]
# Private field prefix style
dotnet_naming_style.private_field_prefix_style.capitalization = pascal_case
dotnet_naming_style.private_field_prefix_style.required_prefix = _

# Representative style rules (non‑exhaustive)
csharp_prefer_braces = true:warning
csharp_style_var_when_type_is_apparent = true:warning
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_require_accessibility_modifiers = always:error
dotnet_style_namespace_match_folder = true:error
```

Key additional enforced concepts (see full `.editorconfig` for details):

- No public/protected instance fields (must use properties) – violations error out.
- Private fields must be prefixed with `_` and use PascalCase after the underscore.
- Constant & static readonly fields: PascalCase.
- Analyzer-driven rules enforce explicit accessibility modifiers and namespace ↔ folder alignment.
- Use `var` when the type is apparent or a built-in type.
- Prefer expression-bodied members where the rule severity allows and complexity stays low.
- Parentheses are required for clarity in most binary expressions (`always_for_clarity`).

Brace new-line placement is intentionally not fixed here—match neighboring code.

---

## Automated Tools & Validation

### C# Toolchain

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <!-- Central Package Management -->
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
    <CentralPackageVersionOverrideEnabled>true</CentralPackageVersionOverrideEnabled>
    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" />
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.12.0" />
  </ItemGroup>
</Project>
```

### Pre-commit Hooks

```yaml
# .pre-commit-config.yaml
repos:
  - repo: https://github.com/pre-commit/pre-commit-hooks
    hooks:
      - id: trailing-whitespace
      - id: end-of-file-fixer
      - id: check-merge-conflict

  - repo: local
    hooks:
      - id: dotnet-format
        name: dotnet format
        entry: dotnet format --verify-no-changes
        language: system
        files: \.(cs|csproj)$
```

---

## Troubleshooting & FAQ

### Common Issues

#### Q: "Copilot suggests code that doesn't match our style"

**A:** Ensure the `.github/copilot-instructions.md` file is present and contains these standards. Restart your IDE after adding the file.

#### Q: "Tests are failing due to formatting"

**A:** Run formatters before committing:

```bash
dotnet format
```

#### Q: "Documentation warnings in build"

**A:** Enable XML documentation in your `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

#### Q: "CA1848 warnings for logging"

**A:** Replace all direct ILogger calls with LoggerMessage extensions. Create a `ILoggerExtensions.cs` file with your logging methods.

### Validation Checklist

Before submitting .NET code:

- [ ] All public members have XML documentation
- [ ] Test coverage includes positive, negative, and boundary cases
- [ ] No analyzer warnings (CA1848, S2221, etc.)
- [ ] All logging uses LoggerMessage extensions
- [ ] File ends with newline
- [ ] No trailing whitespace
- [ ] English used in all documentation
- [ ] Test names follow `MethodName_Scenario_ExpectedBehavior` pattern

### Pre-Generation Checklist

Before generating any .NET code, ensure:

1. **Logging Methods**: All log calls use LoggerMessage extensions, not direct ILogger calls
2. **Exception Handling**: Specific exception types or justified suppressions for generic catches
3. **Documentation**: XML documentation for all public members
4. **Validation**: ArgumentNullException.ThrowIfNull() and similar validation methods
5. **Async Patterns**: Proper async/await usage with ConfigureAwait(false) where appropriate

---

## Summary

These standards ensure:

- ✅ **Consistent** code across the team
- ✅ **Maintainable** through clear documentation
- ✅ **Reliable** through comprehensive testing
- ✅ **Quality** through automated validation
- ✅ **Performance** through optimized logging

Remember: **When in doubt, be consistent with existing code!**
