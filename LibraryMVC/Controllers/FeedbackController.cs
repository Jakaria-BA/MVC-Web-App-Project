using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryMVC.Data;
using LibraryMVC.Models;

namespace LibraryMVC.Controllers;

public class FeedbackController : Controller
{
    private readonly ApplicationDbContext _context;

    public FeedbackController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Feedback
    public async Task<IActionResult> Index(int? bookId, bool showAll = false)
    {
        IQueryable<Feedback> feedbackQuery = _context.Feedbacks
            .Include(f => f.Book);

        // Filter by book if specified
        if (bookId.HasValue)
        {
            feedbackQuery = feedbackQuery.Where(f => f.BookId == bookId.Value);
        }

        // Only show approved feedback for regular users
        if (!showAll && !User.IsInRole("Admin"))
        {
            feedbackQuery = feedbackQuery.Where(f => f.IsApproved);
        }

        var feedbacks = await feedbackQuery
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        ViewBag.BookId = bookId;
        return View(feedbacks);
    }

    // GET: Feedback/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feedback = await _context.Feedbacks
            .Include(f => f.Book)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (feedback == null)
        {
            return NotFound();
        }

        return View(feedback);
    }

    // GET: Feedback/Create
    public IActionResult Create(int? bookId)
    {
        ViewBag.BookId = bookId;
        ViewBag.Books = new SelectList(_context.Books.Where(b => b.IsAvailable).ToList(), "Id", "Title");
        return View();
    }

    // POST: Feedback/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,BookId,UserEmail,Comment,Rating")] Feedback feedback)
    {
        // Validate rating range
        if (feedback.Rating < 1 || feedback.Rating > 5)
        {
            ModelState.AddModelError("Rating", "Rating must be between 1 and 5");
        }

        // Check if book exists
        var book = await _context.Books.FindAsync(feedback.BookId);
        if (book == null)
        {
            ModelState.AddModelError("BookId", "Selected book does not exist");
        }

        if (ModelState.IsValid)
        {
            feedback.CreatedAt = DateTime.Now;
            feedback.IsApproved = false; // Requires moderation
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your feedback has been submitted and is awaiting approval.";
            return RedirectToAction("Details", "Book", new { id = feedback.BookId });
        }

        ViewBag.Books = new SelectList(_context.Books.ToList(), "Id", "Title", feedback.BookId);
        return View(feedback);
    }

    // GET: Feedback/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null)
        {
            return NotFound();
        }

        ViewBag.Books = new SelectList(_context.Books.ToList(), "Id", "Title", feedback.BookId);
        return View(feedback);
    }

    // POST: Feedback/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,BookId,UserEmail,Comment,Rating,IsApproved")] Feedback feedback)
    {
        if (id != feedback.Id)
        {
            return NotFound();
        }

        if (feedback.Rating < 1 || feedback.Rating > 5)
        {
            ModelState.AddModelError("Rating", "Rating must be between 1 and 5");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingFeedback = await _context.Feedbacks.FindAsync(id);
                if (existingFeedback == null)
                {
                    return NotFound();
                }

                existingFeedback.BookId = feedback.BookId;
                existingFeedback.UserEmail = feedback.UserEmail;
                existingFeedback.Comment = feedback.Comment;
                existingFeedback.Rating = feedback.Rating;
                existingFeedback.IsApproved = feedback.IsApproved;

                _context.Update(existingFeedback);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FeedbackExists(feedback.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Books = new SelectList(_context.Books.ToList(), "Id", "Title", feedback.BookId);
        return View(feedback);
    }

    // GET: Feedback/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feedback = await _context.Feedbacks
            .Include(f => f.Book)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (feedback == null)
        {
            return NotFound();
        }

        return View(feedback);
    }

    // POST: Feedback/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback != null)
        {
            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Feedback/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null)
        {
            return NotFound();
        }

        feedback.IsApproved = true;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Feedback approved successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Feedback/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null)
        {
            return NotFound();
        }

        feedback.IsApproved = false;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Feedback rejected.";
        return RedirectToAction(nameof(Index));
    }

    private bool FeedbackExists(int id)
    {
        return _context.Feedbacks.Any(e => e.Id == id);
    }
}
