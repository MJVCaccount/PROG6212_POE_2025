using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Data; // Needed for AppDbContext
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq; // Needed for LINQ queries

namespace Contract_Monthly_Claim_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context; // Switch to Database Context

        // Inject AppDbContext instead of ClaimService
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Fetch real-time stats from the SQL Database
            var model = new DashboardViewModel
            {
                PendingClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = _context.Claims.Count(c => c.Status == ClaimStatus.Approved),
                UnderReviewClaims = _context.Claims.Count(c => c.Status == ClaimStatus.UnderReview),

                // Calculate total earnings from approved claims only
                TotalEarnings = _context.Claims
                    .Where(c => c.Status == ClaimStatus.Approved)
                    .Sum(c => c.Amount),

                // Get the 3 most recent claims
                RecentClaims = _context.Claims
                    .OrderByDescending(c => c.SubmitDate)
                    .Take(3)
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}