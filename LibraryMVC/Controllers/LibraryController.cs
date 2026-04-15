using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;

namespace LibraryMVC.Controllers;

public class LibraryController : Controller
{
    private readonly ApplicationDbContext _context;

    public LibraryController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Library
    public async Task<IActionResult> Index()
    {
        var libraries = await _context.Libraries
            .Include(l => l.Books)
            .ToListAsync();

        // Add book count to ViewBag for each library
        ViewBag.LibraryBookCounts = libraries.ToDictionary(
            l => l.Id,
            l => l.Books.Count(b => b.IsAvailable)
        );

        return View(libraries);
    }

    // GET: Library/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var library = await _context.Libraries
            .Include(l => l.Books)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (library == null)
        {
            return NotFound();
        }

        return View(library);
    }

    // GET: Library/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Library/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Address")] Library library)
    {
        if (ModelState.IsValid)
        {
            _context.Libraries.Add(library);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(library);
    }

    // GET: Library/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var library = await _context.Libraries.FindAsync(id);
        if (library == null)
        {
            return NotFound();
        }
        return View(library);
    }

    // POST: Library/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address")] Library library)
    {
        if (id != library.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Libraries.Update(library);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LibraryExists(library.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(library);
    }

    // GET: Library/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var library = await _context.Libraries
            .Include(l => l.Books)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (library == null)
        {
            return NotFound();
        }

        return View(library);
    }

    // POST: Library/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var library = await _context.Libraries
            .Include(l => l.Books)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (library != null)
        {
            // Set LibraryId to null for all books in this library instead of deleting them
            foreach (var book in library.Books)
            {
                book.LibraryId = null;
            }

            _context.Libraries.Remove(library);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool LibraryExists(int id)
    {
        return _context.Libraries.Any(e => e.Id == id);
    }
}
