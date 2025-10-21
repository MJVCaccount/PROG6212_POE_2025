using Contract_Monthly_Claim_System.Models; // Explicitly qualify Claim
using Contract_Monthly_Claim_System.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Contract_Monthly_Claim_System.Controllers
{
    public class ClaimController : Controller
    {
        private readonly ClaimService _claimService;
        private readonly EncryptionService _encryptionService;

        public ClaimController(ClaimService claimService, EncryptionService encryptionService)
        {
            _claimService = claimService;
            _encryptionService = encryptionService;
        }

        // Submit Claim GET
        [HttpGet]
        public IActionResult Submit()
        {
            var model = new Contract_Monthly_Claim_System.Models.Claim { LecturerId = "IIE2024001", HourlyRate = 350.00m };
            return View(model);
        }

        // Submit Claim POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Contract_Monthly_Claim_System.Models.Claim model, List<IFormFile> Documents)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.Id = Guid.NewGuid().ToString();
                model.Status = ClaimStatus.Pending;
                model.SubmitDate = DateTime.Now;
                model.Amount = (decimal)model.HoursWorked * model.HourlyRate; // Cast double to decimal

                foreach (var file in Documents)
                {
                    if (file != null && file.Length > 0)
                    {
                        if (file.Length > 5 * 1024 * 1024) // 5MB limit
                        {
                            ModelState.AddModelError("Documents", "File too large (max 5MB).");
                            return View(model);
                        }

                        var ext = Path.GetExtension(file.FileName).ToLower();
                        if (ext != ".pdf" && ext != ".docx" && ext != ".xlsx")
                        {
                            ModelState.AddModelError("Documents", "Invalid file type (only .pdf, .docx, .xlsx).");
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

                _claimService.AddClaim(model);
                TempData["Success"] = "Claim submitted successfully.";
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error submitting claim: {ex.Message}");
                return View(model);
            }
        }

        // View Claims
        public IActionResult ViewClaims()
        {
            var claims = _claimService.GetAllClaims();
            return View(claims);
        }

        // Coordinator Approve GET
        public IActionResult CoordinatorApprove()
        {
            var pending = _claimService.GetClaimsByStatus(ClaimStatus.Pending);
            return View(pending);
        }

        // Coordinator Verify POST
        [HttpPost]
        public IActionResult Verify(string id)
        {
            _claimService.UpdateStatus(id, ClaimStatus.UnderReview);
            TempData["Success"] = "Claim verified and moved to under review.";
            return RedirectToAction("CoordinatorApprove");
        }

        // Coordinator Reject POST
        [HttpPost]
        public IActionResult RejectCoordinator(string id)
        {
            _claimService.UpdateStatus(id, ClaimStatus.Rejected);
            TempData["Success"] = "Claim rejected.";
            return RedirectToAction("CoordinatorApprove");
        }

        // Manager Approve GET
        public IActionResult ManagerApprove()
        {
            var underReview = _claimService.GetClaimsByStatus(ClaimStatus.UnderReview);
            return View(underReview);
        }

        // Manager Approve POST
        [HttpPost]
        public IActionResult Approve(string id)
        {
            _claimService.UpdateStatus(id, ClaimStatus.Approved);
            TempData["Success"] = "Claim approved.";
            return RedirectToAction("ManagerApprove");
        }

        // Manager Reject POST
        [HttpPost]
        public IActionResult RejectManager(string id)
        {
            _claimService.UpdateStatus(id, ClaimStatus.Rejected);
            TempData["Success"] = "Claim rejected.";
            return RedirectToAction("ManagerApprove");
        }

        // Download Document
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string docId)
        {
            var doc = _claimService.GetAllClaims().SelectMany(c => c.Documents).FirstOrDefault(d => d.Id == docId);
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