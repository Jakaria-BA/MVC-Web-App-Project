using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers;

[Authorize]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Books
    [AllowAnonymous]
    public async Task<IActionResult> Index(string searchString)
    {
        var books = from b in _context.Books
                    select b;

        if (!String.IsNullOrEmpty(searchString))
        {
            books = books.Where(s => s.Title!.Contains(searchString) || s.Author!.Contains(searchString));
        }

        return View(await books.ToListAsync());
    }

    // GET: Books/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(m => m.ISBN == id);
        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // GET: Books/Create
    [Authorize(Roles = "Librarian")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> Create([Bind("ISBN,Title,Author,CoverImageURL,IsAvailable,Genre,PublicationYear,TotalCopies,AvailableCopies")] Book book)
    {
        ValidateCopyCounts(book);

        if (ModelState.IsValid)
        {
            book.IsAvailable = book.AvailableCopies > 0;
            _context.Add(book);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Book \"{book.Title}\" was created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(book);
    }

    // GET: Books/Edit/5
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }
        return View(book);
    }

    // POST: Books/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> Edit(string id, [Bind("ISBN,Title,Author,CoverImageURL,IsAvailable,Genre,PublicationYear,TotalCopies,AvailableCopies")] Book book)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        ValidateCopyCounts(book);

        if (ModelState.IsValid)
        {
            try
            {
                var existingBook = await _context.Books
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.ISBN == id);

                if (existingBook == null)
                {
                    return NotFound();
                }

                book.IsAvailable = book.AvailableCopies > 0;

                if (id == book.ISBN)
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (await BookExistsAsync(book.ISBN))
                    {
                        ModelState.AddModelError(nameof(Book.ISBN), "Another book already uses this ISBN.");
                        return View(book);
                    }

                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    _context.Books.Add(book);
                    await _context.SaveChangesAsync();

                    var relatedLoans = await _context.Loans
                        .Where(l => l.BookISBN == id)
                        .ToListAsync();

                    foreach (var loan in relatedLoans)
                    {
                        loan.BookISBN = book.ISBN;
                    }

                    await _context.SaveChangesAsync();

                    _context.Books.Remove(existingBook);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["SuccessMessage"] = $"Book \"{book.Title}\" was updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(book);
    }

    // GET: Books/Delete/5
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(m => m.ISBN == id);
        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // POST: Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Librarian")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            TempData["SuccessMessage"] = $"Book \"{book.Title}\" was deleted successfully.";
            _context.Books.Remove(book);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool BookExists(string id)
    {
        return _context.Books.Any(e => e.ISBN == id);
    }

    private async Task<bool> BookExistsAsync(string id)
    {
        return await _context.Books.AnyAsync(e => e.ISBN == id);
    }

    private void ValidateCopyCounts(Book book)
    {
        if (book.AvailableCopies < 0)
        {
            ModelState.AddModelError(nameof(Book.AvailableCopies), "Available copies cannot be negative.");
        }

        if (book.AvailableCopies > book.TotalCopies)
        {
            ModelState.AddModelError(nameof(Book.AvailableCopies), "Available copies cannot be greater than total copies.");
        }
    }
}
