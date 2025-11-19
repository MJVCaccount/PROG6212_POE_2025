using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// SERVICE CONFIGURATION - Register all services before building the app
// ====================================================================

// Add MVC Controllers with Views support
builder.Services.AddControllersWithViews();

// ====================================================================
// DATABASE CONFIGURATION
// Configure Entity Framework Core with SQL Server
// Connection string is stored in appsettings.json for security
// EnableRetryOnFailure: Automatically retries failed connections
// ====================================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    )
);

// ====================================================================
// SESSION CONFIGURATION (Required for Authentication)
// Sessions store user authentication state without ASP.NET Identity
// IdleTimeout: Session expires after 30 minutes of inactivity
// HttpOnly: Prevents JavaScript access to session cookie (security)
// IsEssential: Session cookie works even if user rejects tracking
// SecurePolicy: Use HTTPS for session cookies in production
// SameSite.Strict: Prevents CSRF attacks
// ====================================================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Changed to Always for production
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = ".CMCS.Session"; // Custom session cookie name
});

// HttpContextAccessor allows access to session in services
builder.Services.AddHttpContextAccessor();

// ====================================================================
// CUSTOM SERVICE REGISTRATION
// These services implement the automation features required in Part 3
// Scoped: New instance per HTTP request (recommended for DB operations)
// ====================================================================

// EncryptionService: Handles document encryption/decryption using AES
builder.Services.AddScoped<EncryptionService>();

// ClaimAutomationService: Core automation features
// - Auto-calculate claim amounts
// - Validate business rules (max hours, duplicate claims)
// - Auto-verify claims against criteria
// - Generate payment reports using LINQ
builder.Services.AddScoped<ClaimAutomationService>();

// ====================================================================
// SECURITY: ANTI-FORGERY TOKEN CONFIGURATION
// Protects against Cross-Site Request Forgery (CSRF) attacks
// All POST forms must include @Html.AntiForgeryToken()
// ====================================================================
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".CMCS.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ====================================================================
// BUILD THE APPLICATION
// After this point, NO MORE SERVICES can be registered
// ====================================================================
var app = builder.Build();

// ====================================================================
// HTTP REQUEST PIPELINE CONFIGURATION
// Define how the application handles incoming requests
// ====================================================================

// Development Environment: Show detailed error pages
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Production Environment: Show user-friendly error page
    app.UseExceptionHandler("/Home/Error");

    // HSTS: Force browsers to use HTTPS for 1 year
    // Prevents downgrade attacks and cookie hijacking
    app.UseHsts();
}

// Redirect HTTP requests to HTTPS (security requirement)
app.UseHttpsRedirection();

// Serve static files (CSS, JS, images) from wwwroot folder
app.UseStaticFiles();

// Enable routing to map URLs to controllers/actions
app.UseRouting();

// Enable session middleware (MUST be before UseAuthorization)
// This allows controllers to access HttpContext.Session
app.UseSession();

// Enable authorization checks (for future role-based access)
app.UseAuthorization();

// ====================================================================
// ROUTING CONFIGURATION
// Default pattern: /{controller}/{action}/{id?}
// Example: /Claim/Submit/abc123
// If no controller specified, defaults to Home/Index
// ====================================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ====================================================================
// DATABASE INITIALIZATION AND SEEDING
// Creates database and seeds initial data if database is empty
// Only runs on application startup
// ====================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Ensure database is created (creates tables if they don't exist)
        context.Database.EnsureCreated();

        // ====================================================================
        // SEED INITIAL DATA (Only if database is empty)
        // Creates default users for testing and development
        // In production, HR would create these through the UI
        // ====================================================================
        if (!context.Lecturers.Any())
        {
            // --- HR SUPERUSER ---
            // Password: Admin@123 (hashed with BCrypt)
            // Role: HR (full system access)
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

            // --- TEST LECTURER ---
            // Password: Lecturer@123
            // Role: Lecturer (can submit claims)
            // HourlyRate: R350 (will be auto-pulled when submitting claims)
            var lecturer1 = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "IIE2024001",
                Name = "John Doe",
                Email = "john.doe@iie.ac.za",
                HourlyRate = 350.00m, // AUTOMATION SOURCE: Auto-pulled in claims
                Department = "Computer Science",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lecturer@123"),
                Role = "Lecturer",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(lecturer1);

            // --- ADDITIONAL TEST LECTURER ---
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

            // --- PROGRAMME COORDINATOR ---
            // Password: Coord@123
            // Role: Coordinator (can verify claims)
            var coordinator = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "COORD2025001",
                Name = "Sarah Johnson",
                Email = "coord@iie.ac.za",
                HourlyRate = 0,
                Department = "Academic Administration",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Coord@123"),
                Role = "Coordinator",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(coordinator);

            // --- ACADEMIC MANAGER ---
            // Password: Manager@123
            // Role: Manager (final approval authority)
            var manager = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "MGR2025001",
                Name = "Michael Williams",
                Email = "manager@iie.ac.za",
                HourlyRate = 0,
                Department = "Academic Management",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                Role = "Manager",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(manager);

            // Save all seeded data to database
            context.SaveChanges();

            Console.WriteLine("✅ Database seeded successfully with test users!");
            Console.WriteLine("📊 Seeded Users:");
            Console.WriteLine("   - HR Admin: HR2025001 / Admin@123");
            Console.WriteLine("   - Lecturer: IIE2024001 / Lecturer@123");
            Console.WriteLine("   - Coordinator: COORD2025001 / Coord@123");
            Console.WriteLine("   - Manager: MGR2025001 / Manager@123");
        }
        else
        {
            Console.WriteLine("ℹ️ Database already contains data. Skipping seed.");
        }
    }
    catch (Exception ex)
    {
        // Log database initialization errors
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while initializing the database.");

        // In production, you might want to:
        // 1. Send alert to operations team
        // 2. Write to error logging service (e.g., Application Insights)
        // 3. Prevent application from starting if critical
        throw; // Re-throw to prevent app from running with broken database
    }
}

// ====================================================================
// START THE APPLICATION
// Begin listening for HTTP requests
// ====================================================================
Console.WriteLine("🚀 Contract Monthly Claim System is running...");
Console.WriteLine($"🌐 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔒 HTTPS Enforced: {!app.Environment.IsDevelopment()}");

app.Run();