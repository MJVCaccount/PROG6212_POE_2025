using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Contract_Monthly_Claim_System.Models
{
    public class Claim
    {
        [Key]
        [BindNever]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Lecturer ID is required")]
        [ForeignKey("Lecturer")]
        public string LecturerId { get; set; } = null!;

        public virtual Lecturer? Lecturer { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(1, 180, ErrorMessage = "Hours must be between 1 and 180")]
        public double HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(200, 500, ErrorMessage = "Hourly rate must be between R200 and R500")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [BindNever]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Module is required")]
        [MaxLength(50)]
        public string Module { get; set; } = null!;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [BindNever]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [BindNever]
        public DateTime SubmitDate { get; set; }

        [Required(ErrorMessage = "Claim period is required")]
        [MaxLength(7)]
        public string ClaimPeriod { get; set; } = null!;

        public virtual List<SupportingDocument> Documents { get; set; } = new();

        [BindNever]
        public DateTime? LastUpdated { get; set; }

        [MaxLength(50)]
        public string? LastModifiedBy { get; set; }
    }

    public enum ClaimStatus
    {
        Pending = 0,
        UnderReview = 1,
        Approved = 2,
        Rejected = 3
    }
}
