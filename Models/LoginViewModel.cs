using System.ComponentModel.DataAnnotations;

namespace Contract_Monthly_Claim_System.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "User ID is required")]
        [Display(Name = "User ID")]
        public string LecturerId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}