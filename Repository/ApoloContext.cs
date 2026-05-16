using Models;
using Microsoft.EntityFrameworkCore;

namespace Repository
{
    public class ApoloArchiveContext : DbContext
    {
        public ApoloArchiveContext(DbContextOptions<ApoloArchiveContext> options) : base(options) { }

        public DbSet<Payer> Payers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<BillingDocument> BillingDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Payer
            modelBuilder.Entity<Payer>()
                .HasMany(p => p.Students)
                .WithOne(s => s.Payer)
                .HasForeignKey(s => s.PayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Student)
                .WithMany(s => s.Lessons)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice 
            modelBuilder.Entity<BillingDocument>(doc =>
            {
                doc.HasKey(d => d.Id);
                doc.HasIndex(d => new { d.Type, d.Year, d.SequenceNumber }).IsUnique();
            });

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.BillingDocument)
                .WithMany(i => i.Lines)
                .HasForeignKey(a => a.BillingDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
    public class ApoloContext : DbContext
    {
        public ApoloContext(DbContextOptions<ApoloContext> options) : base(options) { }

        public DbSet<Payer> Payers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Specification> Specifications { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<BillingDocument> BillingDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Payer
            modelBuilder.Entity<Payer>()
                .HasMany(p => p.Students)
                .WithOne(s => s.Payer)
                .HasForeignKey(s => s.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student
            modelBuilder.Entity<Student>()
                .HasIndex(s => new { s.PayerId, s.LastName, s.FirstName });

            // Service 
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<Service>()
                .Property(s => s.Name)
                .UseCollation("NOCASE"); // SQLite will now ignore case automatically

            // Specifications (N:1 to Customer, N:1 to Service)
            modelBuilder.Entity<Specification>()
                .HasOne(sp => sp.Student)
                .WithMany(s => s.Specifications)
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Specification>()
                .HasOne(sp => sp.Service)
                .WithMany(s => s.Specifications)
                .HasForeignKey(sp => sp.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lesson (join: Lesson x Customer, unique per pair)
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Student)
                .WithMany(s => s.Lessons)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice 
            modelBuilder.Entity<BillingDocument>(doc =>
            {
                doc.HasKey(d => d.Id);
                doc.HasIndex(d => new { d.Type, d.Year, d.SequenceNumber }).IsUnique();
            });

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.BillingDocument)
                .WithMany(i => i.Lines)
                .HasForeignKey(a => a.BillingDocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
