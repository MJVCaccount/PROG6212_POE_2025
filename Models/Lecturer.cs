using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contract_Monthly_Claim_System.Models
{
    /// <summary>
    /// Represents a lecturer (independent contractor) in the system.
    /// This is the master record managed by HR for rate automation.
    /// All claims reference this table to pull the official contracted hourly rate.
    /// </summary>
    public class Lecturer
    {
        /// <summary>
        /// Unique lecturer identifier (Primary Key).
        /// Format: IIE + Year + Sequential Number (e.g., IIE2024001).
        /// Used as the login username and for tracking all lecturer activities.
        /// </summary>
        [Key]
        [Required(ErrorMessage = "Lecturer ID is required")]
        [MaxLength(20)]
        [Display(Name = "Lecturer ID")]
        public string LecturerId { get; set; }

        /// <summary>
        /// Full name of the lecturer.
        /// Used for display purposes in claims, reports, and correspondence.
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        /// <summary>
        /// Official email address for communication and notifications.
        /// Used for password reset, claim status updates, and official correspondence.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Contracted hourly rate set by HR (in South African Rand).
        /// AUTOMATION KEY FIELD: This rate is automatically pulled when a lecturer submits a claim.
        /// Only HR personnel can modify this value to ensure contract compliance.
        /// Range: R200 - R500 per hour (typical IIE contract rates).
        /// </summary>
        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(200, 500, ErrorMessage = "Hourly rate must be between R200 and R500")]
        [Display(Name = "Hourly Rate (R)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        /// <summary>
        /// Academic department or faculty (e.g., Computer Science, Engineering).
        /// Used for reporting and organizational grouping of claims.
        /// </summary>
        [MaxLength(100)]
        public string Department { get; set; }

        /// <summary>
        /// Hashed password for lecturer authentication.
        /// Passwords are never stored in plain text (hashed using BCrypt or similar).
        /// Used for session-based authentication (not ASP.NET Identity).
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Role designation for authorization purposes.
        /// Possible values: "Lecturer", "Coordinator", "Manager", "HR"
        /// Used to control access to different views and functionalities.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Lecturer";

        /// <summary>
        /// Indicates whether the lecturer account is active.
        /// Inactive accounts cannot submit claims or log in.
        /// HR uses this to deactivate former contractors without deleting historical data.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date when the lecturer record was created.
        /// Used for audit purposes and contract anniversary reporting.
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Date of last update to the lecturer record.
        /// Automatically updated when HR modifies rates or other details.
        /// </summary>
        public DateTime? LastUpdated { get; set; }
    }
}