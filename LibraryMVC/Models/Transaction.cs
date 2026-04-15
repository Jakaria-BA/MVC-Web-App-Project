using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMVC.Models;

public class Transaction
{
    public int Id { get; set; }

    [Required]
    public int BookId { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string MemberEmail { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; } = DateTime.Now;

    public DateTime? ReturnDate { get; set; }

    [Required(ErrorMessage = "Due date is required")]
    public DateTime DueDate { get; set; }

    public decimal FineAmount { get; set; }

    // Computed property
    public bool IsOverdue => !ReturnDate.HasValue && DateTime.Now > DueDate;

    // Navigation Property
    [ForeignKey("BookId")]
    public virtual Book? Book { get; set; }
}
