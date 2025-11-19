using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// Session configuration (required for authentication)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHttpContextAccessor();

// Register custom services
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<ClaimAutomationService>(); // <--- CORRECT LOCATION (Keep this one)

// Anti-forgery configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// --- BUILD THE APP ---
var app = builder.Build();
// After this line, you CANNOT use builder.Services anymore!

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// Database seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        // Seed only if database is empty
        if (!context.Lecturers.Any())
        {
            // ... (Keep your seeding logic exactly as is) ...
            var hrUser = new Contract_Monthly_Claim_System.Models.Lecturer
            {
                LecturerId = "HR2025001",
                Name = "HR Administrator",
                Email = "hr@iie.ac.za",
                HourlyRate = 0,
                Department = "Human Resources",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "HR",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Lecturers.Add(hrUser);

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

            // ... (Add other users) ...

            context.SaveChanges();
            Console.WriteLine("✅ Database seeded successfully!");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while seeding the database.");
    }
}

app.Run();