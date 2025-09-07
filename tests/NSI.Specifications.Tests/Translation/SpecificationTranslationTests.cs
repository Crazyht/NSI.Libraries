using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Filtering;
using NSI.Specifications.Filtering.Comparison;
using NSI.Specifications.Filtering.Text;
using NSI.Specifications.Sorting;
using NSI.Specifications.Projection;
using NSI.Specifications.Include;
using Xunit;

namespace NSI.Specifications.Tests.Translation;

/// <summary>
/// EF Core translation tests ensuring specifications compose into translatable expression trees (no client evaluation).
/// </summary>
public sealed class SpecificationTranslationTests : IDisposable
{
    private static readonly string[] ExpectedAliceCarol = ["Alice", "Carol"]; // reuse across tests
    private static readonly string[] ExpectedAliceBob = ["Alice", "Bob"]; // for OR + ordering test
    private readonly TestDbContext _Ctx;
    private readonly SqliteConnection _Conn;

    private sealed class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<Book> Books { get; set; } = [];
    }
    private sealed class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public Author Author { get; set; } = null!;
    }
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Book> Books => Set<Book>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>().HasKey(a => a.Id);
            modelBuilder.Entity<Book>().HasKey(b => b.Id);
            modelBuilder.Entity<Author>().HasMany(a => a.Books).WithOne(b => b.Author).HasForeignKey(b => b.AuthorId);
            base.OnModelCreating(modelBuilder);
        }
    }

    public SpecificationTranslationTests()
    {
        _Conn = new SqliteConnection("DataSource=:memory:;Cache=Shared");
        _Conn.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>()
          .UseSqlite(_Conn)
          .Options;
        _Ctx = new TestDbContext(options);
        _Ctx.Database.EnsureCreated();
        Seed();
    }

    private void Seed()
    {
        var a1 = new Author { Id = 1, Name = "Alice", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var a2 = new Author { Id = 2, Name = "Bob", CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc) };
        var a3 = new Author { Id = 3, Name = "Carol", CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc) };
        _Ctx.Authors.AddRange(a1, a2, a3);
        _Ctx.Books.AddRange(
            new Book { Id = 10, Title = "Alpha", Author = a1, AuthorId = a1.Id },
            new Book { Id = 11, Title = "Beta", Author = a1, AuthorId = a1.Id },
            new Book { Id = 12, Title = "Gamma", Author = a2, AuthorId = a2.Id }
        );
        _Ctx.SaveChanges();
    }

    [Fact]
    public void Filter_And_Between_OrderBy_Translates()
    {
        var nameContains = new ContainsSpecification<Author>(a => a.Name, "a", ignoreCase: true);
        var createdBetween = new BetweenSpecification<Author, DateTime>(a => a.CreatedAt,
            new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        var combined = nameContains.And(createdBetween);
        var order = SortSpecification<Author>.FromSingle(a => a.CreatedAt, SortDirection.Desc).Then(a => a.Name);
        var projection = new ProjectionSpecification<Author, string>(a => a.Name + ":" + a.CreatedAt.Year);

        var query = _Ctx.Authors
          .Where(combined)
          .OrderBy(order)
          .Select(projection);

        var sql = query.ToQueryString();
        Assert.Contains("FROM \"Authors\"", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        var results = query.ToList();
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Include_Projection_NoClientEval()
    {
        var include = new IncludeSpecification<Author>(chains: [IncludeChains.For<Author>((Expression<Func<Author, List<Book>>>)(a => a.Books))]);
        var proj = new ProjectionSpecification<Author, int>(a => a.Books.Count);
        var query = _Ctx.Authors.Include(include).Select(proj);
        var sql = query.ToQueryString();
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEFT JOIN", sql, StringComparison.OrdinalIgnoreCase);
        _ = query.ToArray();
    }

    [Fact]
    public void InSpecification_TranslatesOrExecutes()
    {
        var ids = new[] { 1, 3 };
        var inSpec = new InSpecification<Author, int>(a => a.Id, ids);
        var q = _Ctx.Authors.Where(inSpec);
        var sql = q.ToQueryString();
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        var list = q.ToList();
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Not_Specification_Composes()
    {
        var startsWithA = new StartsWithSpecification<Author>(a => a.Name, "A", ignoreCase: true);
        var notSpec = startsWithA.Not();
        var q = _Ctx.Authors.Where(notSpec);
        var sql = q.ToQueryString();
        Assert.DoesNotContain("A\" =", sql, StringComparison.Ordinal); // crude
        var list = q.ToList();
        Assert.Contains(list, a => a.Name == "Bob");
    }

    [Fact]
    public void Or_Composition_Translates()
    {
        var startsWithA = new StartsWithSpecification<Author>(a => a.Name, "A", ignoreCase: true);
        var startsWithC = new StartsWithSpecification<Author>(a => a.Name, "C", ignoreCase: true);
        var orSpec = startsWithA.Or(startsWithC);
        var q = _Ctx.Authors.Where(orSpec);
        var sql = q.ToQueryString();
        Assert.Contains("OR", sql, StringComparison.OrdinalIgnoreCase);
        var names = q.Select(a => a.Name).ToList();
        Assert.Equal(ExpectedAliceCarol, names.OrderBy(n => n));
    }

    [Fact]
    public void Nested_Not_Or_And_PreservesLogic()
    {
        var containsA = new ContainsSpecification<Author>(a => a.Name, "a", ignoreCase: true); // Alice, Carol
        var startsWithB = new StartsWithSpecification<Author>(a => a.Name, "B", ignoreCase: true); // Bob
        var complex = containsA.Or(startsWithB).And(containsA.Not()); // (A or B) and not A => B only
        var q = _Ctx.Authors.Where(complex);
        var sql = q.ToQueryString();
        Assert.Contains("AND", sql, StringComparison.OrdinalIgnoreCase);
        var list = q.Select(a => a.Name).ToList();
        Assert.Single(list);
        Assert.Equal("Bob", list[0]);
    }

    [Fact]
    public void Or_OrderBy_Projection_TranslatesOrdered()
    {
        var startsWithA = new StartsWithSpecification<Author>(a => a.Name, "A", ignoreCase: true);
        var startsWithB = new StartsWithSpecification<Author>(a => a.Name, "B", ignoreCase: true);
        var orSpec = startsWithA.Or(startsWithB);
        var sort = SortSpecification<Author>.FromSingle(a => a.Name, SortDirection.Asc).Then(a => a.Id, SortDirection.Desc);
        var proj = new ProjectionSpecification<Author, string>(a => a.Name);

        var query = _Ctx.Authors.Where(orSpec).OrderBy(sort).Select(proj);
        var sql = query.ToQueryString();
        Assert.Contains("OR", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"Name\"", sql, StringComparison.OrdinalIgnoreCase);
        var result = query.ToArray();
        Assert.Equal(ExpectedAliceBob, result);
    }

    public void Dispose()
    {
        _Ctx.Dispose();
        _Conn.Dispose();
    }
}
