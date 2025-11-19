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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Changed from Always for development
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHttpContextAccessor();

// Register custom services
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<ClaimAutomationService>();

// Anti-forgery configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

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
app.UseSession(); // MUST be before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

builder.Services.AddScoped<ClaimAutomationService>();
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

            context.SaveChanges();

            Console.WriteLine("✅ Database seeded successfully!");
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

app.Run();