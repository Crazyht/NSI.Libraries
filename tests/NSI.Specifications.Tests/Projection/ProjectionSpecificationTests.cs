using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSI.Specifications.Projection;
using Xunit;

namespace NSI.Specifications.Tests.Projection;

/// <summary>
/// Tests for the Projection specification.
/// </summary>
public sealed class ProjectionSpecificationTests
{
    private sealed record Person(int Id, string First, string Last);
    private sealed record PersonDto(int Id, string FullName);

    [Fact]
    public void Selector_Wraps_And_Projects_InMemory()
    {
        var data = new[] { new Person(1, "Ada", "Lovelace"), new Person(2, "Alan", "Turing") };
        IProjectionSpecification<Person, PersonDto> spec = new ProjectionSpecification<Person, PersonDto>(p => new PersonDto(p.Id, p.First + " " + p.Last));

        var projected = data.AsEnumerable().Select(spec).ToArray();

        Assert.Equal(2, projected.Length);
        Assert.Equal("Ada Lovelace", projected[0].FullName);
        Assert.Equal("Alan Turing", projected[1].FullName);
    }

    [Fact]
    public void Queryable_Select_UsesQueryableSelect()
    {
        var data = new[] { new Person(1, "Ada", "Lovelace") }.AsQueryable();
        IProjectionSpecification<Person, PersonDto> spec = new ProjectionSpecification<Person, PersonDto>(p => new PersonDto(p.Id, p.First + " " + p.Last));

        var q = data.Select(spec);
        var result = q.ToArray();

        Assert.Single(result);
        Assert.Equal("Ada Lovelace", result[0].FullName);
    }

    [Fact]
    public void Select_NullArguments_Throw()
    {
        IProjectionSpecification<Person, PersonDto> spec = new ProjectionSpecification<Person, PersonDto>(p => new PersonDto(p.Id, p.First + " " + p.Last));
        Assert.Throws<ArgumentNullException>(() => ProjectionExtensions.Select<Person, PersonDto>((IQueryable<Person>)null!, spec));
        Assert.Throws<ArgumentNullException>(() => ProjectionExtensions.Select<Person, PersonDto>([], null!));
    }

    private sealed class PeopleContext(DbContextOptions<PeopleContext> options) : DbContext(options)
    {
        public DbSet<Person> People => Set<Person>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().HasKey(p => p.Id);
            base.OnModelCreating(modelBuilder);
        }
    }

    [Fact]
    public void EfCore_ToQueryString_ReflectsProjectionShape()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<PeopleContext>().UseSqlite(connection).Options;
            using var ctx = new PeopleContext(options);
            ctx.Database.EnsureCreated();
            ctx.People.AddRange(new Person(1, "Ada", "Lovelace"));
            ctx.SaveChanges();

            IProjectionSpecification<Person, PersonDto> spec = new ProjectionSpecification<Person, PersonDto>(p => new PersonDto(p.Id, p.First + " " + p.Last));
            var query = ctx.People.AsQueryable().Select(spec);
            var sql = query.ToQueryString();

            Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Id", sql, StringComparison.Ordinal);
        }
        finally
        {
            connection.Close();
        }
    }
}
