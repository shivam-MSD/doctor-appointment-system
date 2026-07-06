using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class AdminService : IAdminService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly INotificationService _notificationService;

		public AdminService(ApplicationDbContext dbContext, INotificationService notificationService)
		{
			_dbContext = dbContext;
			_notificationService = notificationService;
		}

		public async Task VerifyDoctorAsync(Guid doctorId, string status)
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

			doctor.VerificationStatus = parsedStatus;
			doctor.UpdatedDate = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync();

			await _notificationService.CreateNotificationAsync(doctor.User.UserId, $"Your doctor profile verification status has been updated to: {status}.");
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
					HospitalName = d.HospitalName,
					YearsOfExperience = d.YearsOfExperience,
					ConsultationFee = d.ConsultationFee,
					AboutDoctor = d.AboutDoctor ?? string.Empty,
					VerificationStatus = d.VerificationStatus.ToString(),
					CreatedDate = d.CreatedDate,
					UpdatedDate = d.UpdatedDate
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
					d.HospitalName.ToLower().Contains(searchLower) ||
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
					HospitalName = d.HospitalName,
					YearsOfExperience = d.YearsOfExperience,
					ConsultationFee = d.ConsultationFee,
					AboutDoctor = d.AboutDoctor ?? string.Empty,
					VerificationStatus = d.VerificationStatus.ToString(),
					CreatedDate = d.CreatedDate,
					UpdatedDate = d.UpdatedDate
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicDto>> GetAllClinicsAsync(string? search, string? state, string? city, bool? isVerified)
		{
			var query = _dbContext.Clinics
				.Include(c => c.Doctor)
				.Include(c => c.Address)
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
				.OrderByDescending(c => c.CreatedDate)
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
					IsVerified = c.VerificationStatus == EVerificationStatus.Verified,
					VerificationStatus = c.VerificationStatus.ToString(),
					RejectionReason = c.RejectionReason,
					HasAdmin = _dbContext.Admins.Any(a => a.Clinic.ClinicId == c.ClinicId)
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<ClinicAdminDto>> GetAllAdminsAsync(string? search, bool? isVerified)
		{
			var query = _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.AsQueryable();

			if (!string.IsNullOrEmpty(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(a => 
					a.FirstName.ToLower().Contains(searchLower) ||
					a.LastName.ToLower().Contains(searchLower) ||
					a.Clinic.ClinicName.ToLower().Contains(searchLower) ||
					a.MobileNo.Contains(searchLower)
				);
			}

			if (isVerified.HasValue)
			{
				query = query.Where(a => a.IsVerified == isVerified.Value);
			}

			return await query
				.OrderByDescending(a => a.CreatedDate)
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
	}
}
