using EMS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EMS.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Auth & Core Tables
    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Designation> Designations { get; set; }
    // --- YEH NAYI LINE ADD KAREIN ---
    public DbSet<SuperAdmin> SuperAdmins { get; set; }

    // Feature Tables
    public DbSet<Attendance> Attendance { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Notification> Notifications { get; set; } 
    public DbSet<Models.Task> Tasks { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<HelpdeskTicket> HelpdeskTickets { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<OnboardingChecklist> OnboardingChecklists { get; set; }
    public DbSet<SalaryStructure> SalaryStructures { get; set; }
    public DbSet<PerformanceReview> PerformanceReviews { get; set; }
    public DbSet<TicketReply> TicketReplies { get; set; }

    // --- NEW: Register the new model for Company Settings ---
    public DbSet<CompanySetting> CompanySettings { get; set; }
}