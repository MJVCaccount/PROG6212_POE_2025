using System.ComponentModel.DataAnnotations;

namespace Contract_Monthly_Claim_System.Models
{
    public class Claim
    {
        public string Id { get; set; }
        public string LecturerId { get; set; } = "IIE2024001"; // Hardcoded
        [Required(ErrorMessage = "Hours worked is required.")]
        [Range(1, 160, ErrorMessage = "Hours must be between 1 and 160.")]
        public double HoursWorked { get; set; }
        [Required(ErrorMessage = "Hourly rate is required.")]
        public decimal HourlyRate { get; set; }
        public decimal Amount { get; set; }
        [Required(ErrorMessage = "Module is required.")]
        public string Module { get; set; } // e.g., PROG6212
        public string Notes { get; set; }
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        public DateTime SubmitDate { get; set; }
        public List<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    }

    public enum ClaimStatus { Pending, UnderReview, Approved, Rejected }
}