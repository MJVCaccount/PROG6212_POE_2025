using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Contract_Monthly_Claim_System.Controllers
{
    /// <summary>
    /// HR Controller - Human Resources Management with Automation
    /// Handles lecturer data management, user account creation, and automated reporting.
    /// AUTOMATION FEATURES:
    /// 1. Centralized rate management (eliminates manual entry errors)
    /// 2. Automated report generation using LINQ
    /// 3. Bulk operations for efficiency
    /// </summary>
    public class HRController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ClaimAutomationService _automationService;

        /// <summary>
        /// Constructor with dependency injection of database context and automation service.
        /// </summary>
        public HRController(AppDbContext context, ClaimAutomationService automationService)
        {
            _context = context;
            _automationService = automationService;
        }

        #region Lecturer Management

        /// <summary>
        /// GET: Display the HR dashboard showing all lecturers.
        /// This is the central view for managing lecturer accounts and rates.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Session-based authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                TempData["Error"] = "Access denied. HR privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // Retrieve all lecturers with active status indicator
            var lecturers = await _context.Lecturers
                .OrderBy(l => l.Name)
                .ToListAsync();

            return View(lecturers);
        }

        /// <summary>
        /// GET: Display the form for adding a new lecturer.
        /// HR uses this to onboard new independent contractors.
        /// </summary>
        [HttpGet]
        public IActionResult AddLecturer()
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                return Unauthorized();
            }

            return View();
        }

        /// <summary>
        /// POST: Add a new lecturer to the system with initial credentials.
        /// AUTOMATION: This creates the master record that will be used for automated rate lookup.
        /// Default password is set to "ChangeMe123!" and must be changed on first login.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLecturer(Lecturer lecturer)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                return Unauthorized();
            }

            // Remove password hash from validation since we set it manually
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                // Check if lecturer ID already exists to prevent duplicates
                if (await _context.Lecturers.AnyAsync(l => l.LecturerId == lecturer.LecturerId))
                {
                    ModelState.AddModelError("LecturerId",
                        "This Lecturer ID already exists. Please use a unique ID.");
                    return View(lecturer);
                }

                // Check if email already exists
                if (await _context.Lecturers.AnyAsync(l => l.Email == lecturer.Email))
                {
                    ModelState.AddModelError("Email",
                        "This email address is already registered.");
                    return View(lecturer);
                }

                // Set default password (should be changed on first login)
                // In production, use BCrypt or similar for password hashing
                lecturer.PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!");

                // Set default values
                lecturer.Role = "Lecturer";
                lecturer.IsActive = true;
                lecturer.CreatedDate = DateTime.Now;

                // Add to database
                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Lecturer {lecturer.Name} added successfully. Default password: ChangeMe123!";
                return RedirectToAction("Index");
            }

            return View(lecturer);
        }

        /// <summary>
        /// GET: Display the form for editing an existing lecturer's details.
        /// HR uses this to update rates, contact information, or deactivate accounts.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditLecturer(string id)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                return Unauthorized();
            }

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            return View(lecturer);
        }

        /// <summary>
        /// POST: Update lecturer information.
        /// AUTOMATION KEY FEATURE: When the hourly rate is updated here, it automatically
        /// affects all future claims, ensuring consistency across the system.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(Lecturer lecturer)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                return Unauthorized();
            }

            // Remove password hash from validation (not updating password here)
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                var existing = await _context.Lecturers.FindAsync(lecturer.LecturerId);
                if (existing == null)
                {
                    return NotFound();
                }

                // Update only allowed fields (preserve password and creation date)
                existing.Name = lecturer.Name;
                existing.Email = lecturer.Email;
                existing.HourlyRate = lecturer.HourlyRate; // AUTOMATION: Central rate control
                existing.Department = lecturer.Department;
                existing.IsActive = lecturer.IsActive;
                existing.Role = lecturer.Role;
                existing.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Lecturer {lecturer.Name} updated successfully. " +
                    $"New rate: R{lecturer.HourlyRate}/hour will apply to all future claims.";
                return RedirectToAction("Index");
            }

            return View(lecturer);
        }

        /// <summary>
        /// POST: Deactivate a lecturer account.
        /// Deactivated lecturers cannot log in or submit claims but historical data is preserved.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateLecturer(string id)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                return Unauthorized();
            }

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            lecturer.IsActive = false;
            lecturer.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Lecturer {lecturer.Name} has been deactivated.";
            return RedirectToAction("Index");
        }

        #endregion

        #region Automated Reporting

        /// <summary>
        /// GET: Generate and display a comprehensive payment report.
        /// AUTOMATION FEATURE: Uses LINQ to aggregate approved claims and calculate totals.
        /// Generates invoice-ready data for the finance department.
        /// </summary>
        public async Task<IActionResult> GenerateReport(DateTime? startDate, DateTime? endDate)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR" && role != "Manager")
            {
                TempData["Error"] = "Access denied. HR or Manager privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // Default to current month if dates not specified
            var start = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = endDate ?? DateTime.Now;

            // AUTOMATION: Use the automation service to generate the report
            var report = await _automationService.GeneratePaymentReportAsync(start, end);

            // Pass date range to view for display
            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            return View(report);
        }

        /// <summary>
        /// GET: Export payment report as CSV file for accounting systems.
        /// AUTOMATION: Generates CSV format suitable for import into financial software.
        /// </summary>
        public async Task<IActionResult> ExportReportCSV(DateTime? startDate, DateTime? endDate)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR" && role != "Manager")
            {
                return Unauthorized();
            }

            // Default to current month if dates not specified
            var start = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = endDate ?? DateTime.Now;

            // Generate report using automation service
            var report = await _automationService.GeneratePaymentReportAsync(start, end);

            // Build CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Lecturer ID,Lecturer Name,Total Hours,Total Amount,Claim Count");

            foreach (var breakdown in report.ClaimBreakdown)
            {
                csv.AppendLine($"{breakdown.LecturerId},{breakdown.LecturerName}," +
                              $"{breakdown.TotalHours},{breakdown.TotalAmount:F2}," +
                              $"{breakdown.ClaimCount}");
            }

            csv.AppendLine();
            csv.AppendLine($"TOTAL CLAIMS:,{report.TotalClaims}");
            csv.AppendLine($"TOTAL AMOUNT:,R{report.TotalAmount:N2}");

            // Return CSV file for download
            var filename = $"Payment_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", filename);
        }

        /// <summary>
        /// GET: Display a dashboard with HR statistics and key metrics.
        /// AUTOMATION: Real-time analytics using LINQ queries.
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                TempData["Error"] = "Access denied. HR privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // AUTOMATION: Calculate key metrics using LINQ
            var stats = new HRDashboardStats
            {
                TotalLecturers = await _context.Lecturers.CountAsync(),
                ActiveLecturers = await _context.Lecturers.CountAsync(l => l.IsActive),
                TotalClaims = await _context.Claims.CountAsync(),
                PendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved),
                RejectedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Rejected),
                TotalPaymentsThisMonth = await _context.Claims
                    .Where(c => c.Status == ClaimStatus.Approved
                             && c.SubmitDate.Month == DateTime.Now.Month
                             && c.SubmitDate.Year == DateTime.Now.Year)
                    .SumAsync(c => c.Amount),
                AverageClaimAmount = await _context.Claims
                    .Where(c => c.Status == ClaimStatus.Approved)
                    .AverageAsync(c => (double?)c.Amount) ?? 0
            };

            return View(stats);
        }

        /// <summary>
        /// GET: Display audit log of all claim status changes.
        /// Provides transparency and accountability in the approval process.
        /// </summary>
        public async Task<IActionResult> AuditLog()
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                TempData["Error"] = "Access denied. HR privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // Retrieve all claims with status change history
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.LastUpdated != null)
                .OrderByDescending(c => c.LastUpdated)
                .Take(100) // Last 100 status changes
                .ToListAsync();

            return View(claims);
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// POST: Reset passwords for multiple lecturers (e.g., after security incident).
        /// AUTOMATION: Bulk operation to improve efficiency.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkResetPasswords(List<string> lecturerIds)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "HR")
            {
                return Unauthorized();
            }

            if (lecturerIds == null || !lecturerIds.Any())
            {
                TempData["Error"] = "No lecturers selected.";
                return RedirectToAction("Index");
            }

            var lecturers = await _context.Lecturers
                .Where(l => lecturerIds.Contains(l.LecturerId))
                .ToListAsync();

            var defaultPassword = "ChangeMe123!";
            foreach (var lecturer in lecturers)
            {
                lecturer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
                lecturer.LastUpdated = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{lecturers.Count} password(s) reset to default: {defaultPassword}";
            return RedirectToAction("Index");
        }

        #endregion
    }

    #region HR Dashboard Statistics Model

    /// <summary>
    /// View model for HR dashboard statistics.
    /// Contains aggregated metrics for management overview.
    /// </summary>
    public class HRDashboardStats
    {
        public int TotalLecturers { get; set; }
        public int ActiveLecturers { get; set; }
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalPaymentsThisMonth { get; set; }
        public double AverageClaimAmount { get; set; }
    }

    #endregion
}