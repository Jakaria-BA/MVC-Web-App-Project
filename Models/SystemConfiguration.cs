using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class SystemConfiguration
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Range(1, 30)]
    public int MaxBooksPerUser { get; set; } = 5;

    [Required]
    [Range(1, 90)]
    public int LoanDurationDays { get; set; } = 14;

    [Required]
    [Range(0, 100)]
    public decimal FinePerDay { get; set; } = 1.0m;
}
