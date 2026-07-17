using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Domain.Entities;

namespace DoctorAppointmentSystem.Persistent.Context
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<User> Users { get; set; }
		public DbSet<Roles> Roles { get; set; }
		public DbSet<Address> Addresses { get; set; }
		public DbSet<Doctor> Doctors { get; set; }
		public DbSet<Patient> Patients { get; set; }
		public DbSet<Specialization> Specializations { get; set; }
		public DbSet<Appointment> Appointments { get; set; }
		public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
		public DbSet<DoctorDocument> DoctorDocuments { get; set; }
		public DbSet<UserPatient> UserPatients { get; set; }
		public DbSet<Clinic> Clinics { get; set; }
		public DbSet<Admin> Admins { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<ClinicAuditLog> ClinicAuditLogs { get; set; }
		public DbSet<AppointmentAuditLog> AppointmentAuditLogs { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// 1. Roles Entity Configuration
			modelBuilder.Entity<Roles>(entity =>
			{
				entity.ToTable("Roles");
				entity.HasKey(r => r.RoleId);
				entity.Property(r => r.Role)
					.HasConversion<string>() // Store enum as string
					.IsRequired();
			});

			// 2. User Entity Configuration
			modelBuilder.Entity<User>(entity =>
			{
				entity.ToTable("Users");
				entity.HasKey(u => u.UserId);
				entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
				entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
				entity.Property(u => u.IsActive).HasDefaultValue(true);

				// Shadow property RoleId (from ER Diagram) to map Users to Roles (1 Role has Many Users)
				entity.Property<Guid>("RoleId");
				entity.HasOne<Roles>()
					.WithMany()
					.HasForeignKey("RoleId")
					.OnDelete(DeleteBehavior.Restrict);
			});

			// 3. Address Entity Configuration
			modelBuilder.Entity<Address>(entity =>
			{
				entity.ToTable("Addresses");
				entity.HasKey(a => a.AddressId);
				entity.Property(a => a.Country).IsRequired().HasMaxLength(100);
				entity.Property(a => a.State).IsRequired().HasMaxLength(100);
				entity.Property(a => a.City).IsRequired().HasMaxLength(100);
				entity.Property(a => a.Area).IsRequired().HasMaxLength(150);
				entity.Property(a => a.Addressline1).IsRequired().HasMaxLength(250);
				entity.Property(a => a.Addressline2).HasMaxLength(250);

				// User and Address relationship (1 User has Many Addresses)
				entity.HasOne(a => a.User)
					.WithMany()
					.HasForeignKey("UserId") // Shadow property FK
					.OnDelete(DeleteBehavior.Cascade);
			});

			// 4. Patient Entity Configuration
			modelBuilder.Entity<Patient>(entity =>
			{
				entity.ToTable("Patients");
				entity.HasKey(p => p.PatientId);
				entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
				entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
				entity.Property(p => p.MobileNo).IsRequired().HasMaxLength(20);
				entity.Property(p => p.Gender).HasConversion<string>().IsRequired();
				entity.Property(p => p.BloodGroup).HasConversion<string>().HasMaxLength(20);
				entity.Property(p => p.EmergencyConactName).HasMaxLength(100).IsRequired(false);
				entity.Property(p => p.EmergencyConactNumber).HasMaxLength(20).IsRequired(false);
			});

			// 5. Specialization Entity Configuration
			modelBuilder.Entity<Specialization>(entity =>
			{
				entity.ToTable("Specializations");
				entity.HasKey(s => s.SpecializationId);
				entity.Property(s => s.SpecializationName).IsRequired().HasMaxLength(100);
			});

			// 6. Doctor Entity Configuration
			modelBuilder.Entity<Doctor>(entity =>
			{
				entity.ToTable("Doctors");
				entity.HasKey(d => d.DoctorId);
				entity.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
				entity.Property(d => d.LastName).IsRequired().HasMaxLength(100);
				entity.Property(d => d.MobileNo).IsRequired().HasMaxLength(20);
				entity.Property(d => d.Gender).HasConversion<string>().IsRequired();
				entity.Property(d => d.Qualification).IsRequired().HasMaxLength(150);
				entity.Property(d => d.LicenceNumber).IsRequired().HasMaxLength(50);
				entity.Property(d => d.VerificationStatus).HasConversion<string>().HasDefaultValue(EVerificationStatus.Pending);

				// User to Doctor (1 to 1)
				entity.HasOne(d => d.User)
					.WithOne()
					.HasForeignKey<Doctor>("UserId") // Shadow property FK
					.OnDelete(DeleteBehavior.Cascade);

				// Doctor to Specialization (Many to 1)
				entity.HasOne(d => d.Specialization)
					.WithMany()
					.HasForeignKey("SpecializationId") // Shadow property FK
					.OnDelete(DeleteBehavior.Restrict);
			});

			// 7. DoctorSchedule Entity Configuration
			modelBuilder.Entity<DoctorSchedule>(entity =>
			{
				entity.ToTable("DoctorSchedules");
				entity.HasKey(ds => ds.ScheduleId);

				// Doctor to Schedule (1 to Many)
				entity.HasOne(ds => ds.Doctor)
					.WithMany()
					.HasForeignKey("DoctorId") // Shadow property FK
					.OnDelete(DeleteBehavior.Cascade);
			});

			// 8. DoctorDocument Entity Configuration
			modelBuilder.Entity<DoctorDocument>(entity =>
			{
				entity.ToTable("DoctorDocuments");
				entity.HasKey(dd => dd.DocumentId);
				entity.Property(dd => dd.DocumentType).IsRequired().HasMaxLength(50);
				entity.Property(dd => dd.Status).IsRequired().HasMaxLength(50);
				entity.Property(dd => dd.Path).IsRequired().HasMaxLength(500);

				// Doctor to Document (1 to Many)
				entity.HasOne(dd => dd.Doctor)
					.WithMany()
					.HasForeignKey("DoctorId") // Shadow property FK
					.OnDelete(DeleteBehavior.Cascade);
			});

			// 9. Appointment Entity Configuration
			modelBuilder.Entity<Appointment>(entity =>
			{
				entity.ToTable("Appointments");
				entity.HasKey(app => app.AppointmentId);
				entity.Property(app => app.EAppointmentStatus).HasConversion<string>().HasDefaultValue(EAppointmentStatus.Pending);
				entity.Property(app => app.EConsultationType).HasConversion<string>().IsRequired();
				entity.Property(app => app.Reason).IsRequired().HasMaxLength(500);

				// Appointment to Patient (Many to 1)
				entity.HasOne(app => app.Patient)
					.WithMany()
					.HasForeignKey("PatientId") // Shadow property FK
					.OnDelete(DeleteBehavior.Restrict);

				// Appointment to Doctor (Many to 1)
				entity.HasOne(app => app.Doctor)
					.WithMany()
					.HasForeignKey("DoctorId") // Shadow property FK
					.OnDelete(DeleteBehavior.Restrict);

				// Appointment to Clinic (Many to 1)
				entity.HasOne(app => app.Clinic)
					.WithMany()
					.HasForeignKey("ClinicId") // Shadow property FK
					.OnDelete(DeleteBehavior.SetNull);
			});

			// 10. UserPatient (Join Table) Configuration
			modelBuilder.Entity<UserPatient>(entity =>
			{
				entity.ToTable("UserPatients");
				// Composite primary key
				entity.HasKey(up => new { up.UserId, up.PatientId });

				entity.Property(up => up.RelationshipType)
					.HasConversion<string>()
					.IsRequired();

				entity.HasOne(up => up.User)
					.WithMany()
					.HasForeignKey(up => up.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(up => up.Patient)
					.WithMany()
					.HasForeignKey(up => up.PatientId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// 11. Clinic Entity Configuration
			modelBuilder.Entity<Clinic>(entity =>
			{
				entity.ToTable("Clinics");
				entity.HasKey(c => c.ClinicId);
				entity.Property(c => c.ClinicName).IsRequired().HasMaxLength(150);
				entity.Property(c => c.ClinicType).IsRequired().HasMaxLength(50);
				entity.Property(c => c.VerificationStatus)
					.HasConversion<string>()
					.HasDefaultValue(EVerificationStatus.Pending);
				entity.Property(c => c.RejectionReason).HasMaxLength(500);

				// Doctor to Clinic (1 to Many)
				entity.HasOne(c => c.Doctor)
					.WithMany()
					.HasForeignKey("DoctorId")
					.OnDelete(DeleteBehavior.Restrict);

				// Address to Clinic (1 to 1)
				entity.HasOne(c => c.Address)
					.WithMany()
					.HasForeignKey("AddressId")
					.OnDelete(DeleteBehavior.Cascade);
			});

			// 12. Admin Entity Configuration
			modelBuilder.Entity<Admin>(entity =>
			{
				entity.ToTable("Admins");
				entity.HasKey(a => a.AdminId);
				entity.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
				entity.Property(a => a.LastName).IsRequired().HasMaxLength(100);
				entity.Property(a => a.MobileNo).IsRequired().HasMaxLength(20);
				entity.Property(a => a.IsVerified).HasDefaultValue(false);

				// User to Admin (1 to 1)
				entity.HasOne(a => a.User)
					.WithOne()
					.HasForeignKey<Admin>("UserId")
					.OnDelete(DeleteBehavior.Cascade);

				// Clinic to Admin (Many to 1)
				entity.HasOne(a => a.Clinic)
					.WithMany()
					.HasForeignKey("ClinicId")
					.OnDelete(DeleteBehavior.Restrict);
			});
		}
	}
}
