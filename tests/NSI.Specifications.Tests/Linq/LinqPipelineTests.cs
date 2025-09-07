using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NSI.Specifications.Filtering;
using NSI.Specifications.Filtering.Text;
using NSI.Specifications.Include;
using NSI.Specifications.Projection;
using NSI.Specifications.Sorting;
using Xunit;

namespace NSI.Specifications.Tests.Linq;

/// <summary>
/// Tests end-to-end chaining of specification LINQ extensions: Where → Include → OrderBy → Select.
/// </summary>
public sealed class LinqPipelineTests
{
    private sealed class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // List for CA1859 performance preference in tests.
        public List<Book> Books { get; set; } = new();
    }

    private sealed class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public Author Author { get; set; } = null!;
    }

    private sealed class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options)
    {
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Book> Books => Set<Book>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>().HasKey(a => a.Id);
            modelBuilder.Entity<Book>().HasKey(b => b.Id);
            modelBuilder.Entity<Author>()
              .HasMany(a => a.Books)
              .WithOne(b => b.Author)
              .HasForeignKey(b => b.AuthorId);
            base.OnModelCreating(modelBuilder);
        }
    }

    [Fact]
    public void Pipeline_IQueryable_FilterIncludeSortSelect_ProducesExpectedOrder()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<LibraryContext>().UseInMemoryDatabase(dbName).Options;
        using var ctx = new LibraryContext(options);

        var alice = new Author { Id = 1, Name = "Alice" };
        var bob = new Author { Id = 2, Name = "Bob" };
        var carol = new Author { Id = 3, Name = "Carol" };
        ctx.Authors.AddRange(alice, bob, carol);
        ctx.Books.AddRange(new[] {
          new Book { Id = 10, Title = "A1", Author = alice },
          new Book { Id = 11, Title = "A2", Author = alice },
          new Book { Id = 20, Title = "B1", Author = bob }
        });
        ctx.SaveChanges();

        var filter = new ContainsSpecification<Author>(a => a.Name, "a", ignoreCase: true);
        var include = new IncludeSpecification<Author>(chains: [IncludeChains.For<Author>((Expression<Func<Author, List<Book>>>)(a => a.Books))]);
        var sort = SortSpecification<Author>.FromSingle(a => a.Name, SortDirection.Asc);
        var project = new ProjectionSpecification<Author, string>(a => a.Name);

        var names = ctx.Authors
          .AsQueryable()
          .Where(filter)
          .Include(include)
          .OrderBy(sort)
          .Select(project)
          .ToArray();

        Assert.Equal(ExpectedOrderedNames, names);

        // Ensure Include materialized related entities
        var authorsWithBooks = ctx.Authors.Where(filter).Include(include).ToArray();
        var aliceLoaded = authorsWithBooks.Single(a => a.Name == "Alice");
        Assert.Equal(2, aliceLoaded.Books.Count);
    }

    [Fact]
    public void Pipeline_IEnumerable_FilterIncludeSortSelect_ProducesExpectedOrder()
    {
        var authors = new[] {
          new Author { Id = 1, Name = "Alice", Books = new List<Book> { new() { Id = 10, Title = "A1", AuthorId = 1 }, new() { Id = 11, Title = "A2", AuthorId = 1 } } },
          new Author { Id = 2, Name = "Bob", Books = new List<Book> { new() { Id = 20, Title = "B1", AuthorId = 2 } } },
          new Author { Id = 3, Name = "Carol" }
        };

        var filter = new ContainsSpecification<Author>(a => a.Name, "a", ignoreCase: true);
        var include = new IncludeSpecification<Author>(chains: [IncludeChains.For<Author>((Expression<Func<Author, List<Book>>>)(a => a.Books))]);
        var sort = SortSpecification<Author>.FromSingle(a => a.Name, SortDirection.Asc);
        var project = new ProjectionSpecification<Author, string>(a => a.Name);

        var names = authors
          .AsEnumerable()
          .Where(filter)
          .Include(include) // no-op for IEnumerable
          .OrderBy(sort)
          .Select(project)
          .ToArray();

        Assert.Equal(ExpectedOrderedNames, names);
    }

    private static readonly string[] ExpectedOrderedNames = ["Alice", "Carol"]; // CA1861 compliant
}
