using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;

namespace LibraryMVC.Controllers;

public class BookController : Controller
{
    private readonly ApplicationDbContext _context;

    public BookController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Book
    public async Task<IActionResult> Index(string searchString, string genreFilter, int pageNumber = 1)
    {
        var booksQuery = _context.Books
            .Include(b => b.Library)
            .AsQueryable();

        // Search by title or author
        if (!string.IsNullOrEmpty(searchString))
        {
            booksQuery = booksQuery.Where(b =>
                b.Title.Contains(searchString) || b.Author.Contains(searchString));
        }

        // Filter by genre
        if (!string.IsNullOrEmpty(genreFilter))
        {
            booksQuery = booksQuery.Where(b => b.Genre == genreFilter);
        }

        // Pagination
        int pageSize = 10;
        int totalBooks = await booksQuery.CountAsync();
        var books = await booksQuery
            .OrderBy(b => b.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalBooks / pageSize);
        ViewBag.SearchString = searchString;
        ViewBag.GenreFilter = genreFilter;
        ViewBag.Genres = await _context.Books.Select(b => b.Genre).Distinct().ToListAsync();

        return View(books);
    }

    // GET: Book/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.Library)
            .Include(b => b.Feedbacks.Where(f => f.IsApproved))
            .FirstOrDefaultAsync(m => m.Id == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // GET: Book/Create
    public IActionResult Create()
    {
        ViewBag.Libraries = new SelectList(_context.Libraries.ToList(), "Id", "Name");
        return View();
    }

    // POST: Book/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,Author,Genre,IsAvailable,LibraryId")] Book book)
    {
        if (ModelState.IsValid)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Libraries = new SelectList(_context.Libraries.ToList(), "Id", "Name", book.LibraryId);
        return View(book);
    }

    // GET: Book/Edit/5
    public async Task<IActionResult> Edit(int? id)
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

        ViewBag.Libraries = new SelectList(_context.Libraries.ToList(), "Id", "Name", book.LibraryId);
        return View(book);
    }

    // POST: Book/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Genre,IsAvailable,LibraryId")] Book book)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(book);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Libraries = new SelectList(_context.Libraries.ToList(), "Id", "Name", book.LibraryId);
        return View(book);
    }

    // GET: Book/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.Library)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // POST: Book/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool BookExists(int id)
    {
        return _context.Books.Any(e => e.Id == id);
    }
}
