using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract_Monthly_Claim_System.Services
{
    public class ClaimAutomationService
    {
        private readonly AppDbContext _context;

        public ClaimAutomationService(AppDbContext context)
        {
            _context = context;
        }

        // AUTOMATION 1: Calculate Amount Server-Side
        public decimal CalculateClaimAmount(double hoursWorked, decimal hourlyRate)
        {
            return (decimal)hoursWorked * hourlyRate;
        }

        // AUTOMATION 2: Validate Business Rules
        public async Task<ValidationResult> ValidateClaimAsync(Claim claim)
        {
            var result = new ValidationResult { IsValid = true };

            // Rule: Check for duplicate claims for the same period
            var existingClaim = await _context.Claims
                .FirstOrDefaultAsync(c => c.LecturerId == claim.LecturerId
                                       && c.ClaimPeriod == claim.ClaimPeriod
                                       && c.Id != claim.Id);

            if (existingClaim != null)
            {
                result.IsValid = false;
                result.Errors.Add($"A claim for period {claim.ClaimPeriod} already exists.");
            }

            // Rule: Max hours per month
            if (claim.HoursWorked > 160)
            {
                result.IsValid = false;
                result.Errors.Add("Hours worked cannot exceed 160 hours per month.");
            }

            return result;
        }

        // AUTOMATION 3: Auto-Verification for Coordinators
        public async Task<VerificationResult> AutoVerifyClaimAsync(string claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null) return new VerificationResult { Success = false, Message = "Claim not found." };

            var result = new VerificationResult { Success = true };

            // Auto-check: Large Claims
            if (claim.Amount > 10000)
            {
                claim.Notes += "\n[AUTO-FLAG]: High Value Claim (>R10k).";
                result.RequiresManagerReview = true;
                result.Message = "Flagged for High Value.";
            }

            // Auto-check: Rate consistency
            var lecturer = await _context.Lecturers.FindAsync(claim.LecturerId);
            if (lecturer != null && claim.HourlyRate != lecturer.HourlyRate)
            {
                claim.Notes += $"\n[AUTO-FLAG]: Rate Mismatch (Claim: {claim.HourlyRate}, HR: {lecturer.HourlyRate}).";
                result.RequiresManagerReview = true;
                result.Message += " Rate Mismatch detected.";
            }

            // Update claim status if passed
            claim.Status = ClaimStatus.UnderReview;
            claim.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            return result;
        }

        // AUTOMATION 4: Generate Payment Report (Fixed Missing Method)
        public async Task<PaymentReportViewModel> GeneratePaymentReportAsync(DateTime startDate, DateTime endDate)
        {
            // Fetch all approved claims within the date range
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved &&
                            c.SubmitDate >= startDate &&
                            c.SubmitDate <= endDate)
                .ToListAsync();

            // Build the report using LINQ aggregation
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

    // Helper classes for automation results
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