using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

		private string SerializeClinicState(Clinic clinic)
		{
			if (clinic == null) return "{}";
			var state = new
			{
				clinic.ClinicName,
				clinic.ClinicType,
				clinic.OpenDays,
				clinic.StartTime,
				clinic.EndTime,
				clinic.IsAvailable,
				clinic.UnavailabilityReason,
				clinic.IsDoctorAvailable,
				clinic.DoctorUnavailabilityReason,
				clinic.BookingWindowEndDate,
				clinic.BookingWindowStartDate,
				clinic.SupportedModes,
				clinic.MaxAppointmentsPerDay,
				State = clinic.Address?.State ?? string.Empty,
				City = clinic.Address?.City ?? string.Empty,
				Pincode = clinic.Address?.Pincode ?? string.Empty,
				Area = clinic.Address?.Area ?? string.Empty,
				Addressline1 = clinic.Address?.Addressline1 ?? string.Empty,
				Addressline2 = clinic.Address?.Addressline2 ?? string.Empty
			};
			return JsonSerializer.Serialize(state);
		}

		private string SerializeUpdateDtoState(UpdateClinicDto dto)
		{
			if (dto == null) return "{}";
			var state = new
			{
				dto.ClinicName,
				dto.ClinicType,
				dto.OpenDays,
				dto.StartTime,
				dto.EndTime,
				dto.IsAvailable,
				dto.UnavailabilityReason,
				dto.IsDoctorAvailable,
				dto.DoctorUnavailabilityReason,
				dto.BookingWindowEndDate,
				dto.BookingWindowStartDate,
				dto.SupportedModes,
				dto.MaxAppointmentsPerDay,
				State = dto.State,
				City = dto.City,
				Pincode = dto.Pincode,
				Area = dto.Area,
				Addressline1 = dto.Addressline1,
				Addressline2 = dto.Addressline2 ?? string.Empty
			};
			return JsonSerializer.Serialize(state);
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

			var auditLog = new ClinicAuditLog
			{
				LogId = Guid.NewGuid(),
				ClinicId = clinic.ClinicId,
				Action = "Created",
				ActorUserId = doctor.User.UserId,
				ActorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
				Timestamp = DateTime.UtcNow,
				OldDataJson = "{}",
				NewDataJson = SerializeClinicState(clinic),
				Notes = "New clinic and admin registered. Pending Super Admin verification."
			};
			_dbContext.ClinicAuditLogs.Add(auditLog);
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

			// Validate timing clashes
			await ValidateNoClinicClashAsync(doctor.DoctorId, null, dto.OpenDays, dto.StartTime, dto.EndTime);

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
				OpenDays = dto.OpenDays,
				StartTime = dto.StartTime,
				EndTime = dto.EndTime,
				IsAvailable = dto.IsAvailable,
				UnavailabilityReason = dto.UnavailabilityReason,
				CreatedDate = DateTime.UtcNow
			};
			_dbContext.Clinics.Add(clinic);

			await _dbContext.SaveChangesAsync();

			var auditLog = new ClinicAuditLog
			{
				LogId = Guid.NewGuid(),
				ClinicId = clinic.ClinicId,
				Action = "Created",
				ActorUserId = doctor.User.UserId,
				ActorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
				Timestamp = DateTime.UtcNow,
				OldDataJson = "{}",
				NewDataJson = SerializeClinicState(clinic),
				Notes = "New clinic registered. Pending Super Admin verification."
			};
			_dbContext.ClinicAuditLogs.Add(auditLog);
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
				.Where(c => c.Doctor.User.UserId == doctorUserId && c.ParentClinicId == null)
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
					VerificationStatus = _dbContext.Clinics.Any(clone => clone.ParentClinicId == c.ClinicId && clone.VerificationStatus == EVerificationStatus.UpdatedPending)
						? "UpdatedPending"
						: c.VerificationStatus.ToString(),
					RejectionReason = c.RejectionReason,
					HasAdmin = _dbContext.Admins.Any(a => a.Clinic.ClinicId == c.ClinicId),
					AdminName = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.FirstName + " " + a.LastName).FirstOrDefault(),
					AdminEmail = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.User.Email).FirstOrDefault(),
					AdminMobileNo = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.MobileNo).FirstOrDefault(),
					AdminIsVerified = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.IsVerified).FirstOrDefault(),
					OpenDays = c.OpenDays,
					StartTime = c.StartTime,
					EndTime = c.EndTime,
					IsAvailable = c.IsAvailable,
					UnavailabilityReason = c.UnavailabilityReason,
					IsDoctorAvailable = c.IsDoctorAvailable,
					DoctorUnavailabilityReason = c.DoctorUnavailabilityReason,
					BookingWindowEndDate = c.BookingWindowEndDate,
					BookingWindowStartDate = c.BookingWindowStartDate,
					SupportedModes = c.SupportedModes,
					MaxAppointmentsPerDay = c.MaxAppointmentsPerDay
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId)
		{
			return await _dbContext.Clinics
				.Include(c => c.Doctor)
				.Include(c => c.Address)
				.Where(c => c.Doctor.DoctorId == doctorId && c.VerificationStatus == EVerificationStatus.Verified && c.ParentClinicId == null)
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
					HasAdmin = _dbContext.Admins.Any(a => a.Clinic.ClinicId == c.ClinicId),
					AdminName = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.FirstName + " " + a.LastName).FirstOrDefault(),
					AdminEmail = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.User.Email).FirstOrDefault(),
					AdminMobileNo = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.MobileNo).FirstOrDefault(),
					AdminIsVerified = _dbContext.Admins.Where(a => a.Clinic.ClinicId == c.ClinicId).Select(a => a.IsVerified).FirstOrDefault(),
					OpenDays = c.OpenDays,
					StartTime = c.StartTime,
					EndTime = c.EndTime,
					IsAvailable = c.IsAvailable,
					UnavailabilityReason = c.UnavailabilityReason,
					IsDoctorAvailable = c.IsDoctorAvailable,
					DoctorUnavailabilityReason = c.DoctorUnavailabilityReason,
					BookingWindowEndDate = c.BookingWindowEndDate,
					BookingWindowStartDate = c.BookingWindowStartDate,
					SupportedModes = c.SupportedModes,
					MaxAppointmentsPerDay = c.MaxAppointmentsPerDay
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
					ParentClinicId = c.ParentClinicId,
					HasAdmin = _dbContext.Admins.Any(a => a.Clinic.ClinicId == c.ClinicId),
					OpenDays = c.OpenDays,
					StartTime = c.StartTime,
					EndTime = c.EndTime,
					IsAvailable = c.IsAvailable,
					UnavailabilityReason = c.UnavailabilityReason,
					IsDoctorAvailable = c.IsDoctorAvailable,
					DoctorUnavailabilityReason = c.DoctorUnavailabilityReason,
					BookingWindowEndDate = c.BookingWindowEndDate,
					BookingWindowStartDate = c.BookingWindowStartDate,
					SupportedModes = c.SupportedModes,
					MaxAppointmentsPerDay = c.MaxAppointmentsPerDay
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

		public async Task<string> VerifyClinicAsync(Guid clinicId)
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

			if (clinic.ParentClinicId.HasValue)
			{
				// This is an edit request. Fetch the active verified parent clinic
				var parent = await _dbContext.Clinics
					.Include(c => c.Address)
					.FirstOrDefaultAsync(c => c.ClinicId == clinic.ParentClinicId.Value);

				if (parent != null)
				{
					// Log audit log for edit request approval
					var auditLog = new ClinicAuditLog
					{
						LogId = Guid.NewGuid(),
						ClinicId = parent.ClinicId,
						Action = "Approved",
						ActorUserId = null,
						ActorName = "Super Admin",
						Timestamp = DateTime.UtcNow,
						OldDataJson = SerializeClinicState(parent),
						NewDataJson = SerializeClinicState(clinic),
						Notes = "Proposed edits approved and applied."
					};
					_dbContext.ClinicAuditLogs.Add(auditLog);

					// Update parent details from the approved edit clone
					parent.ClinicName = clinic.ClinicName;
					parent.ClinicType = clinic.ClinicType;
					parent.Address.State = clinic.Address.State;
					parent.Address.City = clinic.Address.City;
					parent.Address.Pincode = clinic.Address.Pincode;
					parent.Address.Area = clinic.Address.Area;
					parent.Address.Addressline1 = clinic.Address.Addressline1;
					parent.Address.Addressline2 = clinic.Address.Addressline2;
					parent.OpenDays = clinic.OpenDays;
					parent.StartTime = clinic.StartTime;
					parent.EndTime = clinic.EndTime;
					parent.IsAvailable = clinic.IsAvailable;
					parent.UnavailabilityReason = clinic.UnavailabilityReason;
					parent.IsDoctorAvailable = clinic.IsDoctorAvailable;
					parent.DoctorUnavailabilityReason = clinic.DoctorUnavailabilityReason;
					parent.BookingWindowEndDate = clinic.BookingWindowEndDate;
					parent.BookingWindowStartDate = clinic.BookingWindowStartDate;
					parent.SupportedModes = clinic.SupportedModes;
					parent.MaxAppointmentsPerDay = clinic.MaxAppointmentsPerDay;
					parent.VerificationStatus = EVerificationStatus.Verified;
					parent.RejectionReason = null;

					// Remove the clone request record and its proposed address record
					_dbContext.Clinics.Remove(clinic);
					_dbContext.Addresses.Remove(clinic.Address);

					await _dbContext.SaveChangesAsync();

					await _notificationService.CreateNotificationAsync(parent.Doctor.User.UserId, $"Your proposed edits for clinic branch '{parent.ClinicName}' have been approved and applied.");
				}
			}
			else
			{
				// This is a new clinic registration verification
				var auditLog = new ClinicAuditLog
				{
					LogId = Guid.NewGuid(),
					ClinicId = clinic.ClinicId,
					Action = "Approved",
					ActorUserId = null,
					ActorName = "Super Admin",
					Timestamp = DateTime.UtcNow,
					OldDataJson = "{}",
					NewDataJson = SerializeClinicState(clinic),
					Notes = "New clinic registration approved and verified."
				};
				_dbContext.ClinicAuditLogs.Add(auditLog);

				clinic.VerificationStatus = EVerificationStatus.Verified;
				clinic.RejectionReason = null;
				await _dbContext.SaveChangesAsync();

				await _notificationService.CreateNotificationAsync(clinic.Doctor.User.UserId, $"Your clinic branch '{clinic.ClinicName}' has been verified and approved by the Super Admin.");
			}

			await _notificationService.SendRefreshSignalAsync("Clinics");
			return clinic.ClinicName;
		}

		public async Task<string> VerifyAdminAsync(Guid adminId)
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
			return $"{admin.FirstName} {admin.LastName}";
		}

		public async Task<string> RejectAdminAsync(Guid adminId)
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

			var adminName = $"{admin.FirstName} {admin.LastName}";
			var clinicName = admin.Clinic.ClinicName;
			var doctorUserId = admin.Clinic.Doctor.User.UserId;
			var adminUserId = admin.User.UserId;

			// Remove admin record and deactivate user
			_dbContext.Admins.Remove(admin);
			admin.User.IsActive = false;
			await _dbContext.SaveChangesAsync();

			// Notify the admin user
			await _notificationService.CreateNotificationAsync(adminUserId, $"Your Clinic Admin account for '{clinicName}' has been rejected by the Super Admin.");
			// Notify the doctor
			await _notificationService.CreateNotificationAsync(doctorUserId, $"The Clinic Admin {adminName} assigned to '{clinicName}' has been rejected by the Super Admin.");
			await _notificationService.SendRefreshSignalAsync("Admins");
			return adminName;
		}

		public async Task<string> RejectClinicAsync(Guid clinicId, string rejectionReason)
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

			if (clinic.ParentClinicId.HasValue)
			{
				// Log audit log for edit request rejection
				var auditLog = new ClinicAuditLog
				{
					LogId = Guid.NewGuid(),
					ClinicId = clinic.ParentClinicId.Value,
					Action = "Rejected",
					ActorUserId = null,
					ActorName = "Super Admin",
					Timestamp = DateTime.UtcNow,
					OldDataJson = SerializeClinicState(clinic),
					NewDataJson = "{}",
					Notes = $"Proposed edits rejected. Reason: {rejectionReason}"
				};
				_dbContext.ClinicAuditLogs.Add(auditLog);

				// This is a proposed edit clone rejection. Discard and remove it!
				_dbContext.Clinics.Remove(clinic);
				_dbContext.Addresses.Remove(clinic.Address);
				await _dbContext.SaveChangesAsync();

				await _notificationService.CreateNotificationAsync(clinic.Doctor.User.UserId, $"Your proposed edits for clinic branch '{clinic.ClinicName}' were rejected by the Super Admin.<br><b>Reason: {rejectionReason}</b>");
			}
			else
			{
				// Log audit log for rejection
				var auditLog = new ClinicAuditLog
				{
					LogId = Guid.NewGuid(),
					ClinicId = clinic.ClinicId,
					Action = "Rejected",
					ActorUserId = null,
					ActorName = "Super Admin",
					Timestamp = DateTime.UtcNow,
					OldDataJson = SerializeClinicState(clinic),
					NewDataJson = "{}",
					Notes = $"New clinic registration rejected. Reason: {rejectionReason}"
				};
				_dbContext.ClinicAuditLogs.Add(auditLog);

				// This is a new clinic registration rejection
				clinic.VerificationStatus = EVerificationStatus.Rejected;
				clinic.RejectionReason = rejectionReason;
				await _dbContext.SaveChangesAsync();

				await _notificationService.CreateNotificationAsync(clinic.Doctor.User.UserId, $"Your clinic branch '{clinic.ClinicName}' registration has been rejected.<br><b>Reason: {rejectionReason}</b>");
			}

			await _notificationService.SendRefreshSignalAsync("Clinics");
			return clinic.ClinicName;
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

			bool isDtoTimingEmpty = string.IsNullOrWhiteSpace(dto.OpenDays) && 
			                        string.IsNullOrWhiteSpace(dto.StartTime) && 
			                        string.IsNullOrWhiteSpace(dto.EndTime);

			bool isDbTimingEmpty = string.IsNullOrWhiteSpace(clinic.OpenDays) && 
			                       string.IsNullOrWhiteSpace(clinic.StartTime) && 
			                       string.IsNullOrWhiteSpace(clinic.EndTime);

			if (dto.IsAvailable && !isDtoTimingEmpty && 
			    (string.IsNullOrWhiteSpace(dto.OpenDays) || string.IsNullOrWhiteSpace(dto.StartTime) || string.IsNullOrWhiteSpace(dto.EndTime)))
			{
				throw new BadRequestException("Active/Open clinics must have a timing schedule (open days, start time, and end time) configured.");
			}

			// Validate timing clashes (exclude this clinic itself from clash check)
			await ValidateNoClinicClashAsync(clinic.Doctor.DoctorId, clinicId, dto.OpenDays, dto.StartTime, dto.EndTime, dto.IsAvailable, dto.IsDoctorAvailable);

			bool detailsChanged = clinic.ClinicName != dto.ClinicName ||
			                      clinic.ClinicType != dto.ClinicType ||
			                      clinic.Address.State != dto.State ||
			                      clinic.Address.City != dto.City ||
			                      clinic.Address.Area != dto.Area ||
			                      clinic.Address.Pincode != dto.Pincode ||
			                      clinic.Address.Addressline1 != dto.Addressline1 ||
			                      clinic.Address.Addressline2 != (dto.Addressline2 ?? string.Empty);

			if (detailsChanged)
			{
				// Find existing active edit clone if any
				var clone = await _dbContext.Clinics
					.Include(c => c.Address)
					.FirstOrDefaultAsync(c => c.ParentClinicId == clinicId && c.VerificationStatus == EVerificationStatus.UpdatedPending);

				if (clone == null)
				{
					// Create new edit request clone record
					var proposedAddress = new Address
					{
						State = dto.State,
						City = dto.City,
						Pincode = dto.Pincode,
						Area = dto.Area,
						Addressline1 = dto.Addressline1,
						Addressline2 = dto.Addressline2 ?? string.Empty,
						Country = clinic.Address.Country ?? "India",
						User = clinic.Doctor.User
					};

					clone = new Clinic
					{
						ClinicId = Guid.NewGuid(),
						ClinicName = dto.ClinicName,
						ClinicType = dto.ClinicType,
						Doctor = clinic.Doctor,
						Address = proposedAddress,
						VerificationStatus = EVerificationStatus.UpdatedPending,
						ParentClinicId = clinicId,
						OpenDays = string.IsNullOrWhiteSpace(dto.OpenDays) ? clinic.OpenDays : dto.OpenDays,
						StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? clinic.StartTime : dto.StartTime,
						EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? clinic.EndTime : dto.EndTime,
						IsAvailable = dto.IsAvailable,
						UnavailabilityReason = dto.UnavailabilityReason,
						IsDoctorAvailable = dto.IsDoctorAvailable,
						DoctorUnavailabilityReason = dto.DoctorUnavailabilityReason,
						BookingWindowEndDate = dto.BookingWindowEndDate,
						BookingWindowStartDate = dto.BookingWindowStartDate,
						SupportedModes = dto.SupportedModes
					};
					_dbContext.Clinics.Add(clone);
				}
				else
				{
					// Update existing edit request clone record
					clone.ClinicName = dto.ClinicName;
					clone.ClinicType = dto.ClinicType;
					clone.OpenDays = string.IsNullOrWhiteSpace(dto.OpenDays) ? clinic.OpenDays : dto.OpenDays;
					clone.StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? clinic.StartTime : dto.StartTime;
					clone.EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? clinic.EndTime : dto.EndTime;
					clone.IsAvailable = dto.IsAvailable;
					clone.UnavailabilityReason = dto.UnavailabilityReason;
					clone.IsDoctorAvailable = dto.IsDoctorAvailable;
					clone.DoctorUnavailabilityReason = dto.DoctorUnavailabilityReason;
					clone.BookingWindowEndDate = dto.BookingWindowEndDate;
					clone.BookingWindowStartDate = dto.BookingWindowStartDate;
					clone.SupportedModes = dto.SupportedModes;
					clone.Address.State = dto.State;
					clone.Address.City = dto.City;
					clone.Address.Pincode = dto.Pincode;
					clone.Address.Area = dto.Area;
					clone.Address.Addressline1 = dto.Addressline1;
					clone.Address.Addressline2 = dto.Addressline2 ?? string.Empty;
				}

				// Log audit log for edit request submission
				var auditLog = new ClinicAuditLog
				{
					LogId = Guid.NewGuid(),
					ClinicId = clinic.ClinicId,
					Action = "EditSubmitted",
					ActorUserId = clinic.Doctor.User.UserId,
					ActorName = $"Dr. {clinic.Doctor.FirstName} {clinic.Doctor.LastName}",
					Timestamp = DateTime.UtcNow,
					OldDataJson = SerializeClinicState(clinic),
					NewDataJson = SerializeUpdateDtoState(dto),
					Notes = "Edit request submitted. Pending Super Admin approval."
				};
				_dbContext.ClinicAuditLogs.Add(auditLog);
			}
			else
			{
				// Only timings or availability changed, update the active record directly
				// Log audit log for timings update
				var auditLog = new ClinicAuditLog
				{
					LogId = Guid.NewGuid(),
					ClinicId = clinic.ClinicId,
					Action = "TimingsUpdated",
					ActorUserId = clinic.Doctor.User.UserId,
					ActorName = $"Dr. {clinic.Doctor.FirstName} {clinic.Doctor.LastName}",
					Timestamp = DateTime.UtcNow,
					OldDataJson = SerializeClinicState(clinic),
					NewDataJson = SerializeUpdateDtoState(dto),
					Notes = "Timings and availability updated directly."
				};
				_dbContext.ClinicAuditLogs.Add(auditLog);

				clinic.OpenDays = dto.OpenDays;
				clinic.StartTime = dto.StartTime;
				clinic.EndTime = dto.EndTime;
				clinic.IsAvailable = dto.IsAvailable;
				clinic.UnavailabilityReason = dto.UnavailabilityReason;
				clinic.IsDoctorAvailable = dto.IsDoctorAvailable;
				clinic.DoctorUnavailabilityReason = dto.DoctorUnavailabilityReason;
				clinic.BookingWindowEndDate = dto.BookingWindowEndDate;
				clinic.BookingWindowStartDate = dto.BookingWindowStartDate;
				clinic.SupportedModes = dto.SupportedModes;
				clinic.MaxAppointmentsPerDay = dto.MaxAppointmentsPerDay;

				if (clinic.VerificationStatus == EVerificationStatus.Verified || clinic.VerificationStatus == EVerificationStatus.UpdatedPending)
				{
					clinic.VerificationStatus = EVerificationStatus.Verified;
				}
			}

			await _dbContext.SaveChangesAsync();

			if (detailsChanged)
			{
				// Trigger notification to SuperAdmins only when details actually change
				await _notificationService.CreateNotificationForRoleAsync("SuperAdmin", $"Dr. {clinic.Doctor.FirstName} {clinic.Doctor.LastName} submitted clinic details edit request for '{dto.ClinicName}' (requires verification).");
			}

			await _notificationService.SendRefreshSignalAsync("Clinics");
		}

		public async Task AdminUpdateClinicAsync(Guid adminUserId, UpdateClinicDto dto)
		{
			var admin = await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.ThenInclude(c => c.Address)
				.Include(a => a.Clinic.Doctor)
				.FirstOrDefaultAsync(a => a.User.UserId == adminUserId);

			if (admin == null)
			{
				throw new NotFoundException("Clinic Admin profile was not found.");
			}

			if (!admin.IsVerified)
			{
				throw new ForbiddenException("Your Clinic Admin account is pending verification.");
			}

			var clinic = admin.Clinic;

			bool isDtoTimingEmpty = string.IsNullOrWhiteSpace(dto.OpenDays) && 
			                        string.IsNullOrWhiteSpace(dto.StartTime) && 
			                        string.IsNullOrWhiteSpace(dto.EndTime);

			bool isDbTimingEmpty = string.IsNullOrWhiteSpace(clinic.OpenDays) && 
			                       string.IsNullOrWhiteSpace(clinic.StartTime) && 
			                       string.IsNullOrWhiteSpace(clinic.EndTime);

			if (dto.IsAvailable && !isDtoTimingEmpty && 
			    (string.IsNullOrWhiteSpace(dto.OpenDays) || string.IsNullOrWhiteSpace(dto.StartTime) || string.IsNullOrWhiteSpace(dto.EndTime)))
			{
				throw new BadRequestException("Active/Open clinics must have a timing schedule (open days, start time, and end time) configured.");
			}

			await ValidateNoClinicClashAsync(clinic.Doctor.DoctorId, clinic.ClinicId, dto.OpenDays, dto.StartTime, dto.EndTime, dto.IsAvailable, dto.IsDoctorAvailable);

			// Log audit log for direct admin edit
			var auditLog = new ClinicAuditLog
			{
				LogId = Guid.NewGuid(),
				ClinicId = clinic.ClinicId,
				Action = "TimingsUpdated",
				ActorUserId = admin.User.UserId,
				ActorName = $"{admin.FirstName} {admin.LastName} (Clinic Admin)",
				Timestamp = DateTime.UtcNow,
				OldDataJson = SerializeClinicState(clinic),
				NewDataJson = SerializeUpdateDtoState(dto),
				Notes = "Clinic details updated directly by Clinic Admin."
			};
			_dbContext.ClinicAuditLogs.Add(auditLog);

			clinic.ClinicName = dto.ClinicName;
			clinic.ClinicType = dto.ClinicType;
			clinic.OpenDays = string.IsNullOrWhiteSpace(dto.OpenDays) ? clinic.OpenDays : dto.OpenDays;
			clinic.StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? clinic.StartTime : dto.StartTime;
			clinic.EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? clinic.EndTime : dto.EndTime;
			clinic.IsAvailable = dto.IsAvailable;
			clinic.UnavailabilityReason = dto.UnavailabilityReason;
			clinic.IsDoctorAvailable = dto.IsDoctorAvailable;
			clinic.DoctorUnavailabilityReason = dto.DoctorUnavailabilityReason;
			clinic.BookingWindowEndDate = dto.BookingWindowEndDate;
			clinic.BookingWindowStartDate = dto.BookingWindowStartDate;
			clinic.SupportedModes = dto.SupportedModes;
		clinic.MaxAppointmentsPerDay = dto.MaxAppointmentsPerDay;

			// Don't drop verification status of a clinic for trusted admin edits
			clinic.VerificationStatus = EVerificationStatus.Verified;

			clinic.Address.State = dto.State;
			clinic.Address.City = dto.City;
			clinic.Address.Pincode = dto.Pincode;
			clinic.Address.Area = dto.Area;
			clinic.Address.Addressline1 = dto.Addressline1;
			clinic.Address.Addressline2 = dto.Addressline2 ?? string.Empty;

			await _dbContext.SaveChangesAsync();
			await _notificationService.SendRefreshSignalAsync("Clinics");
		}

		public async Task<ClinicDto> GetAdminClinicAsync(Guid adminUserId)
		{
			var admin = await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.ThenInclude(c => c.Address)
				.Include(a => a.Clinic.Doctor)
				.FirstOrDefaultAsync(a => a.User.UserId == adminUserId);

			if (admin == null)
			{
				throw new NotFoundException("Clinic Admin profile was not found.");
			}

			var c = admin.Clinic;
			return new ClinicDto
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
				HasAdmin = true,
				AdminName = $"{admin.FirstName} {admin.LastName}",
				AdminEmail = admin.User.Email,
				AdminMobileNo = admin.MobileNo,
				AdminIsVerified = admin.IsVerified,
				OpenDays = c.OpenDays,
				StartTime = c.StartTime,
				EndTime = c.EndTime,
				IsAvailable = c.IsAvailable,
				UnavailabilityReason = c.UnavailabilityReason
			};
		}

		private List<(TimeSpan Start, TimeSpan End)> ParseIntervals(string startTimeStr, string endTimeStr)
		{
			var intervals = new List<(TimeSpan Start, TimeSpan End)>();
			var starts = startTimeStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var ends = endTimeStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			int count = Math.Min(starts.Length, ends.Length);
			for (int i = 0; i < count; i++)
			{
				if (TimeSpan.TryParse(starts[i], out var start) && TimeSpan.TryParse(ends[i], out var end))
				{
					if (start >= end)
					{
						throw new BadRequestException($"Invalid shift timings: Start time {starts[i]} must be before end time {ends[i]}.");
					}
					intervals.Add((start, end));
				}
				else
				{
					throw new BadRequestException("Invalid time format. Use HH:mm.");
				}
			}

			var sortedIntervals = intervals.OrderBy(inv => inv.Start).ToList();
			for (int i = 0; i < sortedIntervals.Count - 1; i++)
			{
				if (sortedIntervals[i].End > sortedIntervals[i + 1].Start)
				{
					throw new BadRequestException($"Shift timings overlap: Session {i + 1} ({sortedIntervals[i].Start.ToString(@"hh\:mm")} - {sortedIntervals[i].End.ToString(@"hh\:mm")}) cannot end after Session {i + 2} ({sortedIntervals[i + 1].Start.ToString(@"hh\:mm")} - {sortedIntervals[i + 1].End.ToString(@"hh\:mm")}) starts.");
				}
			}

			return intervals;
		}

		private async Task ValidateNoClinicClashAsync(Guid doctorId, Guid? excludingClinicId, string? openDays, string? startTimeStr, string? endTimeStr, bool isAvailable = true, bool isDoctorAvailable = true)
		{
			if (!isAvailable || !isDoctorAvailable)
			{
				return; // Skip scheduling clash validation entirely if this clinic is closed or the doctor is unavailable
			}

			if (string.IsNullOrEmpty(openDays) || string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
			{
				return; // No schedule set yet, so no clash validation needed
			}

			// Parse intervals
			var newIntervals = ParseIntervals(startTimeStr, endTimeStr);
			if (!newIntervals.Any())
			{
				throw new BadRequestException("No valid time intervals configured.");
			}

			// Split the open days of the new clinic
			var newDays = openDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(d => d.ToLower())
				.ToList();

			// Fetch all clinics of this doctor that are active and available (ignore closed or doctor unavailable branches)
			var query = _dbContext.Clinics
				.Include(c => c.Doctor)
				.Where(c => c.Doctor.DoctorId == doctorId 
				         && c.VerificationStatus != EVerificationStatus.Rejected
				         && c.IsAvailable == true
				         && c.IsDoctorAvailable == true);

			if (excludingClinicId.HasValue)
			{
				query = query.Where(c => c.ClinicId != excludingClinicId.Value);
			}

			var existingClinics = await query.ToListAsync();

			foreach (var existing in existingClinics)
			{
				if (string.IsNullOrEmpty(existing.OpenDays) || string.IsNullOrEmpty(existing.StartTime) || string.IsNullOrEmpty(existing.EndTime))
				{
					continue; // This clinic has no schedule yet
				}

				// Check if they share any days
				var existingDays = existing.OpenDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(d => d.ToLower())
					.ToList();

				var commonDays = newDays.Intersect(existingDays).ToList();
				if (commonDays.Any())
				{
					var existingIntervals = ParseIntervals(existing.StartTime, existing.EndTime);
					
					foreach (var newInv in newIntervals)
					{
						foreach (var existInv in existingIntervals)
						{
							if (newInv.Start < existInv.End && existInv.Start < newInv.End)
							{
								var daysStr = string.Join(", ", commonDays.Select(d => char.ToUpper(d[0]) + d.Substring(1)));
								var startFormatted = newInv.Start.ToString(@"hh\:mm");
								var endFormatted = newInv.End.ToString(@"hh\:mm");
								var existStartFormatted = existInv.Start.ToString(@"hh\:mm");
								var existEndFormatted = existInv.End.ToString(@"hh\:mm");
								throw new BadRequestException($"Schedule clash on {daysStr} with clinic '{existing.ClinicName}' ({existStartFormatted} - {existEndFormatted}).");
							}
						}
					}
				}
			}
		}

		public async Task<IEnumerable<ClinicAuditLogDto>> GetClinicHistoryAsync(Guid clinicId)
		{
			return await _dbContext.ClinicAuditLogs
				.Where(l => l.ClinicId == clinicId)
				.OrderByDescending(l => l.Timestamp)
				.Select(l => new ClinicAuditLogDto
				{
					LogId = l.LogId,
					ClinicId = l.ClinicId,
					Action = l.Action,
					ActorUserId = l.ActorUserId,
					ActorName = l.ActorName,
					Timestamp = l.Timestamp,
					OldDataJson = l.OldDataJson,
					NewDataJson = l.NewDataJson,
					Notes = l.Notes
				})
				.ToListAsync();
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
