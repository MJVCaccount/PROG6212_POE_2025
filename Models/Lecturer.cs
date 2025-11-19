using System.ComponentModel.DataAnnotations;

namespace Contract_Monthly_Claim_System.Models
{
    public class Lecturer
    {
        [Key]
        public string LecturerId { get; set; } // e.g., "IIE2024001"

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(0, 1000)]
        // AUTOMATION: This rate is stored centrally to prevent manual entry errors
        public decimal HourlyRate { get; set; }

        public string Department { get; set; }
    }
}