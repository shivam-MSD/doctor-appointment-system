using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Persistent
{
	public static class DbInitializer
	{
		public static async Task SeedAsync(ApplicationDbContext db)
		{
			// 1. Seed Roles
			var roles = Enum.GetValues<ERole>();
			foreach (var roleEnum in roles)
			{
				var roleExists = await db.Roles.AnyAsync(r => r.Role == roleEnum);
				if (!roleExists)
				{
					db.Roles.Add(new Roles
					{
						RoleId = Guid.NewGuid(),
						Role = roleEnum
					});
				}
			}
			await db.SaveChangesAsync();

			// 2. Seed Super Admin
			var superAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.Role == ERole.SuperAdmin);
			if (superAdminRole != null)
			{
				var adminExists = await db.Users.AnyAsync(u => u.Email == "superadmin@doctorapp.com");
				if (!adminExists)
				{
					using var sha256 = SHA256.Create();
					var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("SuperAdmin@123"));
					var passwordHash = Convert.ToBase64String(hashedBytes);

					var superAdminUser = new User
					{
						UserId = Guid.NewGuid(),
						Email = "superadmin@doctorapp.com",
						PasswordHash = passwordHash,
						IsActive = true,
						CreatedDate = DateTime.UtcNow,
						LastLoginDate = DateTime.UtcNow
					};

					db.Users.Add(superAdminUser);
					db.Entry(superAdminUser).Property("RoleId").CurrentValue = superAdminRole.RoleId;
					await db.SaveChangesAsync();
				}
			}

			// 3. Seed Specializations
			var specializations = new string[]
			{
				"General Physician",
				"Cardiologist",
				"Dermatologist",
				"Pediatrician",
				"Gynecologist & Obstetrician",
				"Orthopedic Surgeon",
				"Neurologist",
				"Psychiatrist",
				"Ophthalmologist",
				"ENT Specialist",
				"Gastroenterologist",
				"Pulmonologist",
				"Nephrologist",
				"Endocrinologist",
				"Oncologist",
				"Urologist",
				"General Surgeon",
				"Plastic Surgeon",
				"Neurosurgeon",
				"Rheumatologist",
				"Allergist & Immunologist",
				"Anesthesiologist",
				"Radiologist",
				"Pathologist",
				"Hematologist",
				"Geriatrician",
				"Sports Medicine Specialist",
				"Physiotherapist",
				"Dentist",
				"Orthodontist",
				"Periodontist",
				"Endodontist",
				"Oral & Maxillofacial Surgeon",
				"Chiropractor",
				"Podiatrist",
				"Audiologist",
				"Speech Therapist",
				"Dietitian & Nutritionist",
				"Neonatologist",
				"Pain Management Specialist",
				"Infectious Disease Specialist",
				"Occupational Therapist",
				"Clinical Psychologist",
				"Homeopathic Physician",
				"Ayurvedic Physician"
			};

			foreach (var specName in specializations)
			{
				var specExists = await db.Specializations.AnyAsync(s => s.SpecializationName == specName);
				if (!specExists)
				{
					db.Specializations.Add(new Specialization
					{
						SpecializationId = Guid.NewGuid(),
						SpecializationName = specName
					});
				}
			}
			await db.SaveChangesAsync();

			// Default legacy clinics to true availability
			await db.Database.ExecuteSqlRawAsync("UPDATE Clinics SET IsAvailable = 1 WHERE IsAvailable = 0 AND UnavailabilityReason IS NULL");
		}
	}
}
