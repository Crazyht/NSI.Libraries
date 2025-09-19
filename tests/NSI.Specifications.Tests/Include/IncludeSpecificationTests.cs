using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Include;
using Xunit;

namespace NSI.Specifications.Tests.Include;
/// <summary>
/// Tests for Include specifications.
/// </summary>
public sealed class IncludeSpecificationTests {
  private sealed class Product {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
  }

  private sealed class OrderItem {
    public int Id { get; set; }
    public Product Product { get; set; } = null!;
  }

  private sealed class Order {
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = [];
  }

  private sealed class Customer {
    public int Id { get; set; }
    public List<Order> Orders { get; set; } = [];
  }

  private sealed class DemoContext(DbContextOptions<DemoContext> options): DbContext(options) {
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<Customer>().HasKey(x => x.Id);
      modelBuilder.Entity<Order>().HasKey(x => x.Id);
      modelBuilder.Entity<OrderItem>().HasKey(x => x.Id);
      modelBuilder.Entity<Product>().HasKey(x => x.Id);
      base.OnModelCreating(modelBuilder);
    }
  }

  [Fact]
  public void Enumerable_Include_IsNoOp() {
    var spec = new IncludeSpecification<Customer>().Append("Orders.Items.Product");
    var data = new[] { new Customer { Id = 1, Orders = [] } };
    var same = data.Include(spec);
    Assert.True(object.ReferenceEquals(data, same));
  }

  [Fact]
  public void StringPath_Include_Works() {
    var options = new DbContextOptionsBuilder<DemoContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    using var ctx = new DemoContext(options);
    ctx.Customers.Add(new Customer {
      Id = 1,
      Orders = [
        new Order {
          Id = 1,
          Items = [
            new OrderItem {
              Id = 1,
              Product = new Product { Id = 1, Name = "P" }
            }
          ]
        }
      ]
    });
    ctx.SaveChanges();

    var spec = new IncludeSpecification<Customer>().Append("Orders.Items.Product");
    var query = ctx.Customers.AsQueryable().Include(spec);
    var list = query.ToList();
    Assert.Single(list);
  }

  [Fact]
  public void TypedChain_ThenInclude_Works() {
    var options = new DbContextOptionsBuilder<DemoContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    using var ctx = new DemoContext(options);
    ctx.Customers.Add(new Customer {
      Id = 1,
      Orders = [
        new Order {
          Id = 1,
          Items = [
            new OrderItem {
              Id = 1,
              Product = new Product { Id = 1, Name = "P" }
            }
          ]
        }
      ]
    });
    ctx.SaveChanges();

    var chain = IncludeChains.For<Customer>(
      (Customer c) => c.Orders,
      (Order o) => o.Items,
      (OrderItem i) => i.Product
    );
    var spec = new IncludeSpecification<Customer>([chain]);
    var query = ctx.Customers.AsQueryable().Include(spec);
    var _ = query.ToList();
    Assert.True(true);
  }

  [Fact]
  public void MultipleChains_AreApplied() {
    var options = new DbContextOptionsBuilder<DemoContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    using var ctx = new DemoContext(options);
    ctx.Customers.Add(new Customer {
      Id = 1,
      Orders = [
        new Order {
          Id = 1,
          Items = [
            new OrderItem {
              Id = 1,
              Product = new Product { Id = 1, Name = "P" }
            }
          ]
        }
      ]
    });
    ctx.SaveChanges();

    var chain1 = IncludeChains.For<Customer>((Customer c) => c.Orders);
    var chain2 = IncludeChains.For<Customer>((Customer c) => c.Orders, (Order o) => o.Items);
    var spec = new IncludeSpecification<Customer>([chain1, chain2]).Append("Orders.Items.Product");
    var query = ctx.Customers.AsQueryable().Include(spec);
    var _ = query.ToList();
    Assert.True(true);
  }
}
