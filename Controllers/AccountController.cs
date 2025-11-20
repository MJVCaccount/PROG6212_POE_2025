using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Contract_Monthly_Claim_System.Controllers
{
    /// <summary>
    /// Account Controller - Handles user authentication using session-based approach.
    /// Uses HttpContext.Session instead of ASP.NET Identity (as per POE requirements).
    /// Implements secure login, logout, and role-based access control.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor with dependency injection of database context.
        /// </summary>
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        #region Login Actions

        /// <summary>
        /// GET: Display the login page.
        /// Redirects to home if user is already authenticated (session exists).
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect to home
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        /// <summary>
        /// POST: Authenticate user credentials and create session.
        /// SECURITY FEATURES:
        /// 1. Password hashing comparison (BCrypt)
        /// 2. Account active status check
        /// 3. Session-based authentication (not cookies for auth)
        /// 4. Role-based redirection
        /// </summary>
        /// <param name="lecturerId">User's login ID</param>
        /// <param name="password">Plain-text password (hashed for comparison)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string lecturerId, string password)
        {
            // Input validation
            if (string.IsNullOrEmpty(lecturerId) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Lecturer ID and password are required.");
                return View();
            }

            // Retrieve user from database
            var user = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.LecturerId == lecturerId);

            // Check if user exists
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid Lecturer ID or password.");
                return View();
            }

            // Check if account is active
            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Contact HR.");
                return View();
            }

            // Verify password using BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                ModelState.AddModelError("", "Invalid Lecturer ID or password.");
                return View();
            }

            // --- SUCCESSFUL AUTHENTICATION ---

            // Create session variables for authenticated user
            HttpContext.Session.SetString("UserId", user.LecturerId);
            HttpContext.Session.SetString("LecturerId", user.LecturerId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);

            // Set session timeout to 30 minutes of inactivity
            HttpContext.Session.SetInt32("LoginTime", (int)DateTime.Now.Ticks);

            // Log successful login for audit purposes (optional)
            // In production, consider logging to a separate audit table

            // Role-based redirection
            switch (user.Role)
            {
                case "HR":
                    TempData["Success"] = $"Welcome, {user.Name}! (HR Dashboard)";
                    return RedirectToAction("Dashboard", "HR");

                case "Manager":
                    TempData["Success"] = $"Welcome, {user.Name}! (Manager Dashboard)";
                    return RedirectToAction("ManagerApprove", "Claim");

                case "Coordinator":
                    TempData["Success"] = $"Welcome, {user.Name}! (Coordinator Dashboard)";
                    return RedirectToAction("CoordinatorApprove", "Claim");

                case "Lecturer":
                default:
                    TempData["Success"] = $"Welcome, {user.Name}!";
                    return RedirectToAction("Index", "Home");
            }
        }

        #endregion

        #region Logout Actions

        /// <summary>
        /// GET: Log out the current user. 
        /// Marked as HttpGet so the Navbar link works without a form.
        /// </summary>
        [HttpGet]
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            TempData["Success"] = "You have been logged out successfully.";

            // Redirect to Login page (Safer than returning a View that might not exist)
            return RedirectToAction("Login");
        }

        #endregion

        #region Password Management

        /// <summary>
        /// GET: Display the change password form.
        /// Users must be authenticated to access this page.
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Check if user is authenticated
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Session expired. Please log in.";
                return RedirectToAction("Login");
            }

            return View();
        }

        /// <summary>
        /// POST: Process password change request.
        /// SECURITY: Requires current password verification before allowing change.
        /// New password is hashed using BCrypt before storage.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword,
                                                        string newPassword,
                                                        string confirmPassword)
        {
            // Check authentication
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Session expired. Please log in.";
                return RedirectToAction("Login");
            }

            // Input validation
            if (string.IsNullOrEmpty(currentPassword) ||
                string.IsNullOrEmpty(newPassword) ||
                string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            // Validate new password confirmation
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            // Password strength validation (minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 number)
            if (newPassword.Length < 8 ||
                !newPassword.Any(char.IsUpper) ||
                !newPassword.Any(char.IsLower) ||
                !newPassword.Any(char.IsDigit))
            {
                ModelState.AddModelError("",
                    "Password must be at least 8 characters with uppercase, lowercase, and number.");
                return View();
            }

            // Retrieve user from database
            var user = await _context.Lecturers.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Verify current password
            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }

            // Hash new password and update database
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Access Denied

        /// <summary>
        /// GET: Display access denied page when user tries to access unauthorized resources.
        /// </summary>
        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Session Helper Methods (for use in other controllers)

        /// <summary>
        /// Helper method to check if user is authenticated.
        /// Can be called from other controllers to verify session.
        /// </summary>
        /// <returns>True if valid session exists, false otherwise</returns>
        public static bool IsAuthenticated(HttpContext context)
        {
            return !string.IsNullOrEmpty(context.Session.GetString("UserId"));
        }

        /// <summary>
        /// Helper method to check if user has a specific role.
        /// </summary>
        /// <param name="context">Current HTTP context</param>
        /// <param name="requiredRole">Role to check (e.g., "HR", "Manager")</param>
        /// <returns>True if user has the required role, false otherwise</returns>
        public static bool HasRole(HttpContext context, string requiredRole)
        {
            var userRole = context.Session.GetString("UserRole");
            return userRole == requiredRole;
        }

        /// <summary>
        /// Helper method to check if user has any of the specified roles.
        /// </summary>
        /// <param name="context">Current HTTP context</param>
        /// <param name="allowedRoles">Array of allowed roles</param>
        /// <returns>True if user has any of the allowed roles, false otherwise</returns>
        public static bool HasAnyRole(HttpContext context, params string[] allowedRoles)
        {
            var userRole = context.Session.GetString("UserRole");
            return allowedRoles.Contains(userRole);
        }

        #endregion
    }
}