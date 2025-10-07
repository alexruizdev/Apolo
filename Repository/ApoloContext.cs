using Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Repository
{
    public class ApoloContext : DbContext
    {
        public ApoloContext(DbContextOptions<ApoloContext> options) : base(options) { }

        public DbSet<Payer> Payers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Specification> Specifications { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("DataSource=app.db");
        }

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

            // ServiceType
            modelBuilder.Entity<ServiceType>()
                .HasIndex(st => st.Name)
                .IsUnique();

            // Service 
            modelBuilder.Entity<Service>()
                .HasOne(s => s.ServiceType)
                .WithMany(st => st.Services)
                .HasForeignKey(s => s.ServiceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

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
                .OnDelete(DeleteBehavior.Restrict);

            // Lesson (N:1 to Service, optional 1:0..N to Specification)
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Service)
                .WithMany(s => s.Lessons)
                .HasForeignKey(l => l.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Specification)
                .WithMany()
                .HasForeignKey(l => l.SpecificationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Attendance (join: Lesson x Customer, unique per pair)
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Lesson)
                .WithMany(l => l.Attendaces)
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.LessonId, a.StudentId })
                .IsUnique();

            // Constraints
            //modelBuilder.Entity<Payer>()
            //    .ToTable(t => t.HasCheckConstraint("CK_Payer_Balance", "Balance => 0"));
            //modelBuilder.Entity<Student>()
            //    .ToTable(t => t.HasCheckConstraint("CK_Student_Commute", "CommuteMinutes IS NULL OR CommuteMinutes >= 0"));
            //modelBuilder.Entity<Service>()
            //    .ToTable(t => t.HasCheckConstraint("CK_Service_Price", "PricePerHour >= 0"));
            //modelBuilder.Entity<Lesson>()
            //    .ToTable(t => t.HasCheckConstraint("CK_Lesson_Duration", "DurationMinutes > 0"));
            //modelBuilder.Entity<Attendance>()
            //    .ToTable(t => t.HasCheckConstraint("CK_Attendance_Price", "Price >= 0"));
        }

        private void EnforceBusinessRules()
        {
            // Recalculate GrandTotal as sum of Attendance.Price for changed instances
            var changedInstances = ChangeTracker.Entries<Lesson>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            foreach (var inst in changedInstances)
            {
                if (Entry(inst).Collection(i => i.Attendaces).IsLoaded)
                {
                    inst.GrandTotal = inst.Attendaces.Sum(a => a.Price);
                }
            }

            var newOrModInstances = ChangeTracker.Entries<Lesson>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var e in newOrModInstances)
            {
                if (Entry(e.Entity).Collection(i => i.Attendaces).IsLoaded &&
                    e.Entity.Attendaces.Count == 0)
                {
                    throw new InvalidOperationException("A lesson must have at least one Attendance.");
                }
            }
        }

        public override int SaveChanges()
        {
            EnforceBusinessRules();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnforceBusinessRules();
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
