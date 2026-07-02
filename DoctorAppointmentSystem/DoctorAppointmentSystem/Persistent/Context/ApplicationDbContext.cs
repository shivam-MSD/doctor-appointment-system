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
				entity.HasOne<User>()
					.WithMany()
					.HasForeignKey(a => a.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// 4. Patient Entity Configuration
			modelBuilder.Entity<Patient>(entity =>
			{
				entity.ToTable("Patients");
				entity.HasKey(p => p.PatientId);
				entity.Property(p => p.BloodGroup).HasConversion<string>().HasMaxLength(20);
				entity.Property(p => p.EmergencyConactName).IsRequired().HasMaxLength(100);
				entity.Property(p => p.EmergencyConactNumber).IsRequired().HasMaxLength(20);

				// User to Patient (1 to 1)
				entity.HasOne<User>()
					.WithOne()
					.HasForeignKey<Patient>(p => p.UserId)
					.OnDelete(DeleteBehavior.Cascade);
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
				entity.Property(d => d.Qualification).IsRequired().HasMaxLength(150);
				entity.Property(d => d.LicenceNumber).IsRequired().HasMaxLength(50);
				entity.Property(d => d.HospitalName).IsRequired().HasMaxLength(150);
				entity.Property(d => d.VerificationStatus).HasConversion<string>().HasDefaultValue(EVerificationStatus.Pending);

				// User to Doctor (1 to 1)
				entity.HasOne<User>()
					.WithOne()
					.HasForeignKey<Doctor>(d => d.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				// Doctor to Specialization (Many to 1)
				entity.HasOne<Specialization>()
					.WithMany()
					.HasForeignKey(d => d.SpecializationId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// 7. DoctorSchedule Entity Configuration
			modelBuilder.Entity<DoctorSchedule>(entity =>
			{
				entity.ToTable("DoctorSchedules");
				entity.HasKey(ds => ds.ScheduleId);

				// Doctor to Schedule (1 to Many)
				entity.HasOne<Doctor>()
					.WithMany()
					.HasForeignKey(ds => ds.DoctorId)
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
				entity.HasOne<Doctor>()
					.WithMany()
					.HasForeignKey(dd => dd.DoctorId)
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
				entity.HasOne<Patient>()
					.WithMany()
					.HasForeignKey(app => app.PatientId)
					.OnDelete(DeleteBehavior.Restrict);

				// Appointment to Doctor (Many to 1)
				entity.HasOne<Doctor>()
					.WithMany()
					.HasForeignKey(app => app.DoctorId)
					.OnDelete(DeleteBehavior.Restrict);
			});
		}
	}
}
