using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contract_Monthly_Claim_System.Models
{
    public class Claim
    {
        [Key]
        public string Id { get; set; }

        [Required]
        [ForeignKey("Lecturer")] // Links to the Lecturer table
        public string LecturerId { get; set; }

        // NAVIGATION PROPERTY: This fixes the "Claim does not contain definition for Lecturer" error
        public virtual Lecturer Lecturer { get; set; }

        [Required]
        [Range(1, 160, ErrorMessage = "Hours must be between 1 and 160.")]
        public double HoursWorked { get; set; }

        [Required]
        public decimal HourlyRate { get; set; }

        public decimal Amount { get; set; }

        [Required]
        public string Module { get; set; }

        public string Notes { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public DateTime SubmitDate { get; set; }

        public List<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    }

    public enum ClaimStatus { Pending, UnderReview, Approved, Rejected }
}