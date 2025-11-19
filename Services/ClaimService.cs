using Contract_Monthly_Claim_System.Models;
using System.Text;

namespace Contract_Monthly_Claim_System.Services
{
    public class ClaimService
    {
        private static List<Claim> _claims = new List<Claim>();
        private readonly object _lock = new object();

        public void AddClaim(Claim claim)
        {
            lock (_lock)
            {
                _claims.Add(claim);
            }
        }

        public List<Claim> GetAllClaims()
        {
            lock (_lock)
            {
                return _claims.ToList();
            }
        }

        public List<Claim> GetClaimsByStatus(ClaimStatus status)
        {
            lock (_lock)
            {
                return _claims.Where(c => c.Status == status).ToList();
            }
        }

        public void UpdateStatus(string id, ClaimStatus newStatus)
        {
            lock (_lock)
            {
                var claim = _claims.FirstOrDefault(c => c.Id == id);
                if (claim != null) claim.Status = newStatus;
            }
        }

        // --- AUTOMATION: System Verification Checks ---
        public string VerifyClaimRules(Claim claim, decimal expectedRate)
        {
            var sb = new StringBuilder();

            // Rule 1: Check if hours exceed policy limit (e.g., 100 hours/month)
            if (claim.HoursWorked > 100)
                sb.Append("⚠️ High Hours (check policy). ");

            // Rule 2: Check if Rate matches the HR contract rate
            if (claim.HourlyRate != expectedRate)
                sb.Append($"⚠️ Rate Mismatch (Claimed: {claim.HourlyRate}, Contract: {expectedRate}). ");

            // Rule 3: Automate "Good" claims
            if (claim.HoursWorked < 100 && claim.HourlyRate == expectedRate)
                return "✅ Auto-Check Passed";

            return sb.ToString();
        }

        // --- AUTOMATION: Reporting (LINQ) ---
        public string GenerateMonthlyReport()
        {
            var approvedClaims = _claims.Where(c => c.Status == ClaimStatus.Approved).ToList();
            var totalPayout = approvedClaims.Sum(c => c.Amount);

            var report = new StringBuilder();
            report.AppendLine("MONTHLY PAYMENT REPORT");
            report.AppendLine("------------------------------------------------");
            report.AppendLine($"Date Generated: {DateTime.Now}");
            report.AppendLine($"Total Approved Claims: {approvedClaims.Count}");
            report.AppendLine($"Total Payout: R {totalPayout:N2}");
            report.AppendLine("------------------------------------------------");
            foreach (var claim in approvedClaims)
            {
                report.AppendLine($"{claim.LecturerId} | {claim.Module} | R {claim.Amount:N2}");
            }
            return report.ToString();
        }
    }
}