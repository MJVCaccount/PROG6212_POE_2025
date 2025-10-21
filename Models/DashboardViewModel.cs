using System.Collections.Generic;

namespace Contract_Monthly_Claim_System.Models
{
    public class DashboardViewModel
    {
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int UnderReviewClaims { get; set; }
        public decimal TotalEarnings { get; set; }
        public List<Claim> RecentClaims { get; set; }
    }
}