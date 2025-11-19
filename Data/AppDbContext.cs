using Microsoft.EntityFrameworkCore;
using Contract_Monthly_Claim_System.Models;

namespace Contract_Monthly_Claim_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Claim> Claims { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
    }
}