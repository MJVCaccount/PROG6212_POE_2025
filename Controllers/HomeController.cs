using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;

namespace Contract_Monthly_Claim_System.Controllers
{
    /// <summary>
    /// Home Controller - Main dashboard and entry point for the application.
    /// Provides real-time statistics and recent activity overview.
    /// All data is pulled directly from the SQL database for accuracy.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor with dependency injection.
        /// Injects the logger for error tracking and database context for data access.
        /// </summary>
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// GET: Main dashboard page (/)
        /// Displays real-time statistics and recent claims from the database.
        /// Accessible to all authenticated users, with role-based data filtering.
        /// </summary>
        public IActionResult Index()
        {
            // Check if user is authenticated (session exists)
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                // Redirect unauthenticated users to login page
                return RedirectToAction("Login", "Account");
            }

            // Get user role from session for role-based data filtering
            var userRole = HttpContext.Session.GetString("UserRole");
            var lecturerId = HttpContext.Session.GetString("LecturerId");

            try
            {
                // Build dashboard view model with real-time database statistics
                var model = new DashboardViewModel
                {
                    // Count claims by status using LINQ queries
                    PendingClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Pending),
                    ApprovedClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Approved),
                    UnderReviewClaims = _context.Claims.Count(c => c.Status == ClaimStatus.UnderReview),

                    // Calculate total earnings from approved claims only
                    // Sum() returns 0 if no approved claims exist
                    TotalEarnings = _context.Claims
                        .Where(c => c.Status == ClaimStatus.Approved)
                        .Sum(c => (decimal?)c.Amount) ?? 0,

                    // Get the 3 most recent claims (sorted by submission date descending)
                    // For lecturers: show only their claims
                    // For admin roles: show all claims
                    RecentClaims = userRole == "Lecturer"
                        ? _context.Claims
                            .Where(c => c.LecturerId == lecturerId)
                            .OrderByDescending(c => c.SubmitDate)
                            .Take(3)
                            .ToList()
                        : _context.Claims
                            .OrderByDescending(c => c.SubmitDate)
                            .Take(3)
                            .ToList()
                };

                // Pass user information to the view for personalization
                ViewBag.UserName = HttpContext.Session.GetString("UserName");
                ViewBag.UserRole = userRole;

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes
                _logger.LogError(ex, "Error loading dashboard data");

                // Display user-friendly error message
                TempData["Error"] = "An error occurred while loading the dashboard. Please try again.";

                // Return view with empty model to prevent crash
                return View(new DashboardViewModel
                {
                    RecentClaims = new List<Claim>()
                });
            }
        }

        /// <summary>
        /// GET: Privacy policy page
        /// Static content page for privacy information.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// GET: Error page
        /// Displays error information in development, generic message in production.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}