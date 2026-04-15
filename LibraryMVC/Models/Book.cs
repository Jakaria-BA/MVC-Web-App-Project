using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMVC.Models;

public class Book
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required")]
    [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Genre is required")]
    [StringLength(100, ErrorMessage = "Genre cannot exceed 100 characters")]
    public string Genre { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;

    // Foreign Key
    public int? LibraryId { get; set; }

    // Navigation Properties
    [ForeignKey("LibraryId")]
    public virtual Library? Library { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
