using Microsoft.EntityFrameworkCore;
using LibraryMVC.Models;

namespace LibraryMVC.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Library> Libraries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Transaction fine precision
        modelBuilder.Entity<Transaction>()
            .Property(t => t.FineAmount)
            .HasPrecision(10, 2);

        // Book -> Library relationship (one library has many books)
        modelBuilder.Entity<Book>()
            .HasOne(b => b.Library)
            .WithMany(l => l.Books)
            .HasForeignKey(b => b.LibraryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Book -> Feedback relationship (one book has many feedbacks)
        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.Book)
            .WithMany(b => b.Feedbacks)
            .HasForeignKey(f => f.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // Book -> Transaction relationship (one book has many transactions)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Book)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
