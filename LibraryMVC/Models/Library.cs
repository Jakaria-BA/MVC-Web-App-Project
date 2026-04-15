using System.ComponentModel.DataAnnotations;

namespace LibraryMVC.Models;

public class Library
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    // Navigation Property
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
