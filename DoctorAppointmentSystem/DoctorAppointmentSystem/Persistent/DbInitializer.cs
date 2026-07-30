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
				var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "superadmin@doctorapp.com");
				if (adminUser == null)
				{
					using var sha256 = SHA256.Create();
					var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("SuperAdmin@123"));
					var passwordHash = Convert.ToBase64String(hashedBytes);

					var superAdminUser = new User
					{
						UserId = Guid.NewGuid(),
						Email = "superadmin@doctorapp.com",
						IsActive = true,
						IsEmailVerified = true,
						CreatedDate = DateTime.UtcNow,
						LastLoginDate = DateTime.UtcNow
					};

					db.Users.Add(superAdminUser);
					db.Entry(superAdminUser).Property("RoleId").CurrentValue = superAdminRole.RoleId;

					var userPassword = new UserPassword
					{
						UserId = superAdminUser.UserId,
						User = superAdminUser,
						PasswordHash = passwordHash
					};
					db.UserPasswords.Add(userPassword);
					await db.SaveChangesAsync();
				}
				else
				{
					var passwordExists = await db.UserPasswords.AnyAsync(up => up.UserId == adminUser.UserId);
					if (!passwordExists)
					{
						using var sha256 = SHA256.Create();
						var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("SuperAdmin@123"));
						var passwordHash = Convert.ToBase64String(hashedBytes);

						var userPassword = new UserPassword
						{
							UserId = adminUser.UserId,
							User = adminUser,
							PasswordHash = passwordHash
						};
						db.UserPasswords.Add(userPassword);
						await db.SaveChangesAsync();
					}
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
			if (db.Database.IsSqlServer())
			{
				await db.Database.ExecuteSqlRawAsync("UPDATE Clinics SET IsAvailable = 1 WHERE IsAvailable = 0 AND UnavailabilityReason IS NULL");
			}
			else if (db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
			{
				await db.Database.ExecuteSqlRawAsync("UPDATE doctorappointment.\"Clinics\" SET \"IsAvailable\" = TRUE WHERE \"IsAvailable\" = FALSE AND \"UnavailabilityReason\" IS NULL");
			}

			// // Copy existing legacy user password hashes into UserPasswords table if they don't already exist
			// await db.Database.ExecuteSqlRawAsync(@"
			// 	INSERT INTO UserPasswords (UserId, PasswordHash)
			// 	SELECT u.UserId, u.PasswordHash
			// 	FROM Users u
			// 	WHERE u.PasswordHash IS NOT NULL 
			// 	  AND u.PasswordHash <> ''
			// 	  AND NOT EXISTS (
			// 		  SELECT 1 FROM UserPasswords up WHERE up.UserId = u.UserId
			// 	  )
			// ");
		}
	}
}
