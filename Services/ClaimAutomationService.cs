using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract_Monthly_Claim_System.Services
{
    /// <summary>
    /// Core Service for POE Part 3 Automation Requirements.
    /// Handles server-side calculations, business rule validation, and automated verification.
    /// </summary>
    public class ClaimAutomationService
    {
        private readonly AppDbContext _context;

        public ClaimAutomationService(AppDbContext context)
        {
            _context = context;
        }

        // AUTOMATION 1: Server-Side Calculation (Prevents tampering)
        public decimal CalculateClaimAmount(double hoursWorked, decimal hourlyRate)
        {
            return (decimal)hoursWorked * hourlyRate;
        }

        // AUTOMATION 2: Validation Rules (Checklist Requirement)
        public async Task<ValidationResult> ValidateClaimAsync(Claim claim)
        {
            var result = new ValidationResult { IsValid = true };

            // Rule: Max 180 hours per month (Updated from Checklist)
            if (claim.HoursWorked > 180)
            {
                result.IsValid = false;
                result.Errors.Add("Hours worked cannot exceed 180 hours per month.");
            }

            // Rule: Prevent Duplicate Claims for the same period
            var existingClaim = await _context.Claims
                .FirstOrDefaultAsync(c => c.LecturerId == claim.LecturerId
                                       && c.ClaimPeriod == claim.ClaimPeriod
                                       && c.Id != claim.Id
                                       && c.Status != ClaimStatus.Rejected); // Allow re-submission if rejected

            if (existingClaim != null)
            {
                result.IsValid = false;
                result.Errors.Add($"A pending or approved claim for period {claim.ClaimPeriod} already exists.");
            }

            return result;
        }

        // AUTOMATION 3: Automated Verification (Coordinator View)
        public async Task<VerificationResult> AutoVerifyClaimAsync(string claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null) return new VerificationResult { Success = false, Message = "Claim not found." };

            var result = new VerificationResult { Success = true };

            // Auto-check: Large Claims (> R10k)
            if (claim.Amount > 10000)
            {
                claim.Notes += "\n[AUTO-FLAG]: High Value Claim (>R10k).";
                result.RequiresManagerReview = true;
                result.Message = "Flagged for High Value.";
            }

            // Auto-check: Rate Consistency (Matches HR Database?)
            var lecturer = await _context.Lecturers.FindAsync(claim.LecturerId);
            if (lecturer != null && claim.HourlyRate != lecturer.HourlyRate)
            {
                claim.Notes += $"\n[AUTO-FLAG]: Rate Mismatch (Claim: {claim.HourlyRate}, HR: {lecturer.HourlyRate}).";
                result.RequiresManagerReview = true;
                result.Message += " Rate Mismatch detected.";
            }

            // Move to UnderReview automatically if valid
            claim.Status = ClaimStatus.UnderReview;
            claim.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            return result;
        }

        // AUTOMATION 4: Reporting (HR View)
        public async Task<PaymentReportViewModel> GeneratePaymentReportAsync(DateTime startDate, DateTime endDate)
        {
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                            c.SubmitDate >= startDate &&
                            c.SubmitDate <= endDate)
                .ToListAsync();

            var report = new PaymentReportViewModel
            {
                GeneratedDate = DateTime.Now,
                StartDate = startDate,
                EndDate = endDate,
                TotalClaims = approvedClaims.Count,
                TotalAmount = approvedClaims.Sum(c => c.Amount),
                ClaimBreakdown = approvedClaims
                    .GroupBy(c => c.LecturerId)
                    .Select(g => new LecturerPaymentBreakdown
                    {
                        LecturerId = g.Key,
                        LecturerName = g.First().Lecturer?.Name ?? "Unknown",
                        ClaimCount = g.Count(),
                        TotalHours = g.Sum(c => c.HoursWorked),
                        TotalAmount = g.Sum(c => c.Amount)
                    })
                    .ToList()
            };

            return report;
        }
    }

    // Helper Classes
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public bool RequiresManagerReview { get; set; }
        public string Message { get; set; }
    }
}