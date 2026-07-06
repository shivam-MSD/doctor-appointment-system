using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class ClinicService : IClinicService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly INotificationService _notificationService;

		public ClinicService(ApplicationDbContext dbContext, INotificationService notificationService)
		{
			_dbContext = dbContext;
			_notificationService = notificationService;
		}

		public async Task RegisterClinicAndAdminAsync(Guid doctorUserId, RegisterClinicDto dto)
		{
			// 1. Resolve Doctor profile from current user session
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.FirstOrDefaultAsync(d => d.User.UserId == doctorUserId);
			if (doctor == null)
			{
				throw new NotFoundException("Doctor profile was not found. Please register as a doctor first.");
			}

			if (doctor.VerificationStatus != EVerificationStatus.Verified)
			{
				throw new ForbiddenException("You cannot register clinics or admins until your doctor profile is verified by a Super Admin.");
			}

			// 2. Validate Admin email uniqueness
			var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == dto.AdminEmail);
			if (emailExists)
			{
				throw new EmailAlreadyExistsException(dto.AdminEmail);
			}

			// 3. Resolve Admin role from database
			var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == ERole.Admin);
			if (adminRole == null)
			{
				adminRole = new Roles { RoleId = Guid.NewGuid(), Role = ERole.Admin };
				_dbContext.Roles.Add(adminRole);
				await _dbContext.SaveChangesAsync();
			}

			// 4. Create User credentials for Clinic Admin
			var adminUser = new User
			{
				UserId = Guid.NewGuid(),
				Email = dto.AdminEmail,
				PasswordHash = HashPassword(dto.AdminPassword),
				IsActive = true,
				CreatedDate = DateTime.UtcNow,
				LastLoginDate = DateTime.UtcNow
			};
			_dbContext.Users.Add(adminUser);
			_dbContext.Entry(adminUser).Property("RoleId").CurrentValue = adminRole.RoleId;

			// 5. Create Address for the Clinic
			var clinicAddress = new Address
			{
				AddressId = Guid.NewGuid(),
				User = adminUser, // Bind clinic address ownership to adminUser
				Country = dto.Country,
				State = dto.State,
				City = dto.City,
				Area = dto.Area,
				Pincode = dto.Pincode,
				Addressline1 = dto.Addressline1,
				Addressline2 = dto.Addressline2
			};
			_dbContext.Addresses.Add(clinicAddress);

			// 6. Create Clinic entry
			var clinic = new Clinic
			{
				ClinicId = Guid.NewGuid(),
				ClinicName = dto.ClinicName,
				ClinicType = dto.ClinicType,
				Doctor = doctor,
				Address = clinicAddress,
				VerificationStatus = EVerificationStatus.Pending,
				CreatedDate = DateTime.UtcNow
			};
			_dbContext.Clinics.Add(clinic);

			// 7. Create Admin profile details
			var adminProfile = new Admin
			{
				AdminId = Guid.NewGuid(),
				User = adminUser,
				Clinic = clinic,
				FirstName = dto.AdminFirstName,
				LastName = dto.AdminLastName,
				MobileNo = dto.AdminMobileNo,
				IsVerified = false,
				CreatedDate = DateTime.UtcNow
			};
			_dbContext.Admins.Add(adminProfile);

			await _dbContext.SaveChangesAsync();
		}

		public async Task RegisterClinicAsync(Guid doctorUserId, CreateClinicDto dto)
		{
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.FirstOrDefaultAsync(d => d.User.UserId == doctorUserId);
			if (doctor == null)
			{
				throw new NotFoundException("Doctor profile was not found. Please register as a doctor first.");
			}

			if (doctor.VerificationStatus != EVerificationStatus.Verified)
			{
				throw new ForbiddenException("You cannot register clinics until your doctor profile is verified by a Super Admin.");
			}

			var clinicAddress = new Address
			{
				AddressId = Guid.NewGuid(),
				User = doctor.User,
				Country = dto.Country,
				State = dto.State,
				City = dto.City,
				Area = dto.Area,
				Pincode = dto.Pincode,
				Addressline1 = dto.Addressline1,
				Addressline2 = dto.Addressline2
			};
			_dbContext.Addresses.Add(clinicAddress);

			var clinic = new Clinic
			{
				ClinicId = Guid.NewGuid(),
				ClinicName = dto.ClinicName,
				ClinicType = dto.ClinicType,
				Doctor = doctor,
				Address = clinicAddress,
				VerificationStatus = EVerificationStatus.Pending,
				CreatedDate = DateTime.UtcNow
			};
			_dbContext.Clinics.Add(clinic);

			await _dbContext.SaveChangesAsync();

			await _notificationService.CreateNotificationForRoleAsync("SuperAdmin", $"Dr. {doctor.FirstName} {doctor.LastName} registered a new clinic branch '{dto.ClinicName}' and requires verification.");
			await _notificationService.SendRefreshSignalAsync("Clinics");
		}

		public async Task RegisterAdminForClinicAsync(Guid doctorUserId, RegisterAdminForClinicDto dto)
		{
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.FirstOrDefaultAsync(d => d.User.UserId == doctorUserId);
			if (doctor == null)
			{
				throw new NotFoundException("Doctor profile was not found.");
			}

			var clinic = await _dbContext.Clinics
				.Include(c => c.Doctor)
				.FirstOrDefaultAsync(c => c.ClinicId == dto.ClinicId);
			if (clinic == null)
			{
				throw new NotFoundException($"Clinic with ID '{dto.ClinicId}' was not found.");
			}

			if (clinic.Doctor.DoctorId != doctor.DoctorId)
			{
				throw new ForbiddenException("You do not own this clinic and cannot assign an admin to it.");
			}

			if (clinic.VerificationStatus != EVerificationStatus.Verified)
			{
				throw new BadRequestException("You can only register admins for verified clinic locations.");
			}

			var existingAdmin = await _dbContext.Admins.AnyAsync(a => a.Clinic.ClinicId == dto.ClinicId);
			if (existingAdmin)
			{
				throw new BadRequestException("This clinic already has an admin registered. You cannot register more than one admin per clinic.");
			}

			var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == dto.AdminEmail);
			if (emailExists)
			{
				throw new EmailAlreadyExistsException(dto.AdminEmail);
			}

			var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == ERole.Admin);
			if (adminRole == null)
			{
				adminRole = new Roles { RoleId = Guid.NewGuid(), Role = ERole.Admin };
				_dbContext.Roles.Add(adminRole);
				await _dbContext.SaveChangesAsync();
			}

			var adminUser = new User
			{
				UserId = Guid.NewGuid(),
				Email = dto.AdminEmail,
				PasswordHash = HashPassword(dto.AdminPassword),
				IsActive = true,
				CreatedDate = DateTime.UtcNow,
				LastLoginDate = DateTime.UtcNow
			};
			_dbContext.Users.Add(adminUser);
			_dbContext.Entry(adminUser).Property("RoleId").CurrentValue = adminRole.RoleId;

			var adminProfile = new Admin
			{
				AdminId = Guid.NewGuid(),
				User = adminUser,
				Clinic = clinic,
				FirstName = dto.AdminFirstName,
				LastName = dto.AdminLastName,
				MobileNo = dto.AdminMobileNo,
				IsVerified = false,
				CreatedDate = DateTime.UtcNow
			};
			_dbContext.Admins.Add(adminProfile);

			await _dbContext.SaveChangesAsync();

			await _notificationService.CreateNotificationForRoleAsync("SuperAdmin", $"Dr. {doctor.FirstName} {doctor.LastName} registered a new clinic admin {dto.AdminFirstName} {dto.AdminLastName} for '{clinic.ClinicName}' requiring verification.");
			await _notificationService.SendRefreshSignalAsync("Admins");
		}

		public async Task<IEnumerable<ClinicDto>> GetDoctorClinicsAsync(Guid doctorUserId)
		{
			return await _dbContext.Clinics
				.Include(c => c.Doctor)
				.Include(c => c.Address)
				.Where(c => c.Doctor.User.UserId == doctorUserId)
				.Select(c => new ClinicDto
				{
					ClinicId = c.ClinicId,
					ClinicName = c.ClinicName,
					ClinicType = c.ClinicType,
					DoctorId = c.Doctor.DoctorId,
					DoctorName = $"Dr. {c.Doctor.FirstName} {c.Doctor.LastName}",
					State = c.Address.State,
					City = c.Address.City,
					Pincode = c.Address.Pincode,
					Area = c.Address.Area,
					Addressline1 = c.Address.Addressline1,
					Addressline2 = c.Address.Addressline2,
					IsVerified = c.VerificationStatus == EVerificationStatus.Verified,
					VerificationStatus = c.VerificationStatus.ToString(),
					RejectionReason = c.RejectionReason,
					HasAdmin = _dbContext.Admins.Any(a => a.Clinic.ClinicId == c.ClinicId)
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicAdminDto>> GetDoctorAdminsAsync(Guid doctorUserId)
		{
			return await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.ThenInclude(c => c.Doctor)
				.Where(a => a.Clinic.Doctor.User.UserId == doctorUserId)
				.Select(a => new ClinicAdminDto
				{
					AdminId = a.AdminId,
					UserId = a.User.UserId,
					ClinicId = a.Clinic.ClinicId,
					ClinicName = a.Clinic.ClinicName,
					FirstName = a.FirstName,
					LastName = a.LastName,
					MobileNo = a.MobileNo,
					IsVerified = a.IsVerified
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicDto>> GetPendingClinicsAsync()
		{
			return await _dbContext.Clinics
				.Include(c => c.Doctor)
				.Include(c => c.Address)
				.Where(c => c.VerificationStatus == EVerificationStatus.Pending || c.VerificationStatus == EVerificationStatus.UpdatedPending)
				.Select(c => new ClinicDto
				{
					ClinicId = c.ClinicId,
					ClinicName = c.ClinicName,
					ClinicType = c.ClinicType,
					DoctorId = c.Doctor.DoctorId,
					DoctorName = $"Dr. {c.Doctor.FirstName} {c.Doctor.LastName}",
					State = c.Address.State,
					City = c.Address.City,
					Pincode = c.Address.Pincode,
					Area = c.Address.Area,
					Addressline1 = c.Address.Addressline1,
					Addressline2 = c.Address.Addressline2,
					IsVerified = c.VerificationStatus == EVerificationStatus.Verified,
					VerificationStatus = c.VerificationStatus.ToString(),
					RejectionReason = c.RejectionReason,
					HasAdmin = _dbContext.Admins.Any(a => a.Clinic.ClinicId == c.ClinicId)
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicAdminDto>> GetPendingAdminsAsync()
		{
			return await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.Where(a => !a.IsVerified)
				.Select(a => new ClinicAdminDto
				{
					AdminId = a.AdminId,
					UserId = a.User.UserId,
					ClinicId = a.Clinic.ClinicId,
					ClinicName = a.Clinic.ClinicName,
					FirstName = a.FirstName,
					LastName = a.LastName,
					MobileNo = a.MobileNo,
					IsVerified = a.IsVerified
				})
				.ToListAsync();
		}

		public async Task VerifyClinicAsync(Guid clinicId)
		{
			var clinic = await _dbContext.Clinics
				.Include(c => c.Doctor)
				.ThenInclude(d => d.User)
				.FirstOrDefaultAsync(c => c.ClinicId == clinicId);

			if (clinic == null)
			{
				throw new NotFoundException($"Clinic with ID '{clinicId}' was not found.");
			}
			clinic.VerificationStatus = EVerificationStatus.Verified;
			clinic.RejectionReason = null;
			await _dbContext.SaveChangesAsync();

			await _notificationService.CreateNotificationAsync(clinic.Doctor.User.UserId, $"Your clinic branch '{clinic.ClinicName}' has been verified and approved by the Super Admin.");
			await _notificationService.SendRefreshSignalAsync("Clinics");
		}

		public async Task VerifyAdminAsync(Guid adminId)
		{
			var admin = await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.ThenInclude(c => c.Doctor)
				.ThenInclude(d => d.User)
				.FirstOrDefaultAsync(a => a.AdminId == adminId);

			if (admin == null)
			{
				throw new NotFoundException($"Clinic Admin with ID '{adminId}' was not found.");
			}
			admin.IsVerified = true;
			await _dbContext.SaveChangesAsync();

			// Notify Admin user they have been approved
			await _notificationService.CreateNotificationAsync(admin.User.UserId, $"Your Clinic Admin account for '{admin.Clinic.ClinicName}' has been approved and activated.");
			// Notify Doctor user that their admin has been approved
			await _notificationService.CreateNotificationAsync(admin.Clinic.Doctor.User.UserId, $"The Clinic Admin {admin.FirstName} {admin.LastName} assigned to '{admin.Clinic.ClinicName}' has been approved.");
			await _notificationService.SendRefreshSignalAsync("Admins");
		}

		public async Task RejectClinicAsync(Guid clinicId, string rejectionReason)
		{
			var clinic = await _dbContext.Clinics
				.Include(c => c.Doctor)
				.ThenInclude(d => d.User)
				.FirstOrDefaultAsync(c => c.ClinicId == clinicId);

			if (clinic == null)
			{
				throw new NotFoundException($"Clinic with ID '{clinicId}' was not found.");
			}
			clinic.VerificationStatus = EVerificationStatus.Rejected;
			clinic.RejectionReason = rejectionReason;
			await _dbContext.SaveChangesAsync();

			await _notificationService.CreateNotificationAsync(clinic.Doctor.User.UserId, $"Your clinic branch '{clinic.ClinicName}' registration has been rejected.<br><b>Reason: {rejectionReason}</b>");
			await _notificationService.SendRefreshSignalAsync("Clinics");
		}

		public async Task UpdateClinicAsync(Guid clinicId, Guid doctorUserId, UpdateClinicDto dto)
		{
			var clinic = await _dbContext.Clinics
				.Include(c => c.Doctor)
				.ThenInclude(d => d.User)
				.Include(c => c.Address)
				.FirstOrDefaultAsync(c => c.ClinicId == clinicId);

			if (clinic == null)
			{
				throw new NotFoundException($"Clinic with ID '{clinicId}' was not found.");
			}

			if (clinic.Doctor.User.UserId != doctorUserId)
			{
				throw new ForbiddenException("You do not have permission to modify this clinic.");
			}

			clinic.ClinicName = dto.ClinicName;
			clinic.ClinicType = dto.ClinicType;
			clinic.VerificationStatus = EVerificationStatus.UpdatedPending;
			clinic.RejectionReason = null;

			clinic.Address.State = dto.State;
			clinic.Address.City = dto.City;
			clinic.Address.Pincode = dto.Pincode;
			clinic.Address.Area = dto.Area;
			clinic.Address.Addressline1 = dto.Addressline1;
			clinic.Address.Addressline2 = dto.Addressline2 ?? string.Empty;

			await _dbContext.SaveChangesAsync();

			// Trigger notification to SuperAdmins
			await _notificationService.CreateNotificationForRoleAsync("SuperAdmin", $"Dr. {clinic.Doctor.FirstName} {clinic.Doctor.LastName} updated clinic details for '{dto.ClinicName}' (requires re-verification).");
			await _notificationService.SendRefreshSignalAsync("Clinics");
		}

		#region Password Hashing Helper
		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}
		#endregion
	}
}
