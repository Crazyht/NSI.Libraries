using Microsoft.Extensions.DependencyInjection;
using NSI.Core.Validation;
using NSI.Core.Validation.Rules;

namespace NSI.Core.Tests.Validation.Rules;
/// <summary>
/// Tests for the <see cref="UniqueRule{T,TValue}"/> asynchronous validation rule.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify uniqueness validation including database checks,
/// service provider integration, and null value handling.
/// </para>
/// </remarks>
public sealed class UniqueRuleTests {
  private sealed class TestModel {
    public string? Email { get; set; }
    public string? Username { get; set; }
  }

  [Fact]
  public async Task ValidateAsync_WithUniqueValue_ShouldReturnNoErrors() {
    var rule = new UniqueRule<TestModel, string>(
      m => m.Email!,
      async (_, _, ct) => {
        await Task.Delay(10, ct);
        return false; // Email does not exist
      }
    );

    var model = new TestModel { Email = "new@example.com" };
    var context = ValidationContext.Empty();

    var errors = await rule.ValidateAsync(model, context);

    Assert.Empty(errors);
  }

  [Fact]
  public async Task ValidateAsync_WithExistingValue_ShouldReturnError() {
    var rule = new UniqueRule<TestModel, string>(
      m => m.Email!,
      async (_, _, ct) => {
        await Task.Delay(10, ct);
        return true; // Email already exists
      }
    );

    var model = new TestModel { Email = "existing@example.com" };
    var context = ValidationContext.Empty();

    var errors = (await rule.ValidateAsync(model, context)).ToList();

    Assert.Single(errors);
    Assert.Equal("NOT_UNIQUE", errors[0].ErrorCode);
    Assert.Equal("Email already exists.", errors[0].ErrorMessage);
    Assert.Equal("Email", errors[0].PropertyName);
    Assert.Null(errors[0].ExpectedValue);
  }

  [Fact]
  public async Task ValidateAsync_WithNullValue_ShouldReturnNoErrors() {
    var rule = new UniqueRule<TestModel, string>(
      m => m.Email!,
      async (_, _, ct) => {
        await Task.Delay(10, ct);
        return true;
      }
    );

    var model = new TestModel { Email = null };
    var context = ValidationContext.Empty();

    var errors = await rule.ValidateAsync(model, context);

    Assert.Empty(errors);
  }

  [Fact]
  public async Task ValidateAsync_WithServiceProvider_ShouldUseInjectedService() {
    // Create a fake repository instead of using Moq
    var fakeRepository = new FakeUserRepository(emailExists: true);

    var services = new ServiceCollection();
    services.AddSingleton<IUserRepository>(fakeRepository);
    var serviceProvider = services.BuildServiceProvider();

    var rule = new UniqueRule<TestModel, string>(
      m => m.Email!,
      async (sp, email, ct) => {
        var repo = sp.GetService<IUserRepository>();
        return repo != null && await repo.EmailExistsAsync(email, ct);
      }
    );

    var model = new TestModel { Email = "test@example.com" };
    var context = new ValidationContext(serviceProvider);

    var errors = (await rule.ValidateAsync(model, context)).ToList();

    Assert.Single(errors);
    Assert.Equal("NOT_UNIQUE", errors[0].ErrorCode);
    Assert.Equal("Email already exists.", errors[0].ErrorMessage);
    Assert.Equal("Email", errors[0].PropertyName);
    Assert.Equal(1, fakeRepository.GetEmailCheckCount("test@example.com"));
  }

  [Fact]
  public async Task ValidateAsync_WithCancellation_ShouldRespectCancellationToken() {
    using var cts = new CancellationTokenSource();
    var rule = new UniqueRule<TestModel, string>(
      m => m.Email!,
      async (_, _, ct) => {
        await Task.Delay(1000, ct);
        return false;
      }
    );

    var model = new TestModel { Email = "test@example.com" };
    var context = ValidationContext.Empty();

    await cts.CancelAsync();

    await Assert.ThrowsAsync<TaskCanceledException>(
      () => rule.ValidateAsync(model, context, cts.Token)
    );
  }

  private interface IUserRepository {
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
  }

  // Simple test double for IUserRepository
  private sealed class FakeUserRepository(bool emailExists): IUserRepository {
    private readonly bool _EmailExists = emailExists;
    private readonly Dictionary<string, int> _CallCounts = [];

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) {
      if (!_CallCounts.TryGetValue(email, out var value)) {
        value = 0;
        _CallCounts[email] = value;
      }

      _CallCounts[email] = value + 1;
      return Task.FromResult(_EmailExists);
    }

    public int GetEmailCheckCount(string email) =>
      _CallCounts.TryGetValue(email, out var count) ? count : 0;
  }
}
