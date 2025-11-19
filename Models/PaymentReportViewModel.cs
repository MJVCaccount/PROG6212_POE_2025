using System;
using System.Collections.Generic;

namespace Contract_Monthly_Claim_System.Models
{
    /// <summary>
    /// View Model for the Monthly Payment Report.
    /// Used by HRController and ClaimAutomationService to structure report data.
    /// </summary>
    public class PaymentReportViewModel
    {
        public DateTime GeneratedDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Summary metrics
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }

        // Detailed breakdown list
        public List<LecturerPaymentBreakdown> ClaimBreakdown { get; set; } = new List<LecturerPaymentBreakdown>();
    }

    /// <summary>
    /// Helper class to store individual lecturer totals within the report.
    /// </summary>
    public class LecturerPaymentBreakdown
    {
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public int ClaimCount { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
    }
}