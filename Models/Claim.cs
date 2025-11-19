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

        [Required(ErrorMessage = "Lecturer ID is required")]
        [ForeignKey("Lecturer")]
        public string LecturerId { get; set; }

        public virtual Lecturer Lecturer { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(1, 160, ErrorMessage = "Hours must be between 1 and 160")]
        [Display(Name = "Hours Worked")]
        public double HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(200, 500, ErrorMessage = "Hourly rate must be between R200 and R500")]
        [Display(Name = "Hourly Rate")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Module is required")]
        [MaxLength(50)]
        public string Module { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Required]
        [Display(Name = "Submission Date")]
        public DateTime SubmitDate { get; set; }

        [Required(ErrorMessage = "Claim period is required")]
        [Display(Name = "Claim Period")]
        [MaxLength(7)] // Format: YYYY-MM
        public string ClaimPeriod { get; set; }

        public virtual List<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();

        public DateTime? LastUpdated { get; set; }

        [MaxLength(50)]
        public string LastModifiedBy { get; set; }
    }

    public enum ClaimStatus
    {
        Pending = 0,
        UnderReview = 1,
        Approved = 2,
        Rejected = 3
    }
}