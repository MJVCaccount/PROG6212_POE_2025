using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contract_Monthly_Claim_System.Models
{
    /// <summary>
    /// Represents a monthly claim submitted by a lecturer for contract work.
    /// This model includes all necessary fields for claim processing, automation, and tracking.
    /// </summary>
    public class Claim
    {
        /// <summary>
        /// Unique identifier for the claim (Primary Key).
        /// Generated using GUID to ensure uniqueness across distributed systems.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Foreign key linking to the Lecturer who submitted this claim.
        /// This enables automated rate lookup and validation against HR records.
        /// </summary>
        [Required(ErrorMessage = "Lecturer ID is required")]
        [ForeignKey("Lecturer")]
        public string LecturerId { get; set; }

        /// <summary>
        /// Navigation property for Entity Framework Core.
        /// Allows access to lecturer details without separate database queries.
        /// Used for automated rate verification and reporting.
        /// </summary>
        public virtual Lecturer Lecturer { get; set; }

        /// <summary>
        /// Total hours worked by the lecturer for this claim period.
        /// Validated to ensure realistic working hours (1-160 hours per month).
        /// Used in automated calculation: Amount = HoursWorked × HourlyRate
        /// </summary>
        [Required(ErrorMessage = "Hours worked is required")]
        [Range(1, 160, ErrorMessage = "Hours must be between 1 and 160 (approximately 1 month of full-time work)")]
        [Display(Name = "Hours Worked")]
        public double HoursWorked { get; set; }

        /// <summary>
        /// Hourly rate pulled automatically from the Lecturer table (HR-managed).
        /// This automation prevents manual rate entry errors and ensures contract compliance.
        /// Read-only for lecturers; only HR can modify rates in the Lecturer master table.
        /// </summary>
        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(200, 500, ErrorMessage = "Hourly rate must be between R200 and R500")]
        [Display(Name = "Hourly Rate")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        /// <summary>
        /// Total claim amount calculated server-side (not user-editable).
        /// AUTOMATION: Computed as HoursWorked × HourlyRate to prevent tampering.
        /// This field is always recalculated before saving to database.
        /// </summary>
        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Module or subject code for which the claim is submitted.
        /// Used for reporting and categorization (e.g., PROG6212, DATA6222).
        /// </summary>
        [Required(ErrorMessage = "Module is required")]
        [MaxLength(50)]
        public string Module { get; set; }

        /// <summary>
        /// Optional notes field for additional information.
        /// System-generated flags (e.g., "Rate Mismatch", "High Hours") are appended here
        /// during automated verification by the Coordinator approval workflow.
        /// </summary>
        [MaxLength(1000)]
        public string Notes { get; set; }

        /// <summary>
        /// Current status of the claim in the approval workflow.
        /// AUTOMATION: Status transitions are enforced:
        /// Pending → UnderReview (Coordinator) → Approved/Rejected (Manager)
        /// </summary>
        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        /// <summary>
        /// Timestamp when the claim was initially submitted.
        /// Used for audit trails and reporting (e.g., monthly summaries).
        /// </summary>
        [Required]
        [Display(Name = "Submission Date")]
        public DateTime SubmitDate { get; set; }

        /// <summary>
        /// Month and year for which this claim applies (e.g., "2025-11").
        /// Useful for generating period-specific reports and preventing duplicate claims.
        /// </summary>
        [Required(ErrorMessage = "Claim period is required")]
        [Display(Name = "Claim Period")]
        [MaxLength(7)] // Format: YYYY-MM
        public string ClaimPeriod { get; set; }

        /// <summary>
        /// Collection of supporting documents uploaded by the lecturer.
        /// Documents are encrypted for security (GDPR/data protection compliance).
        /// One claim can have multiple documents (e.g., timesheets, invoices, contracts).
        /// </summary>
        public virtual List<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();

        /// <summary>
        /// Timestamp for the last status update (for audit and tracking purposes).
        /// Automatically updated when Status changes via automated workflows.
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// User ID of the person who last modified the claim (Coordinator/Manager).
        /// Provides accountability in the approval process.
        /// </summary>
        [MaxLength(50)]
        public string LastModifiedBy { get; set; }
    }

    /// <summary>
    /// Enum representing the possible states of a claim in the approval workflow.
    /// This enforces a controlled state machine for claim processing automation.
    /// </summary>
    public enum ClaimStatus
    {
        /// <summary>
        /// Initial state: Claim has been submitted by lecturer but not yet reviewed.
        /// Visible to Programme Coordinators for verification.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Intermediate state: Claim has passed coordinator verification.
        /// Automated checks have been performed (rate validation, hour limits).
        /// Awaiting final approval from Academic Manager.
        /// </summary>
        UnderReview = 1,

        /// <summary>
        /// Final state: Claim has been approved by Academic Manager.
        /// Payment processing can proceed. Included in financial reports.
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Final state: Claim was rejected at any stage (Coordinator or Manager).
        /// Rejection reasons should be documented in the Notes field.
        /// </summary>
        Rejected = 3
    }
}