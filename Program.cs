using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// SECTION 1: Service Registration
// ============================================================================

/// <summary>
/// Add MVC controllers with views for traditional web application architecture.
/// Includes Razor view engine and model binding.
/// </summary>
builder.Services.AddControllersWithViews();

/// <summary>
/// DATABASE CONFIGURATION
/// Configure Entity Framework Core with SQL Server connection.
/// Connection string is stored in appsettings.json for security and environment separation.
/// In production, use Azure Key Vault or similar for connection string management.
/// </summary>
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure() // Automatic retry for transient failures
    )
);

/// <summary>
/// SESSION CONFIGURATION (Required for authentication without ASP.NET Identity)
/// Sessions are used to maintain user authentication state across requests.
/// Configuration:
/// - IdleTimeout: 30 minutes of inactivity before session expires
/// - HttpOnly: Cookie not accessible via JavaScript (prevents XSS attacks)
/// - IsEssential: Session cookie is essential for application functionality
/// </summary>
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout after 30 minutes inactivity
    options.Cookie.HttpOnly = true; // Security: Cookie not accessible via JavaScript
    options.Cookie.IsEssential = true; // Required for GDPR compliance (essential functionality)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only in production
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
});

/// <summary>
/// HTTP CONTEXT ACCESSOR
/// Allows dependency injection of IHttpContextAccessor for accessing HTTP context
/// in services (e.g., for session access in automation services).
/// </summary>
builder.Services.AddHttpContextAccessor();

/// <summary>
/// DEPENDENCY INJECTION: Register Custom Services
/// All services are registered as Scoped to align with Entity Framework DbContext lifecycle.
/// Scoped services are created once per HTTP request.
/// </summary>

// Encryption service for secure document storage (GDPR/POPIA compliance)
builder.Services.AddScoped<EncryptionService>();

// Automation service for claim processing, validation, and reporting
builder.Services.AddScoped<ClaimAutomationService>();

/// <summary>
/// ANTI-FORGERY TOKEN CONFIGURATION
/// Protects against Cross-Site Request Forgery (CSRF) attacks.
/// All POST actions require [ValidateAntiForgeryToken] attribute.
/// </summary>
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN"; // Custom header name for AJAX requests
});

/// <summary>
/// RESPONSE COMPRESSION (Optional - improves performance)
/// Compresses HTTP responses to reduce bandwidth usage.
/// </summary>
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS
});

// ============================================================================
// SECTION 2: Middleware Pipeline Configuration
// ============================================================================

var app = builder.Build();

/// <summary>
/// ENVIRONMENT-SPECIFIC CONFIGURATION
/// Different middleware based on development vs. production environment.
/// </summary>
if (app.Environment.IsDevelopment())
{
    // Development: Show detailed error pages with stack traces
    app.UseDeveloperExceptionPage();
}
else
{
    // Production: Use generic error page and enforce HTTPS
    app.UseExceptionHandler("/Home/Error");

    // HSTS (HTTP Strict Transport Security)
    // Enforces HTTPS connections for 1 year (365 days)
    app.UseHsts();
}

/// <summary>
/// HTTPS REDIRECTION
/// Automatically redirects HTTP requests to HTTPS for security.
/// Critical for protecting session cookies and sensitive data.
/// </summary>
app.UseHttpsRedirection();

/// <summary>
/// STATIC FILES MIDDLEWARE
/// Serves static files (CSS, JavaScript, images) from wwwroot folder.
/// </summary>
app.UseStaticFiles();

/// <summary>
/// ROUTING MIDDLEWARE
/// Enables endpoint routing for MVC controllers.
/// </summary>
app.UseRouting();

/// <summary>
/// SESSION MIDDLEWARE (CRITICAL - Must be before UseAuthorization)
/// Enables session state for the application.
/// This MUST be placed after UseRouting and before UseAuthorization.
/// </summary>
app.UseSession();

/// <summary>
/// AUTHORIZATION MIDDLEWARE
/// Handles authorization decisions for protected resources.
/// In this application, authorization is handled via session checks in controllers.
/// </summary>
app.UseAuthorization();

/// <summary>
/// ENDPOINT MAPPING
/// Defines the default route pattern for MVC controllers.
/// Default route: {controller=Home}/{action=Index}/{id?}
/// This means:
/// - / → HomeController.Index()
/// - /Claim/Submit → ClaimController.Submit()
/// - /HR/Dashboard → HRController.Dashboard()
/// </summary>
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ============================================================================
// SECTION 3: Database Initialization (Optional - For Development/Testing)
// ============================================================================

/// <summary>
/// SEED DATA: Create default users and test data for development.
/// In production, this should be handled by database migration scripts.
/// This code runs once at application startup to ensure essential data exists.
/// </summary>
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Ensure database is created (for development only)
        // In production, use proper migrations: dotnet ef database update
        context.Database.EnsureCreated();

        // Seed default HR user if no users exist
        if (!context.Lecturers.Any())
        {
            var hrUser = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "HR2025001",
                Name = "HR Administrator",
                Email = "hr@iie.ac.za",
                HourlyRate = 0, // HR staff don't submit claims
                Department = "Human Resources",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "HR",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(hrUser);

            // Seed sample lecturers for testing
            var lecturer1 = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "IIE2024001",
                Name = "John Doe",
                Email = "john.doe@iie.ac.za",
                HourlyRate = 350.00m,
                Department = "Computer Science",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lecturer@123"),
                Role = "Lecturer",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(lecturer1);

            var lecturer2 = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "IIE2024002",
                Name = "Jane Smith",
                Email = "jane.smith@iie.ac.za",
                HourlyRate = 400.00m,
                Department = "Engineering",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lecturer@123"),
                Role = "Lecturer",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(lecturer2);

            // Seed coordinator and manager accounts
            var coordinator = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "COORD2025001",
                Name = "Programme Coordinator",
                Email = "coordinator@iie.ac.za",
                HourlyRate = 0,
                Department = "Academic Management",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Coord@123"),
                Role = "Coordinator",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(coordinator);

            var manager = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "MGR2025001",
                Name = "Academic Manager",
                Email = "manager@iie.ac.za",
                HourlyRate = 0,
                Department = "Academic Management",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                Role = "Manager",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(manager);

            // Save all seed data
            context.SaveChanges();

            Console.WriteLine("✅ Database seeded successfully with default users:");
            Console.WriteLine("   HR:          HR2025001 / Admin@123");
            Console.WriteLine("   Lecturer 1:  IIE2024001 / Lecturer@123");
            Console.WriteLine("   Lecturer 2:  IIE2024002 / Lecturer@123");
            Console.WriteLine("   Coordinator: COORD2025001 / Coord@123");
            Console.WriteLine("   Manager:     MGR2025001 / Manager@123");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while seeding the database.");
    }
}

// ============================================================================
// SECTION 4: Application Startup
// ============================================================================

/// <summary>
/// Start the application and begin listening for HTTP requests.
/// The application will run until stopped (Ctrl+C in development).
/// </summary>
app.Run();

// ============================================================================
// END OF CONFIGURATION
// ============================================================================