using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Contract_Monthly_Claim_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Contract_Monthly_Claim_System.Controllers
{
    /// <summary>
    /// Main controller for claim management with full automation features.
    /// Handles lecturer submissions, coordinator verification, and manager approvals.
    /// Uses session-based authentication (not ASP.NET Identity as per requirements).
    /// </summary>
    public class ClaimController : Controller
    {
        private readonly ClaimAutomationService _automationService;
        private readonly EncryptionService _encryptionService;
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor with dependency injection of required services.
        /// </summary>
        public ClaimController(ClaimAutomationService automationService,
                               EncryptionService encryptionService,
                               AppDbContext context)
        {
            _automationService = automationService;
            _encryptionService = encryptionService;
            _context = context;
        }

        #region Lecturer Views and Actions

        /// <summary>
        /// GET: Display the claim submission form for lecturers.
        /// AUTOMATION: Pre-fills the hourly rate from the HR database (Lecturer table).
        /// This prevents manual rate entry and ensures contract compliance.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            // 1. Retrieve logged-in lecturer ID from session
            var lecturerId = HttpContext.Session.GetString("LecturerId");

            // Redirect to login if session expired or user not authenticated
            if (string.IsNullOrEmpty(lecturerId))
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // 2. Fetch lecturer details from database for automation
            var lecturer = await _context.Lecturers.FindAsync(lecturerId);

            // Safety check: redirect if lecturer not found in database
            if (lecturer == null)
            {
                TempData["Error"] = "Lecturer profile not found. Contact HR.";
                return RedirectToAction("Index", "Home");
            }

            // 3. AUTOMATION: Pre-fill the claim model with lecturer data
            // The hourly rate is pulled automatically from HR records
            var model = new Claim
            {
                LecturerId = lecturer.LecturerId,
                HourlyRate = lecturer.HourlyRate, // Automated rate lookup
                ClaimPeriod = DateTime.Now.ToString("yyyy-MM") // Default to current month
            };

            // Pass lecturer name to view for display purposes
            ViewBag.LecturerName = lecturer.Name;

            return View(model);
        }

        /// <summary>
        /// POST: Process claim submission with automated validation and calculation.
        /// AUTOMATION FEATURES:
        /// 1. Server-side amount calculation (prevents tampering)
        /// 2. Validation against business rules
        /// 3. File encryption for uploaded documents
        /// 4. Duplicate claim detection
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim model, List<IFormFile> Documents)
        {
            // Remove Amount from model validation since it's auto-calculated
            ModelState.Remove("Amount");
            ModelState.Remove("Lecturer");

            if (!ModelState.IsValid)
            {
                // Re-populate lecturer name for view display on error
                var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                ViewBag.LecturerName = lec?.Name;
                return View(model);
            }

            try
            {
                // 1. Generate unique claim ID
                model.Id = Guid.NewGuid().ToString();
                model.Status = ClaimStatus.Pending;
                model.SubmitDate = DateTime.Now;

                // 2. AUTOMATION: Calculate amount server-side using automation service
                // This prevents client-side manipulation of the total amount
                model.Amount = _automationService.CalculateClaimAmount(
                    model.HoursWorked,
                    model.HourlyRate
                );

                // 3. AUTOMATION: Validate claim against business rules
                var validation = await _automationService.ValidateClaimAsync(model);
                if (!validation.IsValid)
                {
                    foreach (var error in validation.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }

                    var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                    ViewBag.LecturerName = lec?.Name;
                    return View(model);
                }

                // 4. AUTOMATION: Handle document uploads with encryption
                foreach (var file in Documents)
                {
                    if (file != null && file.Length > 0)
                    {
                        // File size validation (5MB limit)
                        if (file.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("Documents",
                                $"File '{file.FileName}' exceeds 5MB limit.");

                            var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                            ViewBag.LecturerName = lec?.Name;
                            return View(model);
                        }

                        // File type validation
                        var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("Documents",
                                $"File type '{extension}' not allowed. Use PDF, DOCX, or XLSX.");

                            var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                            ViewBag.LecturerName = lec?.Name;
                            return View(model);
                        }

                        // Encrypt file content for security
                        using var stream = file.OpenReadStream();
                        var encrypted = await _encryptionService.EncryptAsync(stream);

                        // Create document record with encryption
                        var doc = new SupportingDocument
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClaimId = model.Id,
                            FileName = file.FileName,
                            EncryptedContent = encrypted
                        };
                        model.Documents.Add(doc);
                    }
                }

                // 5. Save claim to database
                _context.Claims.Add(model);
                await _context.SaveChangesAsync();

                // Success notification
                TempData["Success"] = $"Claim submitted successfully! Total amount: R{model.Amount:N2}";
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                // Log error and display user-friendly message
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");

                var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                ViewBag.LecturerName = lec?.Name;
                return View(model);
            }
        }

        /// <summary>
        /// GET: Display all claims for the logged-in lecturer.
        /// Shows claim status with visual progress tracking.
        /// </summary>
        public async Task<IActionResult> ViewClaims()
        {
            // Get logged-in lecturer ID from session
            var lecturerId = HttpContext.Session.GetString("LecturerId");

            if (string.IsNullOrEmpty(lecturerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Retrieve all claims for this lecturer with documents included
            var claims = await _context.Claims
                .Include(c => c.Documents)
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.SubmitDate)
                .ToListAsync();

            return View(claims);
        }

        #endregion

        #region Coordinator Views and Actions

        /// <summary>
        /// GET: Display all pending claims for coordinator verification.
        /// Coordinators see all claims in "Pending" status.
        /// </summary>
        public async Task<IActionResult> CoordinatorApprove()
        {
            // Session-based authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Coordinator" && role != "Manager" && role != "HR")
            {
                TempData["Error"] = "Access denied. Coordinator privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // Retrieve all pending claims with lecturer details
            var pendingClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Pending)
                .Include(c => c.Documents)
                .Include(c => c.Lecturer)
                .OrderBy(c => c.SubmitDate)
                .ToListAsync();

            return View(pendingClaims);
        }

        /// <summary>
        /// POST: Verify a claim with automated checks.
        /// AUTOMATION: Runs auto-verification rules and flags issues for manager review.
        /// Auto-checks include: hours validation, rate matching, document presence.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string id)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Coordinator" && role != "Manager" && role != "HR")
            {
                return Unauthorized();
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            // AUTOMATION: Run automated verification checks
            var verificationResult = await _automationService.AutoVerifyClaimAsync(id);

            if (verificationResult.Success)
            {
                // Set success message with verification details
                if (verificationResult.RequiresManagerReview)
                {
                    TempData["Warning"] = $"Claim verified with flags: {verificationResult.Message}";
                }
                else
                {
                    TempData["Success"] = "Claim verified successfully. All automated checks passed.";
                }
            }
            else
            {
                TempData["Error"] = $"Verification failed: {verificationResult.Message}";
            }

            return RedirectToAction("CoordinatorApprove");
        }

        /// <summary>
        /// POST: Reject a claim at the coordinator level.
        /// Rejected claims cannot be reprocessed without resubmission.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCoordinator(string id, string rejectionReason)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Coordinator" && role != "Manager" && role != "HR")
            {
                return Unauthorized();
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            // Update claim status and append rejection reason
            claim.Status = ClaimStatus.Rejected;
            claim.LastUpdated = DateTime.Now;
            claim.LastModifiedBy = HttpContext.Session.GetString("UserId");
            claim.Notes += $"\n\n[REJECTED BY COORDINATOR]\nReason: {rejectionReason ?? "No reason provided"}";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim rejected successfully.";
            return RedirectToAction("CoordinatorApprove");
        }

        #endregion

        #region Manager Views and Actions

        /// <summary>
        /// GET: Display all claims under review for manager final approval.
        /// Managers see claims that have passed coordinator verification.
        /// </summary>
        public async Task<IActionResult> ManagerApprove()
        {
            // Session-based authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager" && role != "HR")
            {
                TempData["Error"] = "Access denied. Manager privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // Retrieve all claims in UnderReview status
            var reviewClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.UnderReview)
                .Include(c => c.Documents)
                .Include(c => c.Lecturer)
                .OrderBy(c => c.SubmitDate)
                .ToListAsync();

            return View(reviewClaims);
        }

        /// <summary>
        /// POST: Approve a claim for payment processing.
        /// Final approval moves the claim to "Approved" status.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager" && role != "HR")
            {
                return Unauthorized();
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            // Update claim to approved status
            claim.Status = ClaimStatus.Approved;
            claim.LastUpdated = DateTime.Now;
            claim.LastModifiedBy = HttpContext.Session.GetString("UserId");
            claim.Notes += $"\n\n[APPROVED BY MANAGER]\nDate: {DateTime.Now:yyyy-MM-dd HH:mm}";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Claim approved! Payment of R{claim.Amount:N2} will be processed.";
            return RedirectToAction("ManagerApprove");
        }

        /// <summary>
        /// POST: Reject a claim at the manager level.
        /// Manager rejections are final and require detailed reasoning.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectManager(string id, string rejectionReason)
        {
            // Authorization check
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager" && role != "HR")
            {
                return Unauthorized();
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            // Update claim status with detailed rejection notes
            claim.Status = ClaimStatus.Rejected;
            claim.LastUpdated = DateTime.Now;
            claim.LastModifiedBy = HttpContext.Session.GetString("UserId");
            claim.Notes += $"\n\n[REJECTED BY MANAGER]\nReason: {rejectionReason ?? "No reason provided"}";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim rejected by manager.";
            return RedirectToAction("ManagerApprove");
        }

        #endregion

        #region Document Management

        /// <summary>
        /// GET: Download and decrypt a supporting document.
        /// Documents are stored encrypted and decrypted on-demand for security.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string docId)
        {
            // Retrieve document from database
            var doc = await _context.SupportingDocuments
                .FirstOrDefaultAsync(d => d.Id == docId);

            if (doc == null)
            {
                return NotFound("Document not found.");
            }

            try
            {
                // Decrypt document content
                var decrypted = await _encryptionService.DecryptAsync(doc.EncryptedContent);

                // Return file for download with original filename
                return File(decrypted, "application/octet-stream", doc.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading document: {ex.Message}");
            }
        }

        #endregion
    }
}