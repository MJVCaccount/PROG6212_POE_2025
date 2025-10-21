using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Contract_Monthly_Claim_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClaimService _claimService;

        public HomeController(ILogger<HomeController> logger, ClaimService claimService)
        {
            _logger = logger;
            _claimService = claimService;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                PendingClaims = _claimService.GetClaimsByStatus(ClaimStatus.Pending).Count,
                ApprovedClaims = _claimService.GetClaimsByStatus(ClaimStatus.Approved).Count,
                UnderReviewClaims = _claimService.GetClaimsByStatus(ClaimStatus.UnderReview).Count,
                TotalEarnings = _claimService.GetAllClaims().Sum(c => c.Amount),
                RecentClaims = _claimService.GetAllClaims().OrderByDescending(c => c.SubmitDate).Take(3).ToList()
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