using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Contract_Monthly_Claim_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Contract_Monthly_Claim_System.Controllers
{
    public class ClaimController : Controller
    {
        private readonly ClaimAutomationService _automationService;
        private readonly EncryptionService _encryptionService;
        private readonly AppDbContext _context;

        public ClaimController(
            ClaimAutomationService automationService,
            EncryptionService encryptionService,
            AppDbContext context)
        {
            _automationService = automationService;
            _encryptionService = encryptionService;
            _context = context;
        }

        // ============================================================
        // LECTURER VIEWS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            var lecturerId = HttpContext.Session.GetString("LecturerId");

            if (string.IsNullOrEmpty(lecturerId))
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var lecturer = await _context.Lecturers.FindAsync(lecturerId);

            if (lecturer == null)
            {
                TempData["Error"] = "Lecturer not found.";
                return RedirectToAction("Index", "Home");
            }

            var model = new Claim
            {
                LecturerId = lecturer.LecturerId,
                HourlyRate = lecturer.HourlyRate,
                ClaimPeriod = DateTime.Now.ToString("yyyy-MM")
            };

            ViewBag.LecturerName = lecturer.Name;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim model, List<IFormFile> Documents)
        {
            ModelState.Remove("Amount");
            ModelState.Remove("Lecturer");

            if (!ModelState.IsValid)
            {
                var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                ViewBag.LecturerName = lec?.Name;
                return View(model);
            }

            try
            {
                // STEP 1: Create claim
                model.Id = Guid.NewGuid().ToString();
                model.Status = ClaimStatus.Pending;
                model.SubmitDate = DateTime.Now;

                // Automation: Calculate Amount
                model.Amount = _automationService.CalculateClaimAmount(model.HoursWorked, model.HourlyRate);

                // Automation: Validate Logic
                var validation = await _automationService.ValidateClaimAsync(model);

                if (!validation.IsValid)
                {
                    foreach (var error in validation.Errors)
                        ModelState.AddModelError("", error);

                    var lec = await _context.Lecturers.FindAsync(model.LecturerId);
                    ViewBag.LecturerName = lec?.Name;
                    return View(model);
                }

                model.LastModifiedBy = model.LecturerId;
                model.LastUpdated = DateTime.Now;

                _context.Claims.Add(model);
                await _context.SaveChangesAsync();

                // STEP 2: Upload supporting documents
                foreach (var file in Documents)
                {
                    if (file != null && file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };

                        if (!allowedExtensions.Contains(extension))
                        {
                            // Note: In a real app, you might want to rollback the claim creation here
                            TempData["Warning"] = $"Claim saved, but file '{file.FileName}' was rejected (invalid type).";
                            continue;
                        }

                        using var stream = file.OpenReadStream();
                        var encrypted = await _encryptionService.EncryptAsync(stream);

                        var doc = new SupportingDocument
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClaimId = model.Id,
                            FileName = file.FileName,
                            EncryptedContent = encrypted
                        };

                        _context.SupportingDocuments.Add(doc);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Claim submitted successfully! Amount: R{model.Amount:N2}";
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }

        public async Task<IActionResult> ViewClaims()
        {
            var lecturerId = HttpContext.Session.GetString("LecturerId");

            if (string.IsNullOrEmpty(lecturerId))
                return RedirectToAction("Login", "Account");

            var claims = await _context.Claims
                .Include(c => c.Documents)
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.SubmitDate)
                .ToListAsync();

            return View(claims);
        }

        // ============================================================
        // COORDINATOR VIEWS & ACTIONS
        // ============================================================

        public async Task<IActionResult> CoordinatorApprove()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Coordinator" && role != "Manager" && role != "HR")
                return RedirectToAction("Index", "Home");

            var claims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Pending)
                .Include(c => c.Lecturer)
                .Include(c => c.Documents)
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Coordinator" && role != "Manager" && role != "HR")
                return Unauthorized();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            var result = await _automationService.AutoVerifyClaimAsync(id);

            if (result.Success)
                TempData["Success"] = "Claim verified.";
            else
                TempData["Error"] = $"Verification failed: {result.Message}";

            return RedirectToAction("CoordinatorApprove");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCoordinator(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Coordinator" && role != "Manager" && role != "HR")
                return Unauthorized();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Rejected;
            claim.LastUpdated = DateTime.Now;
            claim.LastModifiedBy = HttpContext.Session.GetString("LecturerId");
            claim.Notes += "\n[Rejected by Coordinator]";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Claim rejected.";
            return RedirectToAction("CoordinatorApprove");
        }

        // ============================================================
        // MANAGER VIEWS & ACTIONS
        // ============================================================

        public async Task<IActionResult> ManagerApprove()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager" && role != "HR")
                return RedirectToAction("Index", "Home");

            var claims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.UnderReview)
                .Include(c => c.Lecturer)
                .Include(c => c.Documents)
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager" && role != "HR")
                return Unauthorized();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Approved;
            claim.LastUpdated = DateTime.Now;
            claim.LastModifiedBy = HttpContext.Session.GetString("LecturerId");

            await _context.SaveChangesAsync();
            TempData["Success"] = "Claim approved successfully.";
            return RedirectToAction("ManagerApprove");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectManager(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Manager" && role != "HR")
                return Unauthorized();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Rejected;
            claim.LastUpdated = DateTime.Now;
            claim.LastModifiedBy = HttpContext.Session.GetString("LecturerId");
            claim.Notes += "\n[Rejected by Manager]";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Claim rejected.";
            return RedirectToAction("ManagerApprove");
        }

        // ============================================================
        // DOCUMENT MANAGEMENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string docId)
        {
            var doc = await _context.SupportingDocuments.FirstOrDefaultAsync(d => d.Id == docId);
            if (doc == null) return NotFound();

            var decrypted = await _encryptionService.DecryptAsync(doc.EncryptedContent);
            return File(decrypted, "application/octet-stream", doc.FileName);
        }

        // =========================================================
        // SAFETY REDIRECTS (Prevents HTTP 405 Errors)
        // =========================================================

        [HttpGet] // Catches users typing /Claim/Approve in the browser
        public IActionResult Approve()
        {
            // Redirect to the dashboard instead of showing an error
            return RedirectToAction("ManagerApprove");
        }

        [HttpGet]
        public IActionResult RejectManager()
        {
            return RedirectToAction("ManagerApprove");
        }

        [HttpGet]
        public IActionResult Verify()
        {
            return RedirectToAction("CoordinatorApprove");
        }

        [HttpGet]
        public IActionResult RejectCoordinator()
        {
            return RedirectToAction("CoordinatorApprove");
        }
             
    }

}