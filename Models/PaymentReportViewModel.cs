using System;
using System.Collections.Generic;

namespace Contract_Monthly_Claim_System.Models
{
    public class PaymentReportViewModel
    {
        public DateTime GeneratedDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public List<LecturerPaymentBreakdown> ClaimBreakdown { get; set; } = new List<LecturerPaymentBreakdown>();
    }

    public class LecturerPaymentBreakdown
    {
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public int ClaimCount { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
    }
}