using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

using System.Security.Cryptography;
using System.Text;

namespace DoctorAppointmentSystem.Application.Services
{
	public class AdminService : IAdminService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly INotificationService _notificationService;
		private readonly IDistributedCache _distributedCache;
		private readonly IEmailService _emailService;

		public AdminService(
			ApplicationDbContext dbContext,
			INotificationService notificationService,
			IDistributedCache distributedCache,
			IEmailService emailService)
		{
			_dbContext = dbContext;
			_notificationService = notificationService;
			_distributedCache = distributedCache;
			_emailService = emailService;
		}

		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}

		public async Task<string> VerifyDoctorAsync(Guid doctorId, string status)
		{
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.FirstOrDefaultAsync(d => d.DoctorId == doctorId);

			if (doctor == null)
			{
				throw new NotFoundException($"Doctor profile with ID '{doctorId}' was not found.");
			}

			if (!Enum.TryParse<EVerificationStatus>(status, true, out var parsedStatus))
			{
				throw new BadRequestException($"VerificationStatus '{status}' is invalid. Allowed: Verified, Pending, Rejected.");
			}

			if (parsedStatus == EVerificationStatus.Rejected)
			{
				var emailSubject = "HealSync - Doctor Profile Application Status Update";
				var emailBody = $@"
					<h3>Hello Dr. {doctor.FirstName} {doctor.LastName},</h3>
					<p>Thank you for your interest in joining the HealSync Medical Network.</p>
					<p>After reviewing your onboarding credentials and medical license details, we regret to inform you that your application has been rejected at this time.</p>
					<p>Your profile and account registrations have been completely removed from our system. If you believe this was an error or wish to apply again with updated credentials, you are free to register a new profile using your email address.</p>
					<p>Best regards,<br/>HealSync Administration Team</p>";

				try
				{
					await _emailService.SendEmailAsync(doctor.User.Email, emailSubject, emailBody);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[Email Error]: Failed to send rejection email to doctor: {ex.Message}");
				}

				// Remove associated Addresses
				var addresses = _dbContext.Addresses.Where(a => a.User.UserId == doctor.User.UserId);
				_dbContext.Addresses.RemoveRange(addresses);

				// Remove Doctor profile and User credentials
				_dbContext.Doctors.Remove(doctor);
				_dbContext.Users.Remove(doctor.User);

				await _dbContext.SaveChangesAsync();

				// Evict the available doctors cache
				await _distributedCache.RemoveAsync("available_doctors_list");
				await _notificationService.SendRefreshSignalAsync("Doctors");

				return $"Dr. {doctor.FirstName} {doctor.LastName} (Rejected and Purged)";
			}

			var oldData = JsonSerializer.Serialize(new { doctor.VerificationStatus });
			doctor.VerificationStatus = parsedStatus;
			doctor.UpdatedDate = DateTime.UtcNow;

			string generatedPassword = string.Empty;
			if (parsedStatus == EVerificationStatus.Verified)
			{
				// Auto-generate a secure temporary password (e.g. 10 characters)
				generatedPassword = Guid.NewGuid().ToString("N").Substring(0, 10);
				doctor.User.PasswordHash = HashPassword(generatedPassword);
				doctor.User.RequiresPasswordChange = true;
				doctor.User.IsEmailVerified = true; // Auto-verify email once superadmin approves
			}

			var auditLog = new DoctorAuditLog
			{
				DoctorId = doctor.DoctorId,
				Action = parsedStatus == EVerificationStatus.Verified ? "Approved" : "Rejected",
				Timestamp = DateTime.UtcNow,
				OldDataJson = oldData,
				NewDataJson = JsonSerializer.Serialize(new { doctor.VerificationStatus }),
				Notes = $"Doctor verification status changed to {status}"
			};
			_dbContext.DoctorAuditLogs.Add(auditLog);

			await _dbContext.SaveChangesAsync();

			// Evict the available doctors cache since status has changed
			await _distributedCache.RemoveAsync("available_doctors_list");

			if (parsedStatus == EVerificationStatus.Verified && !string.IsNullOrEmpty(generatedPassword))
			{
				var emailSubject = "HealSync - Doctor Account Approved & Activated";
				var emailBody = $@"
					<h3>Congratulations Dr. {doctor.FirstName} {doctor.LastName}!</h3>
					<p>Your doctor onboarding profile has been approved and activated by the Super Admin.</p>
					<p>Here are your secure temporary credentials to log in to the HealSync Portal:</p>
					<table border='0' cellpadding='5'>
						<tr><td><strong>Portal Link:</strong></td><td><a href='http://localhost:4200/doctor/login'>HealSync Doctor Portal</a></td></tr>
						<tr><td><strong>Username:</strong></td><td>{doctor.User.Email}</td></tr>
						<tr><td><strong>Temporary Password:</strong></td><td><code>{generatedPassword}</code></td></tr>
					</table>
					<p><em>Note: You will be required to change this temporary password immediately upon your first login.</em></p>
					<p>Best regards,<br/>HealSync Administration Team</p>";

				try
				{
					await _emailService.SendEmailAsync(doctor.User.Email, emailSubject, emailBody);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[Email Error]: Failed to send approval email to doctor: {ex.Message}");
				}
			}

			await _notificationService.CreateNotificationAsync(doctor.User.UserId, $"Your doctor profile verification status has been updated to: {status}.");
			await _notificationService.SendRefreshSignalAsync("Doctors");

			return $"Dr. {doctor.FirstName} {doctor.LastName}";
		}

		public async Task<IEnumerable<DoctorDto>> GetPendingDoctorsAsync()
		{
			return await _dbContext.Doctors
				.Include(d => d.Specialization)
				.Include(d => d.User)
				.Where(d => d.VerificationStatus == EVerificationStatus.Pending)
				.Select(d => new DoctorDto
				{
					DoctorId = d.DoctorId,
					UserId = d.User.UserId,
					Email = d.User.Email,
					SpecializationId = d.Specialization != null ? d.Specialization.SpecializationId : Guid.Empty,
					SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : string.Empty,
					FirstName = d.FirstName,
					LastName = d.LastName,
					MobileNo = d.MobileNo,
					Qualification = d.Qualification,
					LicenceNumber = d.LicenceNumber,
					YearsOfExperience = d.YearsOfExperience,
					ConsultationFee = d.ConsultationFee,
					AboutDoctor = d.AboutDoctor ?? string.Empty,
					VerificationStatus = d.VerificationStatus.ToString(),
					State = _dbContext.Addresses.Where(a => a.User.UserId == d.User.UserId).Select(a => a.State).FirstOrDefault() ?? string.Empty,
					City = _dbContext.Addresses.Where(a => a.User.UserId == d.User.UserId).Select(a => a.City).FirstOrDefault() ?? string.Empty,
					CreatedDate = d.CreatedDate,
					UpdatedDate = d.UpdatedDate,
					Age = DateTime.UtcNow.Year - d.DOB.Year,
					Clinics = d.Clinics.Select(c => new ClinicBasicDto
					{
						ClinicId = c.ClinicId,
						ClinicName = c.ClinicName,
						ClinicType = c.ClinicType,
						State = c.Address.State,
						City = c.Address.City,
						Area = c.Address.Area,
						ContactNumber = c.ContactNumber
					}).ToList()
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync(string? search, string? status, DateTime? registerDate, DateTime? approveDate)
		{
			var query = _dbContext.Doctors
				.Include(d => d.Specialization)
				.Include(d => d.User)
				.AsQueryable();

			if (!string.IsNullOrEmpty(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(d => 
					d.FirstName.ToLower().Contains(searchLower) ||
					d.LastName.ToLower().Contains(searchLower) ||
					d.LicenceNumber.ToLower().Contains(searchLower) ||
					d.MobileNo.Contains(searchLower)
				);
			}

			if (!string.IsNullOrEmpty(status))
			{
				if (Enum.TryParse<EVerificationStatus>(status, true, out var parsedStatus))
				{
					query = query.Where(d => d.VerificationStatus == parsedStatus);
				}
			}

			if (registerDate.HasValue)
			{
				var targetDate = registerDate.Value.Date;
				query = query.Where(d => d.CreatedDate.Date == targetDate);
			}

			if (approveDate.HasValue)
			{
				var targetDate = approveDate.Value.Date;
				query = query.Where(d => d.UpdatedDate.Date == targetDate);
			}

			return await query
				.OrderByDescending(d => d.CreatedDate)
				.Select(d => new DoctorDto
				{
					DoctorId = d.DoctorId,
					UserId = d.User.UserId,
					Email = d.User.Email,
					SpecializationId = d.Specialization != null ? d.Specialization.SpecializationId : Guid.Empty,
					SpecializationName = d.Specialization != null ? d.Specialization.SpecializationName : string.Empty,
					FirstName = d.FirstName,
					LastName = d.LastName,
					MobileNo = d.MobileNo,
					Qualification = d.Qualification,
					LicenceNumber = d.LicenceNumber,
					YearsOfExperience = d.YearsOfExperience,
					ConsultationFee = d.ConsultationFee,
					AboutDoctor = d.AboutDoctor ?? string.Empty,
					VerificationStatus = d.VerificationStatus.ToString(),
					State = _dbContext.Addresses.Where(a => a.User.UserId == d.User.UserId).Select(a => a.State).FirstOrDefault() ?? string.Empty,
					City = _dbContext.Addresses.Where(a => a.User.UserId == d.User.UserId).Select(a => a.City).FirstOrDefault() ?? string.Empty,
					CreatedDate = d.CreatedDate,
					UpdatedDate = d.UpdatedDate,
					Age = DateTime.UtcNow.Year - d.DOB.Year,
					Clinics = d.Clinics.Select(c => new ClinicBasicDto
					{
						ClinicId = c.ClinicId,
						ClinicName = c.ClinicName,
						ClinicType = c.ClinicType,
						State = c.Address.State,
						City = c.Address.City,
						Area = c.Address.Area,
						ContactNumber = c.ContactNumber
					}).ToList()
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicDto>> GetAllClinicsAsync(string? search, string? state, string? city, bool? isVerified)
		{
			var query = _dbContext.Clinics
				.Include(c => c.Doctor)
				.Include(c => c.Address)
				.Where(c => c.ParentClinicId == null || c.VerificationStatus == EVerificationStatus.UpdatedPending)
				.AsQueryable();

			if (!string.IsNullOrEmpty(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(c => 
					c.ClinicName.ToLower().Contains(searchLower) ||
					c.ClinicType.ToLower().Contains(searchLower) ||
					c.Doctor.FirstName.ToLower().Contains(searchLower) ||
					c.Doctor.LastName.ToLower().Contains(searchLower)
				);
			}

			if (!string.IsNullOrEmpty(state))
			{
				var stateLower = state.ToLower();
				query = query.Where(c => c.Address.State.ToLower().Contains(stateLower));
			}

			if (!string.IsNullOrEmpty(city))
			{
				var cityLower = city.ToLower();
				query = query.Where(c => c.Address.City.ToLower().Contains(cityLower));
			}

			if (isVerified.HasValue)
			{
				query = query.Where(c => (c.VerificationStatus == EVerificationStatus.Verified) == isVerified.Value);
			}

			return await query
				.OrderBy(c => c.VerificationStatus == EVerificationStatus.Verified ? 1 : 0)
				.ThenByDescending(c => c.CreatedDate)
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
					OpenDays = c.OpenDays,
					StartTime = c.StartTime,
					EndTime = c.EndTime,
					IsAvailable = c.IsAvailable,
					UnavailabilityReason = c.UnavailabilityReason,
					IsDoctorAvailable = c.IsDoctorAvailable,
					DoctorUnavailabilityReason = c.DoctorUnavailabilityReason,
					BookingWindowStartDate = c.BookingWindowStartDate,
					BookingWindowEndDate = c.BookingWindowEndDate,
					SupportedModes = c.SupportedModes,
					HasAdmin = _dbContext.AdminClinics.Any(ac => ac.ClinicId == (c.ParentClinicId ?? c.ClinicId)),
					AdminName = _dbContext.AdminClinics.Where(ac => ac.ClinicId == (c.ParentClinicId ?? c.ClinicId)).Select(ac => ac.Admin.FirstName + " " + ac.Admin.LastName).FirstOrDefault(),
					AdminEmail = _dbContext.AdminClinics.Where(ac => ac.ClinicId == (c.ParentClinicId ?? c.ClinicId)).Select(ac => ac.Admin.User.Email).FirstOrDefault(),
					AdminMobileNo = _dbContext.AdminClinics.Where(ac => ac.ClinicId == (c.ParentClinicId ?? c.ClinicId)).Select(ac => ac.Admin.MobileNo).FirstOrDefault(),
					AdminIsVerified = _dbContext.AdminClinics.Where(ac => ac.ClinicId == (c.ParentClinicId ?? c.ClinicId)).Select(ac => ac.Admin.IsVerified).FirstOrDefault()
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicAdminDto>> GetAllAdminsAsync(string? search, bool? isVerified)
		{
			var query = _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.AdminClinics)
					.ThenInclude(ac => ac.Clinic)
				.AsQueryable();

			if (!string.IsNullOrEmpty(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(a => 
					a.FirstName.ToLower().Contains(searchLower) ||
					a.LastName.ToLower().Contains(searchLower) ||
					a.AdminClinics.Any(ac => ac.Clinic.ClinicName.ToLower().Contains(searchLower)) ||
					a.MobileNo.Contains(searchLower)
				);
			}

			if (isVerified.HasValue)
			{
				query = query.Where(a => a.IsVerified == isVerified.Value);
			}

			var admins = await query
				.OrderByDescending(a => a.CreatedDate)
				.ToListAsync();

			return admins.Select(a =>
			{
				var firstClinic = a.AdminClinics?.FirstOrDefault()?.Clinic;
				return new ClinicAdminDto
				{
					AdminId = a.AdminId,
					UserId = a.User.UserId,
					ClinicId = firstClinic?.ClinicId ?? Guid.Empty,
					ClinicName = firstClinic?.ClinicName ?? string.Empty,
					FirstName = a.FirstName,
					LastName = a.LastName,
					MobileNo = a.MobileNo,
					IsVerified = a.IsVerified,
					AssignedClinics = a.AdminClinics?.Select(ac => new ClinicBasicInfoDto
					{
						ClinicId = ac.Clinic.ClinicId,
						ClinicName = ac.Clinic.ClinicName
					}).ToList() ?? new List<ClinicBasicInfoDto>()
				};
			}).ToList();
		}
		public async Task<PagedResult<SystemAuditLogDto>> GetSystemAuditLogsAsync(string? entityType, string? action, DateTime? startDate, DateTime? endDate, int page, int size)
		{
			var clinicLogs = _dbContext.ClinicAuditLogs.Select(l => new SystemAuditLogDto
			{
				LogId = l.LogId,
				EntityType = "Clinic",
				EntityId = l.ClinicId,
				EntityName = _dbContext.Clinics.Where(c => c.ClinicId == l.ClinicId).Select(c => c.ClinicName).FirstOrDefault() ?? "Unknown",
				Action = l.Action,
				ActorUserId = l.ActorUserId,
				ActorName = l.ActorName,
				Timestamp = l.Timestamp,
				OldDataJson = l.OldDataJson,
				NewDataJson = l.NewDataJson,
				Notes = l.Notes
			});

			var doctorLogs = _dbContext.DoctorAuditLogs.Select(l => new SystemAuditLogDto
			{
				LogId = l.LogId,
				EntityType = "Doctor",
				EntityId = l.DoctorId,
				EntityName = _dbContext.Doctors.Where(d => d.DoctorId == l.DoctorId).Select(d => "Dr. " + d.FirstName + " " + d.LastName).FirstOrDefault() ?? "Unknown",
				Action = l.Action,
				ActorUserId = l.ActorUserId,
				ActorName = l.ActorName,
				Timestamp = l.Timestamp,
				OldDataJson = l.OldDataJson,
				NewDataJson = l.NewDataJson,
				Notes = l.Notes
			});

			var adminLogs = _dbContext.AdminAuditLogs.Select(l => new SystemAuditLogDto
			{
				LogId = l.LogId,
				EntityType = "Admin",
				EntityId = l.AdminId,
				EntityName = _dbContext.Admins.Where(a => a.AdminId == l.AdminId).Select(a => a.FirstName + " " + a.LastName).FirstOrDefault() ?? "Unknown",
				Action = l.Action,
				ActorUserId = l.ActorUserId,
				ActorName = l.ActorName,
				Timestamp = l.Timestamp,
				OldDataJson = l.OldDataJson,
				NewDataJson = l.NewDataJson,
				Notes = l.Notes
			});

			var query = clinicLogs.Union(doctorLogs).Union(adminLogs);

			if (!string.IsNullOrEmpty(entityType))
			{
				query = query.Where(q => q.EntityType == entityType);
			}

			if (!string.IsNullOrEmpty(action))
			{
				query = query.Where(q => q.Action == action);
			}

			if (startDate.HasValue)
			{
				query = query.Where(q => q.Timestamp >= startDate.Value);
			}

			if (endDate.HasValue)
			{
				query = query.Where(q => q.Timestamp <= endDate.Value);
			}

			var totalCount = await query.CountAsync();

			var items = await query.OrderByDescending(q => q.Timestamp)
				.Skip((page - 1) * size)
				.Take(size)
				.ToListAsync();

			return new PagedResult<SystemAuditLogDto>(items, totalCount, page, size);
		}

		public async Task<IEnumerable<ClinicBasicInfoDto>> AssignAdminToClinicsAsync(Guid adminId, IEnumerable<Guid> clinicIds)
		{
			var clinicIdList = clinicIds?.ToList() ?? new List<Guid>();

			var admin = await _dbContext.Admins
				.Include(a => a.AdminClinics)
					.ThenInclude(ac => ac.Clinic)
				.FirstOrDefaultAsync(a => a.AdminId == adminId);

			if (admin == null)
				throw new NotFoundException($"Admin with ID '{adminId}' was not found.");

			// Remove assignments that are no longer in the requested set
			var toRemove = admin.AdminClinics
				.Where(ac => !clinicIdList.Contains(ac.ClinicId))
				.ToList();
			_dbContext.AdminClinics.RemoveRange(toRemove);

			// Add new assignments
			foreach (var clinicId in clinicIdList)
			{
				// Already linked – skip
				if (admin.AdminClinics.Any(ac => ac.ClinicId == clinicId))
					continue;

				// Verify the clinic exists
				var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(c => c.ClinicId == clinicId);
				if (clinic == null)
					throw new NotFoundException($"Clinic with ID '{clinicId}' was not found.");

				// Check if another admin already owns this clinic
				var existingLink = await _dbContext.AdminClinics
					.FirstOrDefaultAsync(ac => ac.ClinicId == clinicId && ac.AdminId != adminId);
				if (existingLink != null)
					throw new InvalidOperationException($"Clinic '{clinic.ClinicName}' is already assigned to another admin.");

				_dbContext.AdminClinics.Add(new AdminClinic
				{
					AdminClinicId = Guid.NewGuid(),
					AdminId = adminId,
					ClinicId = clinicId,
					AssignedDate = DateTime.UtcNow
				});
			}

			await _dbContext.SaveChangesAsync();

			return await GetClinicsForAdminAsync(adminId);
		}

		public async Task<IEnumerable<ClinicBasicInfoDto>> GetClinicsForAdminAsync(Guid adminId)
		{
			return await _dbContext.AdminClinics
				.Where(ac => ac.AdminId == adminId)
				.Select(ac => new ClinicBasicInfoDto
				{
					ClinicId = ac.Clinic.ClinicId,
					ClinicName = ac.Clinic.ClinicName
				})
				.ToListAsync();
		}
	}
}
