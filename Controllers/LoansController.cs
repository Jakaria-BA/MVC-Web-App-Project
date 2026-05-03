using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LibraryManagementSystem.Controllers;

[Authorize]
public class LoansController : Controller
{
    private readonly ApplicationDbContext _context;

    public LoansController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Loans/Index
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _context.Loans
            .Include(l => l.Book)
            .Include(l => l.User)
            .AsQueryable();

        if (!User.IsInRole("Librarian"))
        {
            query = query.Where(l => l.UserId == userId);
        }

        return View(await query.OrderByDescending(l => l.LoanDate).ToListAsync());
    }

    // GET: Loans/Create?bookISBN=xxx — show confirmation page
    public async Task<IActionResult> Create(string bookISBN)
    {
        // Librarians cannot borrow books
        if (User.IsInRole("Librarian"))
        {
            TempData["ErrorMessage"] = "Librarians cannot borrow books.";
            return RedirectToAction("Index", "Books");
        }

        if (string.IsNullOrEmpty(bookISBN))
            return RedirectToAction("Index", "Books");

        var book = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == bookISBN);

        if (book == null)
        {
            TempData["ErrorMessage"] = "Book not found.";
            return RedirectToAction("Index", "Books");
        }

        if (book.AvailableCopies <= 0)
        {
            TempData["ErrorMessage"] = $"\"{book.Title}\" is currently out of stock.";
            return RedirectToAction("Index", "Books");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingLoan = await _context.Loans
            .FirstOrDefaultAsync(l => l.UserId == userId && l.BookISBN == bookISBN && l.ReturnDate == null);

        if (existingLoan != null)
        {
            TempData["ErrorMessage"] = $"You already have an active loan for \"{book.Title}\".";
            return RedirectToAction("Index", "Books");
        }

        ViewBag.BookTitle  = book.Title;
        ViewBag.BookAuthor = book.Author;
        ViewBag.BookISBN   = book.ISBN;
        ViewBag.DueDate    = DateTime.Now.AddDays(14).ToShortDateString();

        return View();
    }

    // POST: Loans/Create — confirm and save the borrow
    [HttpPost]
    [ActionName("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateConfirmed(string BookISBN)
    {
        // Librarians cannot borrow books
        if (User.IsInRole("Librarian"))
        {
            TempData["ErrorMessage"] = "Librarians cannot borrow books.";
            return RedirectToAction("Index", "Books");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var book = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == BookISBN);

        if (book == null || book.AvailableCopies <= 0)
        {
            TempData["ErrorMessage"] = "This book is no longer available.";
            return RedirectToAction("Index", "Books");
        }

        var existingLoan = await _context.Loans
            .FirstOrDefaultAsync(l => l.UserId == userId && l.BookISBN == BookISBN && l.ReturnDate == null);

        if (existingLoan != null)
        {
            TempData["ErrorMessage"] = $"You already have an active loan for \"{book.Title}\".";
            return RedirectToAction("Index", "Books");
        }

        var loan = new Loan
        {
            UserId     = userId!,
            BookISBN   = BookISBN,
            LoanDate   = DateTime.Now,
            DueDate    = DateTime.Now.AddDays(14),
            FineAmount = 0
        };

        book.AvailableCopies -= 1;
        if (book.AvailableCopies == 0)
            book.IsAvailable = false;

        _context.Add(loan);
        _context.Update(book);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"✅ You have successfully borrowed \"{book.Title}\". Please return it by {loan.DueDate.ToShortDateString()}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Loans/Return — mark a loan as returned
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var loan = await _context.Loans
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
            return NotFound();

        // Only the borrower or a librarian can return
        if (loan.UserId != userId && !User.IsInRole("Librarian"))
        {
            TempData["ErrorMessage"] = "You are not authorised to return this book.";
            return RedirectToAction(nameof(Index));
        }

        // Already returned
        if (loan.ReturnDate != null)
        {
            TempData["ErrorMessage"] = "This book has already been returned.";
            return RedirectToAction(nameof(Index));
        }

        loan.ReturnDate = DateTime.Now;

        // Calculate fine: $1 per overdue day
        if (loan.DueDate < loan.ReturnDate)
        {
            var overdueDays = (loan.ReturnDate.Value - loan.DueDate).Days;
            loan.FineAmount = overdueDays * 1.00m;
        }

        // Restore available copy
        if (loan.Book != null)
        {
            loan.Book.AvailableCopies += 1;
            loan.Book.IsAvailable = true;
            _context.Update(loan.Book);
        }

        _context.Update(loan);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = loan.FineAmount > 0
            ? $"Book returned. A fine of ${loan.FineAmount:0.00} was recorded for {(loan.ReturnDate.Value - loan.DueDate).Days} overdue day(s)."
            : "✅ Book returned successfully. Thank you!";

        return RedirectToAction(nameof(Index));
    }
}
