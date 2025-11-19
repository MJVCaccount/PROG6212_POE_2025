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
    public class ClaimController : Controller
    {
        private readonly ClaimService _claimService;
        private readonly EncryptionService _encryptionService;
        private readonly AppDbContext _context;

        public ClaimController(ClaimService claimService, EncryptionService encryptionService, AppDbContext context)
        {
            _claimService = claimService;
            _encryptionService = encryptionService;
            _context = context;
        }

        // --- LECTURER VIEW ---

        [HttpGet]
        public IActionResult Submit()
        {
            // 1. Get the logged-in Lecturer ID from Session
            var lecturerId = HttpContext.Session.GetString("LecturerId");

            // Redirect to Login if session is expired or missing
            if (string.IsNullOrEmpty(lecturerId))
                return RedirectToAction("Login", "Account");

            // 2. AUTOMATION: Fetch the rate from the DB
            var lecturer = _context.Lecturers.Find(lecturerId);

            // Safety check: If lecturer not found, return to home or error page
            if (lecturer == null) return RedirectToAction("Index", "Home");

            // 3. Pre-fill the model. Rate is now automated, not manual.
            var model = new Claim
            {
                LecturerId = lecturer.LecturerId,
                HourlyRate = lecturer.HourlyRate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim model, List<IFormFile> Documents)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                model.Id = Guid.NewGuid().ToString();
                model.Status = ClaimStatus.Pending;
                model.SubmitDate = DateTime.Now;

                // AUTOMATION: Calculate Amount on Server to prevent tampering
                model.Amount = (decimal)model.HoursWorked * model.HourlyRate;

                // File Handling Loop
                foreach (var file in Documents)
                {
                    if (file != null && file.Length > 0)
                    {
                        if (file.Length > 5 * 1024 * 1024) // 5MB Limit
                        {
                            ModelState.AddModelError("Documents", "File too large (max 5MB).");
                            return View(model);
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
                        model.Documents.Add(doc);
                    }
                }

                // Save to Database using Context directly for Part 3
                _context.Claims.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Claim submitted successfully.";
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }

        public IActionResult ViewClaims()
        {
            // Fix: Include Documents for the view
            var claims = _context.Claims.Include(c => c.Documents).ToList();
            return View(claims);
        }

        // --- COORDINATOR / MANAGER VIEW ---

        public IActionResult CoordinatorApprove()
        {
            var pending = _context.Claims
                .Where(c => c.Status == ClaimStatus.Pending)
                .Include(c => c.Documents) // Ensure docs are loaded
                .ToList();
            return View(pending);
        }

        [HttpPost]
        public IActionResult Verify(string id)
        {
            var claim = _context.Claims.Find(id);
            if (claim == null) return NotFound();

            // AUTOMATION: Verification Logic
            bool isHoursExcessive = claim.HoursWorked > 100;

            // Fix Dynamic Error: Extract variables before LINQ usage
            var lecId = claim.LecturerId;
            var rate = claim.HourlyRate;

            // Check if the submitted rate matches the official HR contract rate
            bool isRateValid = _context.Lecturers.Any(l => l.LecturerId == lecId && l.HourlyRate == rate);

            if (isHoursExcessive)
                claim.Notes += " [SYSTEM FLAG: Hours exceeded limit]";

            if (!isRateValid)
                claim.Notes += " [SYSTEM FLAG: Rate mismatch detected]";

            claim.Status = ClaimStatus.UnderReview;
            _context.SaveChanges();

            // FIX: Added missing return statement
            return RedirectToAction("CoordinatorApprove");
        }

        [HttpPost]
        public IActionResult RejectCoordinator(string id)
        {
            UpdateStatus(id, ClaimStatus.Rejected);
            return RedirectToAction("CoordinatorApprove");
        }

        public IActionResult ManagerApprove()
        {
            var underReview = _context.Claims.Where(c => c.Status == ClaimStatus.UnderReview).ToList();
            return View(underReview);
        }

        [HttpPost]
        public IActionResult Approve(string id)
        {
            UpdateStatus(id, ClaimStatus.Approved);
            return RedirectToAction("ManagerApprove");
        }

        [HttpPost]
        public IActionResult RejectManager(string id)
        {
            UpdateStatus(id, ClaimStatus.Rejected);
            return RedirectToAction("ManagerApprove");
        }

        // Helper to avoid repeating code
        private void UpdateStatus(string id, ClaimStatus status)
        {
            var claim = _context.Claims.Find(id);
            if (claim != null)
            {
                claim.Status = status;
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string docId)
        {
            // Search in DB directly
            var doc = _context.SupportingDocuments.FirstOrDefault(d => d.Id == docId);
            if (doc == null) return NotFound();

            try
            {
                var decrypted = await _encryptionService.DecryptAsync(doc.EncryptedContent);
                return File(decrypted, "application/octet-stream", doc.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading: {ex.Message}");
            }
        }
    }
}