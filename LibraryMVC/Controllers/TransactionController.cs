using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;

namespace LibraryMVC.Controllers;

public class TransactionController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int LoanPeriodDays = 14;
    private const decimal DailyFineRate = 0.50m;

    public TransactionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Transaction
    public async Task<IActionResult> Index(string filter = "active")
    {
        IQueryable<Transaction> transactionsQuery = _context.Transactions
            .Include(t => t.Book);

        transactionsQuery = filter switch
        {
            "active" => transactionsQuery.Where(t => !t.ReturnDate.HasValue),
            "returned" => transactionsQuery.Where(t => t.ReturnDate.HasValue),
            "overdue" => transactionsQuery.Where(t => !t.ReturnDate.HasValue && t.DueDate < DateTime.Now),
            _ => transactionsQuery
        };

        var transactions = await transactionsQuery
            .OrderByDescending(t => t.BorrowDate)
            .ToListAsync();

        ViewBag.CurrentFilter = filter;
        return View(transactions);
    }

    // GET: Transaction/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _context.Transactions
            .Include(t => t.Book)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        // Calculate current fine if overdue and not returned
        if (!transaction.ReturnDate.HasValue && transaction.DueDate < DateTime.Now)
        {
            var daysOverdue = (DateTime.Now - transaction.DueDate).Days;
            transaction.FineAmount = daysOverdue * DailyFineRate;
        }

        return View(transaction);
    }

    // GET: Transaction/Create
    public IActionResult Create(int? bookId)
    {
        ViewBag.BookId = bookId;
        ViewBag.AvailableBooks = new SelectList(
            _context.Books.Where(b => b.IsAvailable).ToList(),
            "Id", "Title"
        );
        return View();
    }

    // POST: Transaction/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("BookId,MemberEmail")] Transaction transaction)
    {
        // Set transaction dates BEFORE validation to satisfy [Required] on DueDate
        transaction.BorrowDate = DateTime.Now;
        transaction.DueDate = DateTime.Now.AddDays(LoanPeriodDays);
        transaction.FineAmount = 0;

        // Clear existing validation errors for dates we just set manually
        ModelState.Remove("BorrowDate");
        ModelState.Remove("DueDate");

        // Validate book exists and is available
        var book = await _context.Books.FindAsync(transaction.BookId);
        if (book == null)
        {
            ModelState.AddModelError("BookId", "Selected book does not exist");
        }
        else if (!book.IsAvailable)
        {
            ModelState.AddModelError("BookId", "This book is currently not available");
        }

        if (ModelState.IsValid)
        {
            // Mark book as unavailable
            if (book != null)
            {
                book.IsAvailable = false;
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Book borrowed successfully. Due date: {transaction.DueDate:MMMM dd, yyyy}";
            return RedirectToAction("Details", "Book", new { id = transaction.BookId });
        }

        ViewBag.AvailableBooks = new SelectList(
            _context.Books.Where(b => b.IsAvailable).ToList(),
            "Id", "Title",
            transaction.BookId
        );
        return View(transaction);
    }

    // GET: Transaction/Return/5
    public async Task<IActionResult> Return(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _context.Transactions
            .Include(t => t.Book)
            .FirstOrDefaultAsync(t => t.Id == id && !t.ReturnDate.HasValue);

        if (transaction == null)
        {
            return NotFound();
        }

        // Calculate fine
        if (transaction.DueDate < DateTime.Now)
        {
            var daysOverdue = (DateTime.Now - transaction.DueDate).Days;
            transaction.FineAmount = daysOverdue * DailyFineRate;
        }
        else
        {
            transaction.FineAmount = 0;
        }

        return View(transaction);
    }

    // POST: Transaction/Return/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Book)
            .FirstOrDefaultAsync(t => t.Id == id && !t.ReturnDate.HasValue);

        if (transaction == null)
        {
            return NotFound();
        }

        // Set return date and calculate fine
        transaction.ReturnDate = DateTime.Now;

        if (transaction.DueDate < transaction.ReturnDate)
        {
            var daysOverdue = (transaction.ReturnDate.Value - transaction.DueDate).Days;
            transaction.FineAmount = daysOverdue * DailyFineRate;
        }
        else
        {
            transaction.FineAmount = 0;
        }

        // Make book available again
        if (transaction.Book != null)
        {
            transaction.Book.IsAvailable = true;
        }

        await _context.SaveChangesAsync();

        string message = transaction.FineAmount > 0
            ? $"Book returned with a fine of ${transaction.FineAmount:F2}"
            : "Book returned successfully. No fines.";

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    // GET: Transaction/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _context.Transactions
            .Include(t => t.Book)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    // POST: Transaction/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            // If transaction is active, make the book available again
            if (!transaction.ReturnDate.HasValue)
            {
                var book = await _context.Books.FindAsync(transaction.BookId);
                if (book != null)
                {
                    book.IsAvailable = true;
                }
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Transaction/MyTransactions
    public async Task<IActionResult> MyTransactions(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return View(new List<Transaction>());
        }

        var transactions = await _context.Transactions
            .Include(t => t.Book)
            .Where(t => t.MemberEmail == email)
            .OrderByDescending(t => t.BorrowDate)
            .ToListAsync();

        ViewBag.Email = email;
        return View("Index", transactions);
    }
}
