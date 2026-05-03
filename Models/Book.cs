using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Book
{
    [Key]
    [Required]
    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Author { get; set; } = string.Empty;

    [Url]
    public string? CoverImageURL { get; set; }

    [Required]
    public bool IsAvailable { get; set; } = true;

    [StringLength(100)]
    public string Genre { get; set; } = string.Empty;

    [Required]
    [CustomValidation(typeof(Book), nameof(ValidatePublicationYear))]
    public int PublicationYear { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Total copies must be at least 1.")]
    public int TotalCopies { get; set; } = 1;

    public int AvailableCopies { get; set; } = 1;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public static ValidationResult? ValidatePublicationYear(int year, ValidationContext context)
    {
        if (year > DateTime.Now.Year)
        {
            return new ValidationResult("Publication year cannot be in the future.");
        }
        return ValidationResult.Success;
    }
}
