using Microsoft.EntityFrameworkCore; // REQUIRED for .Include()
using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class HRController : Controller
{
    private readonly AppDbContext _context;

    public HRController(AppDbContext context) { _context = context; }

    // GET: Manage Lecturers Dashboard
    public IActionResult Index()
    {
        return View(_context.Lecturers.ToList());
    }

    // POST: Add a new Lecturer (Data Automation)
    [HttpPost]
    public IActionResult AddLecturer(Lecturer lecturer)
    {
        if (ModelState.IsValid)
        {
            // Check if ID exists
            if (!_context.Lecturers.Any(l => l.LecturerId == lecturer.LecturerId))
            {
                _context.Lecturers.Add(lecturer);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        return View("Index", _context.Lecturers.ToList());
    }

    // GET: Generate Report (Automation)
    public IActionResult GenerateReport()
    {
        // Automate: detailed report of approved claims
        var approvedClaims = _context.Claims
            .Where(c => c.Status == ClaimStatus.Approved)
            .Include(c => c.Lecturer) // Join tables (Now works because Navigation Property exists)
            .ToList();

        return View(approvedClaims);
    }
}