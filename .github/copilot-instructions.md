# GitHub Copilot Instructions - .NET Development Standards

**Version:** 1.0.0  
**Last Updated:** December 2024  
**Target Framework:** .NET 9  
**Repository:** NSI.Libraries by CrazyHT  

---

## 📑 Table of Contents

1. [🎯 Project Overview](#-project-overview)
2. [📋 Code Style & Formatting Standards](#-code-style--formatting-standards)
   - [General Formatting](#general-formatting)
   - [C# Specific Standards](#c-specific-standards)
   - [C# Language Features (Preferred)](#c-language-features-preferred)
   - [Code Quality Rules](#code-quality-rules)
   - [Formatting Preferences](#formatting-preferences)
3. [📦 Project Configuration Standards](#-project-configuration-standards)
   - [MSBuild Properties - Directory.Build.props Management](#msbuild-properties---directorybuildprops-management)
4. [🧪 Testing Standards & Conventions](#-testing-standards--conventions)
   - [Test Project Structure](#test-project-structure)
   - [Test Coverage Requirements](#test-coverage-requirements)
   - [Test Quality Standards](#test-quality-standards)
5. [🧠 Reflection Standards & MethodInfo Resolution](#-reflection-standards--methodinfo-resolution)
   - [Preference Order (Mandatory)](#preference-order-mandatory)
   - [Performance Optimizations & Fail-Fast](#performance-optimizations--fail-fast)
   - [Best Practices & Anti-Patterns](#best-practices--anti-patterns)
6. [🔒 Licensing Considerations](#-licensing-considerations)
7. [🛠️ Entity Framework Specific](#️-entity-framework-specific)
8. [📁 File Organization](#-file-organization)
9. [⚠️ Error vs Warning Guidelines](#️-error-vs-warning-guidelines)
10. [🎯 When Writing Code for This Repository](#-when-writing-code-for-this-repository)
11. [📝 XML Documentation Standards](#-xml-documentation-standards)
    - [Documentation Structure Patterns](#documentation-structure-patterns)
    - [Type-Specific Documentation](#type-specific-documentation)
    - [Documentation Quality Standards](#documentation-quality-standards)
12. [📊 Optimized Logging with LoggerMessage](#-optimized-logging-with-loggermessage)
    - [High-Performance Logging Standards](#high-performance-logging-standards)
    - [LoggerMessage Requirements](#loggermessage-requirements)
    - [Performance Best Practices](#performance-best-practices)
    - [Advanced Patterns & Integration](#advanced-patterns--integration)
13. [🔍 Code Review Checklist](#-code-review-checklist)
    - [Critical Review Areas](#critical-review-areas)
    - [Code Quality Checklist](#code-quality-checklist)
    - [Review Process & Approval Criteria](#review-process--approval-criteria)

---

## 🎯 Project Overview
This is the **NSI.Libraries** repository by CrazyHT, containing .NET libraries with strict coding standards and proprietary licensing.

## 📋 Code Style & Formatting Standards

### General Formatting
- **Indentation**: 2 spaces (NO tabs)
- **Line endings**: LF (Unix-style)
- **Encoding**: UTF-8
- **Max line length**: 100 characters
- **Final newline**: Always required
- **Trim trailing whitespace**: Always

### C# Specific Standards

#### Naming Conventions
- **Classes, Methods, Properties, Events**: `PascalCase`
- **Interfaces**: `IPascalCase` (prefix with 'I')
- **Generic Type Parameters**: `TPascalCase` (prefix with 'T')
- **Private instance fields**: `_PascalCase` (underscore prefix + PascalCase)
- **Private static fields**: `_PascalCase` (underscore prefix + PascalCase)
- **Private static readonly fields**: `PascalCase` (no underscore prefix)
- **Constants**: `PascalCase` (all access levels)
- **Local variables**: `camelCase`
- **Parameters**: `camelCase`
- **Namespaces**: `PascalCase` and must match folder structure

#### Field Rules (Critical)
- ❌ **NO public/protected instance fields allowed** - Use properties instead
- ✅ **Private instance fields**: Must use `_PascalCase` (underscore prefix + PascalCase)
- ✅ **Private static fields**: Must use `_PascalCase` (underscore prefix + PascalCase)
- ✅ **Private static readonly fields**: Must use `PascalCase` (no underscore prefix)
- ✅ **Constants**: `PascalCase` for all access levels
- ✅ **Public/protected static readonly**: `PascalCase`

#### Type Usage Preferences
- **Always use built-in types**: `int` instead of `Int32`, `string` instead of `String`
- **Var usage**: Use `var` everywhere when type is obvious
- **this. qualification**: Never use `this.` unless absolutely necessary
- **Using directives over full names**: Always prefer `using` statements over fully qualified type names
  - ✅ **Preferred**: `private static readonly MethodInfo GetMethod = ...;` with `using System.Reflection;`
  - ❌ **Avoid**: `private static readonly System.Reflection.MethodInfo GetMethod = ...;`

#### Expression & Method Style
- **Expression-bodied members**: Preferred for simple methods, properties, constructors
- **Braces**: Always required, even for single-line statements
- **Pattern matching**: Preferred over `is` with cast and `as` with null check
- **Null checking**: Use null-propagation (`?.`) and null-coalescing (`??`)

#### Namespace & Using Directives
- **File-scoped namespaces**: Always use (`namespace MyNamespace;`)
- **Using placement**: Outside namespace, System directives first
- **No unnecessary imports**: Remove unused using statements
- **Prefer using over full names**: Always add using statements instead of using fully qualified names in code
- **Organize usings**: System namespaces first, then third-party, then project namespaces

### C# Language Features (Preferred)
- **Primary constructors**: Use when appropriate
- **Top-level statements**: Preferred for simple programs
- **Switch expressions**: Preferred over switch statements
- **Index/range operators**: Use `^` and `..` when applicable
- **Implicit object creation**: Use `new()` when type is apparent
- **UTF-8 string literals**: Use when working with UTF-8

### Code Quality Rules

#### Accessibility Modifiers
- **Always explicit**: Every member must have explicit accessibility modifiers
- **Error level**: Missing modifiers cause compilation errors

#### Performance & Best Practices
- **Unused parameters**: Warning level - remove or use discard (`_`)
- **Readonly fields**: Prefer when field is never reassigned
- **Static local functions**: Prefer when function doesn't capture variables
- **Simple using statements**: Use `using var` pattern

#### **📝 Documentation**
- ✅ **XML Comments**: All public members documented
- ✅ **Realistic examples**: Compilable code in examples
- ✅ **Remarks section**: Architecture and patterns explained
- ✅ **Exception documentation**: Exception conditions specified
- ✅ **Cross-references**: `<see cref="">` for related types

#### **🎨 Formatting & Style**
- ✅ **EditorConfig compliance**: All rules respected
- ✅ **Naming conventions**: PascalCase/camelCase/_PascalCase according to rules
- ✅ **File-scoped namespaces**: `namespace Foo.Bar;` used
- ✅ **Using directives**: Sorted, System first, no unused
- ✅ **Expression-bodied members**: For simple cases

#### **🧠 Logic & Correctness**
- ✅ **Edge cases handled**: Null, empty, boundary values
- ✅ **Thread safety**: Concurrent access documented/protected
- ✅ **Business logic correct**: Algorithms validated with tests
- ✅ **Error paths tested**: Failure scenarios covered

### Formatting Preferences

#### Spacing Rules
- **No space after cast**: `(int)value`
- **Space after control flow keywords**: `if (condition)`
- **Space around binary operators**: `a + b`
- **No space before/after dots**: `object.Method()`
- **Space after commas**: `Method(a, b, c)`

#### Brace & Line Break Rules
- **Opening braces**: Same line (`none` style)
- **Single-line blocks**: Preserve when reasonable
- **Single-line statements**: Break into multiple lines
- **Query expressions**: New lines between clauses

#### Indentation Rules
- **Switch labels**: Indented
- **Case contents**: Indented
- **Block contents**: Indented
- **Labels**: Flush left

## 📦 Project Configuration Standards

### **MSBuild Properties - Directory.Build.props Management**

#### **🚫 Properties NOT to Define in .csproj Files**
The following properties are managed globally in `Directory.Build.props` and should **NEVER** be redefined in individual project files unless there's a specific override requirement:

**Core Framework Properties:**
- ❌ `<TargetFramework>net9.0</TargetFramework>` - Managed globally
- ❌ `<Nullable>enable</Nullable>` - Enabled by default for all projects
- ❌ `<ImplicitUsings>enable</ImplicitUsings>` - Enabled by default for all projects

**Analysis & Quality Properties:**
- ❌ `<EnableNETAnalyzers>true</EnableNETAnalyzers>` - Enabled globally
- ❌ `<AnalysisLevel>latest</AnalysisLevel>` - Set globally
- ❌ `<AnalysisMode>All</AnalysisMode>` - Configured globally
- ❌ `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` - Managed by project type
- ❌ `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` - Enabled globally

**Documentation Properties:**
- ❌ `<GenerateDocumentationFile>true</GenerateDocumentationFile>` - Auto-enabled for non-test/non-benchmark projects

#### **✅ Minimal .csproj Structure**
Most project files should be **minimal** and only contain:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- PackageReferences only if needed -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>

  <!-- ProjectReferences only if needed -->
  <ItemGroup>
    <ProjectReference Include="..\NSI.Core\NSI.Core.csproj" />
  </ItemGroup>

  <!-- Global usings only if needed -->
  <ItemGroup>
    <Using Include="SomeNamespace" />
  </ItemGroup>
</Project>
```

#### **🔧 Valid Override Scenarios**
Override global properties **ONLY** when:

- ✅ **Multi-targeting required**: `<TargetFrameworks>net9.0;net8.0</TargetFrameworks>`
- ✅ **Special output types**: `<OutputType>Exe</OutputType>` for console apps
- ✅ **Specific project needs**: Disabling nullable for legacy code migration
- ✅ **Container configuration**: Specific container base images

#### **📂 Automatic Project Type Detection**
Directory.Build.props automatically configures projects based on naming patterns:

**Test Projects** (`.Tests` suffix):
- `<IsPackable>false</IsPackable>`
- `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`
- `<GenerateDocumentationFile>false</GenerateDocumentationFile>`

**Benchmark Projects** (`.Benchmarks` suffix):
- `<IsPackable>false</IsPackable>`
- `<OutputType>Exe</OutputType>`
- `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`

**API Projects** (`.Api` suffix):
- `<OutputType>Exe</OutputType>`
- `<OpenApiDocumentsDirectory>.</OpenApiDocumentsDirectory>`

## 🧪 Testing Standards & Conventions

### **Test Project Structure**

#### **📁 File Organization**
Test files must mirror the source project structure:

```
tests/NSI.Core.Tests/
├── Results/
│   ├── ResultTests.cs              # Tests for Result<T>
│   ├── ResultErrorTests.cs         # Tests for ResultError
│   ├── ResultExtensionsTests.cs    # Tests for Result extensions
│   └── ResultStaticTests.cs        # Tests for static Result methods
├── Validation/
│   ├── ValidatorTests.cs           # Tests for Validator<T>
│   ├── ValidationResultTests.cs    # Tests for ValidationResult
│   └── Rules/
│       ├── RequiredRuleTests.cs    # Tests for RequiredRule<T>
│       ├── EmailRuleTests.cs       # Tests for EmailRule<T>
│       └── StringLengthRuleTests.cs# Tests for StringLengthRule<T>
└── Mediator/
    ├── MediatorTests.cs            # Tests for IMediator
    └── Decorators/
        ├── LoggingDecoratorTests.cs    # Tests for LoggingDecorator<T,R>
        └── ValidationDecoratorTests.cs # Tests for ValidationDecorator<T,R>
```

#### **🏷️ Naming Conventions**

**Test Classes:**
- ✅ `{ClassUnderTest}Tests` - e.g., `ResultTests`, `ValidatorTests`
- ✅ Must be `public` (not sealed unless no inheritance needed)
- ✅ Must have comprehensive XML documentation

**Test Methods:**
- ✅ `{MethodUnderTest}_{Scenario}_{ExpectedOutcome}` pattern
- ✅ Examples:
  - `Success_WithValue_ShouldCreateSuccessResult()`
  - `Validate_WithNullValue_ShouldReturnError()`
  - `Map_WithFailureResult_ShouldPropagateError()`

**Test Data Classes:**
- ✅ Private nested classes: `private sealed class TestModel`
- ✅ Minimal properties for test scenarios only

#### **📊 Test Coverage Requirements**

**✅ Happy Path (OK) Tests:**
```csharp
[Fact]
public void Success_WithValue_ShouldCreateSuccessResult() {
  const int value = 42;
  
  var result = Result.Success(value);
  
  Assert.True(result.IsSuccess);
  Assert.Equal(value, result.Value);
}
```

**⚠️ Edge Case Tests:**
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData(" ")]
[InlineData("\t")]
public void Validate_WithInvalidStringValue_ShouldReturnError(string? value) {
  var rule = new RequiredRule<TestModel>(m => m.StringValue);
  var model = new TestModel { StringValue = value };
  
  var errors = rule.Validate(model, context).ToList();
  
  Assert.Single(errors);
  Assert.Equal("REQUIRED", errors[0].ErrorCode);
}
```

**❌ Failure (KO) Tests:**
```csharp
[Fact]
public void Success_WithNullValue_ShouldThrowArgumentNullException() {
  string? nullValue = null;
  
  var exception = Assert.Throws<ArgumentNullException>(() => Result.Success(nullValue!));
  
  Assert.Equal("value", exception.ParamName);
  Assert.StartsWith("Success value cannot be null", exception.Message, StringComparison.Ordinal);
}
```

#### **🎯 Test Structure Standards**

**AAA Pattern (Arrange-Act-Assert):**
```csharp
[Fact]
public void Method_WithScenario_ShouldExpectedOutcome() {
  // Arrange - Set up test data and dependencies
  var rule = new RequiredRule<TestModel>(m => m.StringValue);
  var model = new TestModel { StringValue = "Valid Value" };
  var context = ValidationContext.Empty();

  // Act - Execute the method under test
  var errors = rule.Validate(model, context);

  // Assert - Verify the expected outcome
  Assert.Empty(errors);
}
```

**Test Method Organization:**
```csharp
public class ResultTests {
  #region Success Result Tests
  
  [Fact]
  public void Success_WithValue_ShouldCreateSuccessResult() { /* ... */ }
  
  [Fact] 
  public void Success_WithNullValue_ShouldThrowArgumentNullException() { /* ... */ }
  
  #endregion
  
  #region Failure Result Tests
  
  [Fact]
  public void Failure_WithError_ShouldCreateFailureResult() { /* ... */ }
  
  #endregion
}
```

#### **📋 Test Quality Checklist**

**Required Coverage:**
- ✅ **All public methods**: Every public method must have tests
- ✅ **All properties**: Property getters/setters tested
- ✅ **All constructors**: Including parameter validation
- ✅ **Exception scenarios**: All thrown exceptions tested
- ✅ **Edge cases**: Null, empty, boundary values
- ✅ **Business rules**: All validation and business logic

**Assertions Standards:**
- ✅ **Specific assertions**: Use `Assert.Equal(expected, actual)` not `Assert.True(expected == actual)`
- ✅ **Exception testing**: Use `Assert.Throws<TException>(() => action)`
- ✅ **String comparisons**: Specify `StringComparison.Ordinal` when needed
- ✅ **Collection testing**: Use `Assert.Single()`, `Assert.Empty()`, `Assert.Collection()`

**Test Data Management:**
- ✅ **Theory tests**: Use `[Theory]` with `[InlineData]` for multiple scenarios
- ✅ **Constants**: Use `const` for test values when possible
- ✅ **No magic values**: Named constants or clear inline values
- ✅ **Isolated tests**: No dependencies between test methods

#### **🚫 Testing Anti-Patterns**

**❌ Avoid These Patterns:**
```csharp
// ❌ Vague test names
[Fact]
public void Test1() { }

// ❌ Testing multiple concerns
[Fact]  
public void TestEverything() {
  // Testing creation, validation, and transformation in one test
}

// ❌ Magic numbers without context
[Fact]
public void TestMethod() {
  var result = Calculate(42, 17, 3.14159); // What do these numbers represent?
}

// ❌ Implicit assertions
[Fact]
public void TestMethod() {
  var result = SomeMethod();
  // No assertions - test passes but verifies nothing
}
```

**✅ Preferred Patterns:**
```csharp
// ✅ Clear, descriptive test names
[Fact]
public void Calculate_WithValidInputs_ShouldReturnExpectedResult() { }

// ✅ Single concern per test
[Fact]
public void Constructor_WithNullParameter_ShouldThrowArgumentNullException() { }

// ✅ Named constants
[Fact]
public void TestMethod() {
  const int validAge = 25;
  const decimal standardRate = 0.15m;
  var result = Calculate(validAge, standardRate);
}

// ✅ Explicit assertions
[Fact]
public void TestMethod() {
  var result = SomeMethod();
  
  Assert.NotNull(result);
  Assert.True(result.IsValid);
  Assert.Equal(expectedValue, result.Value);
}
```

#### **🔧 Test Configuration**

**Dependencies:**
- ✅ **xUnit**: Primary test framework (`<Using Include="Xunit" />`)
- ✅ **NSubstitute**: Mocking framework for dependencies
- ✅ **NSI.Testing**: Internal testing utilities and helpers

**Project References:**
- ✅ **Source project**: Reference to the project being tested
- ✅ **NSI.Testing**: For testing utilities (MockLogger, etc.)
- ✅ **Microsoft.Extensions.DependencyInjection**: For DI testing scenarios

## 🧠 Reflection Standards & MethodInfo Resolution

### **Preference Order (Mandatory)**

**1. Expressions + Dedicated Helper** ✅ **BEST COMPROMISE** (safety/readability)
```csharp
var methodInfo = MI.Of(() => SomeClass.SomeMethod(default!, default!));
```

**2. Inline Expressions** ✅ **SAFE** ⛔️ **Verbose**
```csharp
Expression<Action> expr = () => SomeClass.SomeMethod(default!, default!);
var methodInfo = ((MethodCallExpression)expr.Body).Method;
```

**3. Cast to Typed Delegate** ✅ **Concise** ⛔️ **Less obvious**
```csharp
var methodInfo = ((Func<string, int, bool>)SomeClass.SomeMethod).Method;
```

**4. Reflection by Name** ⛔️ **LAST RESORT** (dynamic only)
```csharp
var methodInfo = typeof(SomeClass).GetMethod(nameof(SomeClass.SomeMethod), new[] { typeof(string), typeof(int) })!;
```

**🚫 STRICT RULE**: If you can use 1 or 2, NEVER use 3 or 4.

### **Recommended MI Helper**

Add this helper **once** in the Infrastructure (or Common) project :

```csharp
using System.Linq.Expressions;
using System.Reflection;

public static class MI {
  public static MethodInfo Of(Expression<Action> e) =>
    ((MethodCallExpression)e.Body).Method;

  public static MethodInfo Of<T>(Expression<Action<T>> e) =>
    ((MethodCallExpression)e.Body).Method;

  public static MethodInfo Of<TResult>(Expression<Func<TResult>> e) =>
    ((MethodCallExpression)e.Body).Method;

  public static MethodInfo Of<T, TResult>(Expression<Func<T, TResult>> e) =>
    ((MethodCallExpression)e.Body).Method;

  // Overload for cases with 2 input parameters
  public static MethodInfo Of<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> e) =>
    ((MethodCallExpression)e.Body).Method;

  // Overload for cases with 3 input parameters  
  public static MethodInfo Of<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> e) =>
    ((MethodCallExpression)e.Body).Method;
}
```

### **📊 Performance Optimizations & Fail-Fast**

#### **🚀 Mandatory Static Cache**
**ALWAYS** store MethodInfo in static readonly fields to avoid re-resolution:

```csharp
public static class KnownMethods {
  // ✅ EXCELLENT - Resolved once at class loading time
  public static readonly MethodInfo StringIndexOf = MI.Of<string, char, int>(s => s.IndexOf(default!));
  public static readonly MethodInfo ListAdd = MI.Of<List<string>, string>(l => l.Add(default!));
  
  // ❌ FORBIDDEN - Recalculated on every call
  public MethodInfo GetStringIndexOf() => MI.Of<string, char, int>(s => s.IndexOf(default!));
}
```

#### **⚡ Grouped Static Initialization**
Organize MethodInfo by module with fail-fast initialization :

```csharp
public static class EfCoreMethods {
  public static readonly MethodInfo ILike;
  public static readonly MethodInfo Contains;
  public static readonly MethodInfo StartsWith;
  
  static EfCoreMethods() {
    try {
      ILike = MI.Of(() => NpgsqlDbFunctionsExtensions.ILike(default!, default!, default!));
      Contains = MI.Of<string, string, bool>(s => s.Contains(default!));
      StartsWith = MI.Of<string, string, bool>(s => s.StartsWith(default!));
    }
    catch (Exception ex) {
      throw new TypeInitializationException(nameof(EfCoreMethods), ex);
    }
  }
}
```

#### **🛡️ Compile-Time Validation with Tests**
Create unit tests to validate MethodInfo references :

```csharp
[TestClass]
public class MethodInfoValidationTests {
  [TestMethod]
  public void AllKnownMethodsAreValid() {
    // Forces initialization and fails if a method doesn't exist
    Assert.IsNotNull(EfCoreMethods.ILike);
    Assert.IsNotNull(EfCoreMethods.Contains);
    Assert.IsNotNull(EfCoreMethods.StartsWith);
  }
  
  [TestMethod] 
  public void MethodSignaturesAreCorrect() {
    var iLikeMethod = EfCoreMethods.ILike;
    var parameters = iLikeMethod.GetParameters();
    
    Assert.AreEqual(3, parameters.Length);
    Assert.AreEqual(typeof(DbFunctions), parameters[0].ParameterType);
    Assert.AreEqual(typeof(string), parameters[1].ParameterType);
    Assert.AreEqual(typeof(string), parameters[2].ParameterType);
  }
}
```

### **📁 Structural Organization**

#### **🗂️ Domain Separation**
```
Infrastructure/
├── Reflection/
│   ├── MI.cs                    // Main helper
│   ├── CoreMethods.cs           // .NET Core MethodInfo
│   ├── EfCoreMethods.cs         // Entity Framework MethodInfo
│   ├── LinqMethods.cs           // LINQ MethodInfo
│   └── CustomMethods.cs         // Project-specific MethodInfo
```

#### **🔍 Descriptive Constant Naming**
```csharp
public static class LinqMethods {
  // ✅ EXCELLENT - Explicit name with context
  public static readonly MethodInfo EnumerableWhere = MI.Of(() => 
    Enumerable.Where<object>(default!, default!));
  
  public static readonly MethodInfo QueryableSelect = MI.Of(() => 
    Queryable.Select<object, object>(default!, default!));
  
  // ❌ AVOID - Too generic
  public static readonly MethodInfo Where = ...;
  public static readonly MethodInfo Select = ...;
}
```

### **MI Helper Usage Examples**
```csharp
// 1) Simple static method
var methodInfo = MI.Of(() => SomeClass.SomeStatic(default!, default!));

// 2) Instance method
var methodInfo = MI.Of<string, int>(s => s.IndexOf(default!));

// 3) Extension method (e.g. EF Core / Npgsql)
var methodInfo = MI.Of(() => NpgsqlDbFunctionsExtensions.ILike(default!, default!, default!));

// 4) Closed generic method
var methodInfo = MI.Of(() => Enumerable.Contains<int>(default!, default));

// 5) Async method (targeting the method, not the Task)
var methodInfo = MI.Of(() => MyService.DoWorkAsync(default!));
```

⚠️ **ALWAYS use `default!`** to fill parameters in expressions, to avoid allocations and appease nullable analysis.

### **🎯 Recommended Performance Patterns**

#### **🔄 Lazy Loading with Validation**
For complex cases requiring deferred initialization:

```csharp
public static class ExpensiveMethods {
  private static readonly Lazy<MethodInfo> _complexMethod = new(() => {
    try {
      return MI.Of(() => SomeComplexClass.ExpensiveMethod(default!, default!));
    }
    catch (Exception ex) {
      throw new InvalidOperationException($"Failed to resolve {nameof(ExpensiveMethod)}", ex);
    }
  });
  
  public static MethodInfo ComplexMethod => _complexMethod.Value;
}
```

#### **📝 Systematic Documentation**
```csharp
public static class DocumentedMethods {
  /// <summary>
  /// MethodInfo for NpgsqlDbFunctionsExtensions.ILike(DbFunctions, string, string)
  /// Used for LINQ queries with PostgreSQL case-insensitive pattern matching
  /// </summary>
  /// <remarks>
  /// Signature: bool ILike(this DbFunctions functions, string matchExpression, string pattern)
  /// Validated in: MethodInfoValidationTests.ValidateILikeSignature()
  /// </remarks>
  public static readonly MethodInfo PostgresILike = MI.Of(() => 
    NpgsqlDbFunctionsExtensions.ILike(default!, default!, default!));
}
```

## 🔒 Licensing Considerations
This repository uses a **proprietary restrictive license**. When suggesting code:
- Never suggest copying code from external sources without explicit permission
- Prefer standard .NET patterns and original implementations
- Document any external library suggestions for explicit approval

## 🛠️ Entity Framework Specific
- **Migrations**: Auto-generated code, exclude from style rules
- **CA2012 disabled**: GetResult() on ValueTask is accepted pattern for sync bridges

## 📁 File Organization
- **Generated code**: Coverage reports and EF migrations excluded from style rules
- **Namespace-folder matching**: Required and enforced as error
- **File-scoped namespaces**: Mandatory

## ⚠️ Error vs Warning Guidelines

### **Errors (Build-breaking)**:
- Missing accessibility modifiers
- Public instance fields
- Namespace not matching folder structure
- File-scoped namespace not used

### **Warnings (Should fix)**:
- Naming convention violations
- Missing expression-bodied members
- Unused parameters
- Non-preferred language patterns

### **Suggestions (Optional)**:
- Pattern matching improvements
- Null-checking enhancements
- Performance optimizations

## 🎯 When Writing Code for This Repository

1. **Always** start with file-scoped namespaces
2. **Always** use explicit accessibility modifiers
3. **Never** create public fields - use properties
4. **Prefer** expression-bodied members for simple operations
5. **Use** var for local variables when type is obvious
6. **Follow** the exact naming conventions (especially `_PascalCase` for private fields, `PascalCase` for private static readonly)
7. **Keep** lines under 100 characters
8. **Maintain** consistent 2-space indentation
9. **Apply** modern C# language features when appropriate
10. **Remember** this is proprietary code - suggest original implementations
11. **Always prefer using directives** over fully qualified type names in code
12. **Use MI helper for MethodInfo resolution** - follow the strict preference order

## 📝 XML Documentation Standards

### General XML Comments Requirements

#### **Mandatory Documentation**
- **All public classes, interfaces, and members**: Must have XML documentation
- **All protected members**: Must have XML documentation  
- **Complex internal members**: Should have XML documentation
- **Private members**: Optional but recommended for complex logic

#### **Documentation Structure Pattern**

**Standard Class Documentation:**
```csharp
/// <summary>
/// Brief one-line description of what this class/interface does.
/// </summary>
/// <typeparam name="T">Description of generic type parameter and its constraints.</typeparam>
/// <remarks>
/// <para>
/// Detailed explanation of the class purpose, architectural patterns used,
/// and how it fits into the broader system design.
/// </para>
/// <para>
/// Key features or responsibilities:
/// <list type="bullet">
///   <item><description>First key responsibility or feature</description></item>
///   <item><description>Second key responsibility or feature</description></item>
///   <item><description>Third key responsibility or feature</description></item>
/// </list>
/// </para>
/// <para>
/// Additional important information about usage patterns, thread-safety,
/// performance considerations, or integration points.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Complete usage example showing realistic scenarios
/// var validator = new Validator&lt;User&gt;()
///   .AddRule(new RequiredRule&lt;User&gt;(u => u.Email))
///   .AddRule(new EmailRule&lt;User&gt;(u => u.Email));
/// 
/// var result = await validator.ValidateAsync(user);
/// if (!result.IsValid) {
///   throw new ValidationException(result);
/// }
/// </code>
/// </example>
public class ExampleClass<T> { }
```

**Method Documentation Pattern:**
```csharp
/// <summary>
/// Brief description of what the method does and its primary purpose.
/// </summary>
/// <typeparam name="TResult">Description of generic return type.</typeparam>
/// <param name="request">Description of parameter and its constraints.</param>
/// <param name="cancellationToken">Standard cancellation token description.</param>
/// <returns>
/// Detailed description of what is returned, including success/failure scenarios.
/// For Task&lt;Result&lt;T&gt;&gt;, specify both the Task and Result behavior.
/// </returns>
/// <exception cref="ArgumentNullException">When specific parameter is null.</exception>
/// <exception cref="InvalidOperationException">When operation is called in invalid state.</exception>
/// <remarks>
/// <para>
/// Detailed explanation of method behavior, including:
/// <list type="bullet">
///   <item><description>Step-by-step process description</description></item>
///   <item><description>Side effects or state changes</description></item>
///   <item><description>Performance characteristics</description></item>
///   <item><description>Thread-safety considerations</description></item>
/// </list>
/// </para>
/// <para>
/// Special behavior or edge cases that developers should be aware of.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await mediator.ProcessAsync(new GetUserQuery(userId), ct);
/// return result.Match(
///   onSuccess: user => Ok(user),
///   onFailure: error => BadRequest(error)
/// );
/// </code>
/// </example>
public async Task<Result<TResult>> ProcessAsync<TResult>(...) { }
```

#### **Key Documentation Elements**

**1. Summary Section (`<summary>`)**
- ✅ **Brief and precise**: One sentence maximum for simple members
- ✅ **Action-oriented**: Use active voice ("Validates", "Creates", "Processes")
- ✅ **No implementation details**: Focus on what, not how
- ❌ **Avoid redundancy**: Don't repeat the method/class name

**2. Remarks Section (`<remarks>`)**  
- ✅ **Multi-paragraph structure**: Use `<para>` tags for organization
- ✅ **Bulleted lists**: Use for features, responsibilities, or key points
- ✅ **Architectural context**: Explain how the component fits in the system
- ✅ **Usage patterns**: Describe intended usage scenarios
- ✅ **Performance notes**: Include timing, complexity, or resource usage
- ✅ **Thread-safety**: Explicitly state thread-safety guarantees

**3. Parameter Documentation (`<param>`)**
- ✅ **Purpose description**: What the parameter represents
- ✅ **Constraints**: Valid ranges, formats, or requirements  
- ✅ **Nullable behavior**: Explicitly state if null is allowed
- ✅ **Default behavior**: Describe what happens with default values

**4. Return Value Documentation (`<returns>`)**
- ✅ **Success scenarios**: What is returned on success
- ✅ **Failure scenarios**: What is returned on failure  
- ✅ **Result pattern**: For `Result<T>`, describe both success and error cases
- ✅ **Task behavior**: For async methods, describe task completion

**5. Exception Documentation (`<exception>`)**
- ✅ **Standard exceptions**: Always document ArgumentNullException, ArgumentException
- ✅ **Business exceptions**: Document domain-specific exceptions
- ✅ **Conditions**: Clearly state when each exception is thrown
- ✅ **Exception types**: Use `cref` for proper type linking

**6. Example Section (`<example>`)**
- ✅ **Realistic scenarios**: Show actual usage patterns
- ✅ **Complete code**: Include necessary imports and context
- ✅ **Multiple scenarios**: Show different use cases when applicable
- ✅ **Error handling**: Demonstrate proper error handling patterns

#### **Type-Specific Documentation Patterns**

**Interface Documentation:**
```csharp
/// <summary>
/// Defines the contract for [specific functionality].
/// </summary>
/// <remarks>
/// <para>
/// This interface provides [architectural pattern] and is typically implemented
/// by [implementation patterns]. It ensures [specific guarantees].
/// </para>
/// <para>
/// Implementing classes should:
/// <list type="bullet">
///   <item><description>Requirement 1 with specific behavior</description></item>
///   <item><description>Requirement 2 with constraints</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IExampleService { }
```

**Result Pattern Documentation:**
```csharp
/// <summary>
/// Creates a successful result with the specified value.
/// </summary>
/// <param name="value">The success value.</param>
/// <returns>A successful Result containing the value.</returns>
/// <exception cref="ArgumentNullException">Thrown when value is null for reference types.</exception>
/// <example>
/// <code>
/// var result = Result.Success(42);
/// var stringResult = Result.Success("Hello, World!");
/// </code>
/// </example>
public static Result<T> Success<T>(T value) => new(value);
```

**Validation Rule Documentation:**
```csharp
/// <summary>
/// Validates that a property value meets [specific criteria].
/// </summary>
/// <typeparam name="T">The type of object being validated.</typeparam>
/// <remarks>
/// <para>
/// This validation rule checks [specific validation logic].
/// It handles different types of properties:
/// <list type="bullet">
///   <item><description>For string properties, [string-specific behavior]</description></item>
///   <item><description>For reference types, [reference-specific behavior]</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ExampleRule<T> : IValidationRule<T> { }
```

#### **Documentation Quality Standards**

**Language and Style:**
- ✅ **Present tense**: "Validates", "Returns", "Throws"
- ✅ **Third person**: Avoid "you" or "we"
- ✅ **Precise terminology**: Use domain-specific terms consistently
- ✅ **Complete sentences**: End with periods
- ✅ **Active voice**: "The method validates" not "Validation is performed"

**Content Quality:**
- ✅ **Value-added**: Don't just repeat what's obvious from the signature
- ✅ **Context-aware**: Reference related types and patterns using `<see cref="Type"/>`
- ✅ **Comprehensive**: Cover all important behaviors and edge cases
- ✅ **Maintainable**: Update documentation when code changes

**Technical Accuracy:**
- ✅ **Type safety**: Use `<typeparam>` for all generic parameters
- ✅ **Null handling**: Document nullable reference type behavior
- ✅ **Async patterns**: Document cancellation token usage and ConfigureAwait
- ✅ **Result patterns**: Distinguish between different result states

#### **Code Reference Standards**

**Cross-References (`<see cref="..."/>`)**:
```csharp
/// <summary>
/// Processes requests using the <see cref="IMediator"/> pattern.
/// </summary>
/// <seealso cref="IRequestHandler{TRequest, TResponse}"/>
/// <seealso cref="Result{T}"/>
```

**Parameter References:**
```csharp
/// <summary>
/// Validates the specified <paramref name="instance"/> using configured rules.
/// </summary>
/// <param name="instance">The object to validate.</param>
```

**Code Blocks in Examples:**
- ✅ **Proper escaping**: Use `&lt;` and `&gt;` for generic brackets in XML
- ✅ **Syntax highlighting**: Use `<code>` tags for code blocks
- ✅ **Realistic examples**: Show actual usage patterns, not toy examples
- ✅ **Error handling**: Include proper error handling in examples

#### **Anti-Patterns to Avoid**

**❌ Poor Documentation:**
```csharp
/// <summary>
/// Gets or sets the value. // Too obvious, adds no value
/// </summary>
public string Value { get; set; }

/// <summary>
/// This method does validation. // Vague, no specifics
/// </summary>
public void Validate() { }

/// <summary>
/// Constructor. // States the obvious
/// </summary>
public MyClass() { }
```

**✅ Better Documentation:**
```csharp
/// <summary>
/// Gets or sets the user's email address for authentication and communication.
/// </summary>
/// <value>A valid email address format, or null if not provided.</value>
public string? EmailAddress { get; set; }

/// <summary>
/// Validates the user instance against all configured validation rules.
/// </summary>
/// <returns>A validation result containing any errors found.</returns>
public ValidationResult ValidateUser() { }

/// <summary>
/// Initializes a new instance with default validation rules and empty error collection.
/// </summary>
public UserValidator() { }
```

#### **Documentation Validation**

**Required Tools:**
- ✅ **Enable XML documentation generation**: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- ✅ **Treat missing docs as warnings**: Configure in .editorconfig
- ✅ **Use DocFX or Sandcastle**: For documentation site generation
- ✅ **IDE integration**: Leverage IntelliSense and quick info

**Review Checklist:**
- ✅ **All public API documented**: No missing XML comments  
- ✅ **Examples compile**: Verify example code in documentation
- ✅ **Links resolve**: All `<see cref="..."/>` references are valid
- ✅ **Grammar and spelling**: Use tools like spell checkers
- ✅ **Consistency**: Follow established patterns across the codebase

## 📊 Optimized Logging with LoggerMessage

### **High-Performance Logging Standards**

#### **🚀 LoggerMessage Source Generators (Mandatory)**
All logging must use LoggerMessage source generators instead of traditional string interpolation or composite formatting. This provides:

- **Zero allocation** logging for most scenarios
- **Compile-time optimization** with pre-generated delegates
- **Type safety** with compile-time validation
- **Consistent event IDs** and structured logging
- **Better performance** (10x-100x faster than traditional logging)

#### **✅ Preferred LoggerMessage Pattern**
```csharp
/// <summary>
/// High-performance logging extensions using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// This class provides compiled logging methods that avoid boxing and string allocation
/// overhead compared to traditional ILogger extension calls. All methods use source
/// generated LoggerMessage delegates for optimal performance.
/// </para>
/// </remarks>
internal static partial class ILoggerExtensions {

  [LoggerMessage(
    EventId = 1,
    EventName = "ProcessingStarted", 
    Level = LogLevel.Information,
    Message = "Processing request: {RequestType} (CorrelationId: {CorrelationId})"
  )]
  public static partial void LogProcessingStarted(this ILogger logger, string requestType, string correlationId);

  [LoggerMessage(
    EventId = 2,
    EventName = "ProcessingCompleted",
    Level = LogLevel.Information, 
    Message = "Request {RequestType} completed in {ElapsedMs}ms"
  )]
  public static partial void LogProcessingCompleted(this ILogger logger, string requestType, long elapsedMs);

  [LoggerMessage(
    EventId = 3,
    EventName = "ProcessingFailed",
    Level = LogLevel.Warning,
    Message = "Request {RequestType} failed: {ErrorCode} - {ErrorMessage}"
  )]
  public static partial void LogProcessingFailed(this ILogger logger, string requestType, string errorCode, string errorMessage);

  [LoggerMessage(
    EventId = 4,
    EventName = "ProcessingException", 
    Level = LogLevel.Error,
    Message = "Exception processing request {RequestType}"
  )]
  public static partial void LogProcessingException(this ILogger logger, string requestType, Exception ex);
}
```

#### **📋 LoggerMessage Requirements**

**Event ID Organization:**
- ✅ **Structured ranges**: Organize by functional area (1-19: Core, 20-29: Validation, etc.)
- ✅ **Sequential numbering**: No gaps, predictable progression
- ✅ **Documented ranges**: Comment ranges with their purpose
- ✅ **Unique across assembly**: No duplicate EventIds within the same assembly

**Message Template Standards:**
- ✅ **Structured parameters**: Use `{ParameterName}` for all dynamic content
- ✅ **Consistent naming**: PascalCase for parameter names (`{RequestType}`, `{ElapsedMs}`)
- ✅ **No string interpolation**: Never use `$"..."` in Message templates
- ✅ **Semantic names**: Parameters should describe their semantic meaning

**Method Naming Convention:**
- ✅ **Prefix**: Always start with `Log` 
- ✅ **Descriptive**: Clear indication of what is being logged (`LogProcessingStarted`)
- ✅ **PascalCase**: Follow standard C# method naming conventions
- ✅ **Verb-based**: Use action verbs (Started, Completed, Failed, etc.)

#### **🎯 Event ID Organization Standard**

```csharp
/// <summary>
/// Event ID organization:
/// <list type="bullet">
///   <item><description>1-19: Core Processing</description></item>
///   <item><description>20-29: Validation System</description></item>
///   <item><description>30-39: Security & Authentication</description></item>
///   <item><description>40-49: Data Access</description></item>
///   <item><description>50-59: Health Checks</description></item>
///   <item><description>60-69: Pipeline System</description></item>
///   <item><description>70-79: Performance & Metrics</description></item>
///   <item><description>80-89: Service Registration</description></item>
///   <item><description>90-99: Configuration & Startup</description></item>
/// </list>
/// </summary>
internal static partial class ServiceLoggerExtensions {
  
  #region Core Processing (EventId 1-19)
  
  [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Service started")]
  public static partial void LogServiceStarted(this ILogger logger);
  
  [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Processing item: {ItemId}")]
  public static partial void LogProcessingItem(this ILogger logger, string itemId);
  
  #endregion
  
  #region Validation System (EventId 20-29)
  
  [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "Validation failed: {ErrorMessage}")]
  public static partial void LogValidationFailed(this ILogger logger, string errorMessage);
  
  #endregion
}
```

#### **⚡ Performance Best Practices**

**Parameter Types:**
- ✅ **Primitive types**: `int`, `long`, `string`, `bool` are optimal
- ✅ **DateTime/TimeSpan**: Use these directly, they're optimized
- ✅ **Enums**: Logged efficiently without boxing
- ⚠️ **Custom objects**: Implement `ToString()` carefully or use specific properties

**Exception Handling:**
```csharp
// ✅ EXCELLENT - Exception parameter at the end
[LoggerMessage(
  EventId = 4,
  Level = LogLevel.Error,
  Message = "Failed to process {RequestType} for user {UserId}"
)]
public static partial void LogProcessingException(this ILogger logger, string requestType, string userId, Exception ex);

// ❌ AVOID - Exception not at the end, suboptimal parameter order
[LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error occurred")]
public static partial void LogError(this ILogger logger, Exception ex, string context);
```

**Conditional Logging:**
```csharp
// ✅ EXCELLENT - Check IsEnabled for expensive operations
public static void LogComplexOperation(this ILogger logger, ComplexObject obj) {
  if (!logger.IsEnabled(LogLevel.Debug)) {
    return;
  }
  
  // Only perform expensive serialization if logging is enabled
  var serialized = JsonSerializer.Serialize(obj);
  logger.LogComplexOperationInternal(serialized);
}

[LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Complex operation: {Data}")]
private static partial void LogComplexOperationInternal(this ILogger logger, string data);
```

#### **🚫 Anti-Patterns to Avoid**

**❌ Traditional String Interpolation:**
```csharp
// ❌ FORBIDDEN - Causes allocations and boxing
logger.LogInformation($"Processing request {requestType} took {elapsed}ms");

// ❌ FORBIDDEN - String.Format causes allocations  
logger.LogInformation("Processing request {0} took {1}ms", requestType, elapsed);

// ❌ FORBIDDEN - Composite formatting in LoggerMessage
[LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = $"Processing {requestType}")]
public static partial void LogBadExample(this ILogger logger, string requestType);
```

**✅ Correct LoggerMessage Usage:**
```csharp
// ✅ EXCELLENT - Zero allocation, compile-time optimized
[LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing request {RequestType} took {ElapsedMs}ms")]
public static partial void LogProcessingTime(this ILogger logger, string requestType, long elapsedMs);

// Usage
logger.LogProcessingTime(request.GetType().Name, stopwatch.ElapsedMilliseconds);
```

#### **📝 Advanced LoggerMessage Patterns**

**Scoped Logging with Correlation:**
```csharp
[LoggerMessage(
  EventId = 21,
  Level = LogLevel.Information,
  Message = "Starting {OperationType} for {EntityType} {EntityId} (CorrelationId: {CorrelationId})"
)]
public static partial void LogOperationStarted(
  this ILogger logger, 
  string operationType, 
  string entityType, 
  string entityId, 
  string correlationId);

// Usage with scoped context
using (logger.BeginScope(new Dictionary<string, object> {
  ["CorrelationId"] = correlationId,
  ["UserId"] = userId,
  ["Operation"] = "DataProcessing"
})) {
  logger.LogOperationStarted("Create", "User", userId, correlationId);
  // ... processing
}
```

**Result Pattern Integration:**
```csharp
[LoggerMessage(
  EventId = 31,
  Level = LogLevel.Warning,
  Message = "Operation failed: {ErrorType} - {ErrorCode}: {ErrorMessage}"
)]
public static partial void LogOperationFailed(
  this ILogger logger, 
  ErrorType errorType, 
  string errorCode, 
  string errorMessage);

// Extension for Result integration
public static void LogResult<T>(this ILogger logger, Result<T> result, string operationName) {
  if (result.IsSuccess) {
    logger.LogOperationSucceeded(operationName);
  } else {
    logger.LogOperationFailed(result.Error.Type, result.Error.Code, result.Error.Message);
  }
}
```

**Performance Monitoring:**
```csharp
[LoggerMessage(
  EventId = 71,
  Level = LogLevel.Information,
  Message = "Slow operation detected: {OperationName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)"
)]
public static partial void LogSlowOperation(
  this ILogger logger, 
  string operationName, 
  long elapsedMs, 
  long thresholdMs);

[LoggerMessage(
  EventId = 72,
  Level = LogLevel.Debug,
  Message = "Memory usage after {OperationName}: {MemoryMB}MB (Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2})"
)]
public static partial void LogMemoryUsage(
  this ILogger logger, 
  string operationName, 
  long memoryMB, 
  int gen0, 
  int gen1, 
  int gen2);
```

#### **🔧 Configuration & Setup**

**Logger Extension Organization:**
```csharp
// File: ILoggerExtensions.cs - One file per major component/service
namespace MyService.Logging;

/// <summary>
/// High-performance logging extensions for MyService using LoggerMessage source generators.
/// </summary>
internal static partial class ILoggerExtensions {
  // All LoggerMessage methods here
}
```

**Registration & Usage:**
```csharp
// In Program.cs or Startup.cs
services.AddLogging(builder => {
  builder.AddConsole();
  builder.AddApplicationInsights(); 
  builder.SetMinimumLevel(LogLevel.Information);
});

// In service classes
public class MyService {
  private readonly ILogger<MyService> _logger;
  
  public MyService(ILogger<MyService> logger) => _logger = logger;
  
  public async Task ProcessAsync(Request request) {
    _logger.LogProcessingStarted(request.Type, request.CorrelationId);
    
    try {
      // Processing logic
      _logger.LogProcessingCompleted(request.Type, stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex) {
      _logger.LogProcessingException(request.Type, ex);
      throw;
    }
  }
}
```

#### **📊 Performance Benchmarks**

**Expected Performance Improvements:**
- **Traditional logging**: ~1000ns per log call
- **LoggerMessage**: ~100ns per log call  
- **Memory allocations**: 90% reduction in heap allocations
- **GC pressure**: Significantly reduced garbage collection

**Benchmark Template:**
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class LoggingBenchmarks {
  private ILogger<LoggingBenchmarks> _logger;

  [GlobalSetup]
  public void Setup() {
    _logger = NullLogger<LoggingBenchmarks>.Instance;
  }

  [Benchmark(Baseline = true)]
  public void TraditionalLogging() {
    var requestType = "TestRequest";
    var elapsed = 150L;
    _logger.LogInformation($"Processing {requestType} took {elapsed}ms");
  }

  [Benchmark]  
  public void LoggerMessageLogging() {
    _logger.LogProcessingCompleted("TestRequest", 150L);
  }
}
```

#### **🎯 Code Review Checklist for Logging**

**✅ LoggerMessage Compliance:**
- All logging uses LoggerMessage source generators
- Event IDs are unique and organized by functional area
- Message templates use structured parameters
- Method names follow `Log{Action}` convention
- Exception parameters are placed at the end
- No string interpolation or composite formatting

**✅ Performance Optimization:**
- IsEnabled checks for expensive logging operations
- Minimal parameter count (ideally < 6 parameters)
- Primitive types used where possible
- No unnecessary ToString() calls in hot paths

**✅ Structured Logging:**
- Consistent parameter naming across the application
- Semantic parameter names that describe the data
- Correlation IDs and scoped context used appropriately
- Log levels appropriate for the message importance

## 🔍 Code Review Checklist

### **Critical Review Areas**

#### **🚫 Message Suppression Policy (CRITICAL)**

**Suppression Rules - ZERO TOLERANCE:**
- ❌ **FORBIDDEN**: Global suppressions at assembly level (`[assembly: SuppressMessage(...)]`)
- ❌ **FORBIDDEN**: Suppressions without detailed justification
- ❌ **FORBIDDEN**: Suppressions "out of laziness" or "to silence the analyzer"
- ✅ **MANDATORY**: Explicit business/technical justification for each suppression
- ✅ **PREFERRED**: `#pragma warning disable` with limited scope vs `[SuppressMessage(...)]`

**Acceptable Suppression Pattern:**
```csharp
// ✅ EXCELLENT - Clear business justification + limited scope
public static Result<T> Try<T>(Func<T> operation) {
  try {
    return Success(operation());
  }
#pragma warning disable CA1031 // Do not catch general exception types
  catch (Exception ex) { // Intentionally catching all exceptions for Result pattern
#pragma warning restore CA1031
    return Failure<T>(new ResultError(ErrorType.Generic, "EXCEPTION", ex.Message, ex));
  }
}

// ✅ ACCEPTABLE - Valid technical justification
[SuppressMessage(
  "Major Code Smell",
  "S2743:Static fields should not be used in generic types",
  Justification = "Reflection cache on Generic Type - Performance optimization")]
private static readonly MethodInfo TryParseMethod = ...;

// ❌ FORBIDDEN - No justification
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public static bool TryParse(...) { }

// ❌ FORBIDDEN - Vague justification
[SuppressMessage("Style", "IDE0022", Justification = "Not needed")]
public void DoSomething() { }
```

**Mandatory Questions for Reviewers:**
1. **Is the suppression really necessary?** (alternatives explored?)
2. **Is the justification technical and precise?** (not "convenience")
3. **Is the scope as limited as possible?** (#pragma vs [SuppressMessage])
4. **Is there a TODO for future refactoring?** (if applicable)

#### **🏗️ Architecture & Design**

**Patterns & Conventions:**
- ✅ **Result Pattern**: Used correctly for error handling
- ✅ **Mediator Pattern**: Requests/handlers respect interfaces
- ✅ **Validation Pattern**: Validation rules properly encapsulated
- ✅ **Repository Pattern**: Data abstractions respected
- ✅ **Dependency Injection**: Constructors with interfaces, no `new()`

**Responsibilities:**
- ✅ **Single Responsibility**: Each class has a clear responsibility
- ✅ **Separation of Concerns**: Business logic separated from infrastructure
- ✅ **Interface Segregation**: Specialized interfaces, not monolithic
- ✅ **Dependency Inversion**: Dependencies towards abstractions

#### **💾 Performance & Memory**

**Allocations & Resources:**
- ✅ **No unnecessary allocations**: Pre-sized collections, string builders
- ✅ **IDisposable handled**: Using statements or appropriate patterns
- ✅ **Async best practices**: ConfigureAwait(false), CancellationToken usage
- ✅ **Static readonly cache**: MethodInfo, Regex, etc. cached

**Reflection Usage:**
- ✅ **MI Helper used**: No reflection by string
- ✅ **Static cache**: MethodInfo/PropertyInfo in static readonly
- ✅ **Fail-fast initialization**: Errors detected at class loading time

**Logging Optimization:**
- ✅ **LoggerMessage used**: No traditional string interpolation or composite formatting
- ✅ **Event ID organization**: Structured by functional area with proper ranges
- ✅ **Performance patterns**: IsEnabled checks for expensive operations
- ✅ **Parameter optimization**: Minimal count, primitive types preferred

#### **🔒 Security & Safety**

**Input Validation:**
- ✅ **ArgumentNullException.ThrowIfNull**: On all public parameters
- ✅ **Business validation**: Explicit validation rules
- ✅ **SQL Injection protection**: Parameterized queries only
- ✅ **XSS prevention**: Appropriate output encoding

**Exception Handling:**
- ✅ **Specific exceptions**: No catch (Exception) except Result pattern
- ✅ **Exception enrichment**: Context added to exceptions
- ✅ **No swallowing**: Exceptions not suppressed without logging
- ✅ **Cancellation handled**: OperationCanceledException propagated
- ✅ **Cancellation handled**: OperationCanceledException propagated