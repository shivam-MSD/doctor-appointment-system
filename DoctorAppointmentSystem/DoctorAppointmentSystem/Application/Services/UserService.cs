using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class UserService : IUserService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly INotificationService _notificationService;

		public UserService(ApplicationDbContext dbContext, INotificationService notificationService)
		{
			_dbContext = dbContext;
			_notificationService = notificationService;
		}

		public async Task<UserDto> GetUserProfileAsync(Guid userId)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException($"User with ID '{userId}' was not found.");
			}

			return new UserDto
			{
				UserId = user.UserId,
				Email = user.Email,
				IsActive = user.IsActive
			};
		}

		public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException($"User with ID '{userId}' was not found.");
			}

			var currentHashed = HashPassword(dto.CurrentPassword);
			if (user.PasswordHash != currentHashed)
			{
				throw new BadRequestException("The current password entered is incorrect.");
			}

			user.PasswordHash = HashPassword(dto.NewPassword);
			await _dbContext.SaveChangesAsync();
		}

		public async Task<DoctorProfileDto> GetDoctorProfileAsync(Guid userId)
		{
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.Include(d => d.Specialization)
				.FirstOrDefaultAsync(d => d.User.UserId == userId);
			if (doctor == null)
			{
				throw new NotFoundException("Doctor profile was not found.");
			}

			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);

			return new DoctorProfileDto
			{
				FirstName = doctor.FirstName,
				LastName = doctor.LastName,
				MobileNo = doctor.MobileNo,
				Qualification = doctor.Qualification,
				LicenceNumber = doctor.LicenceNumber,
				HospitalName = doctor.HospitalName,
				YearsOfExperience = doctor.YearsOfExperience,
				ConsultationFee = doctor.ConsultationFee,
				AboutDoctor = doctor.AboutDoctor ?? string.Empty,
				SpecializationId = doctor.Specialization?.SpecializationId,
				Country = address?.Country ?? "India",
				State = address?.State ?? string.Empty,
				City = address?.City ?? string.Empty,
				Area = address?.Area ?? string.Empty,
				Pincode = address?.Pincode ?? string.Empty,
				Addressline1 = address?.Addressline1 ?? string.Empty,
				Addressline2 = address?.Addressline2
			};
		}

		public async Task<DoctorProfileDto> UpdateDoctorProfileAsync(Guid userId, DoctorProfileDto dto)
		{
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.Include(d => d.Specialization)
				.FirstOrDefaultAsync(d => d.User.UserId == userId);
			if (doctor == null)
			{
				throw new NotFoundException("Doctor profile was not found.");
			}

			if (dto.SpecializationId.HasValue && dto.SpecializationId.Value != Guid.Empty)
			{
				var spec = await _dbContext.Specializations.FindAsync(dto.SpecializationId.Value);
				if (spec != null)
				{
					doctor.Specialization = spec;
				}
			}

			doctor.FirstName = dto.FirstName;
			doctor.LastName = dto.LastName;
			doctor.MobileNo = dto.MobileNo;
			doctor.Qualification = dto.Qualification;
			doctor.LicenceNumber = dto.LicenceNumber;
			doctor.HospitalName = dto.HospitalName;
			doctor.YearsOfExperience = dto.YearsOfExperience;
			doctor.ConsultationFee = dto.ConsultationFee;
			doctor.AboutDoctor = dto.AboutDoctor;
			doctor.UpdatedDate = DateTime.UtcNow;

			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);
			if (address == null)
			{
				address = new Address
				{
					AddressId = Guid.NewGuid(),
					User = doctor.User,
					Country = dto.Country,
					State = dto.State,
					City = dto.City,
					Area = dto.Area,
					Pincode = dto.Pincode,
					Addressline1 = dto.Addressline1,
					Addressline2 = dto.Addressline2 ?? string.Empty
				};
				_dbContext.Addresses.Add(address);
			}
			else
			{
				address.Country = dto.Country;
				address.State = dto.State;
				address.City = dto.City;
				address.Area = dto.Area;
				address.Pincode = dto.Pincode;
				address.Addressline1 = dto.Addressline1;
				address.Addressline2 = dto.Addressline2 ?? string.Empty;
			}

			await _dbContext.SaveChangesAsync();
			await _notificationService.SendRefreshSignalAsync("Doctors");
			return dto;
		}

		public async Task<AdminProfileDto> GetAdminProfileAsync(Guid userId)
		{
			var adminObj = await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.FirstOrDefaultAsync(a => a.User.UserId == userId);
			if (adminObj == null)
			{
				throw new NotFoundException("Clinic Admin profile was not found.");
			}

			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);

			return new AdminProfileDto
			{
				FirstName = adminObj.FirstName,
				LastName = adminObj.LastName,
				MobileNo = adminObj.MobileNo,
				ClinicName = adminObj.Clinic != null ? adminObj.Clinic.ClinicName : "N/A",
				Country = address?.Country ?? "India",
				State = address?.State ?? string.Empty,
				City = address?.City ?? string.Empty,
				Area = address?.Area ?? string.Empty,
				Pincode = address?.Pincode ?? string.Empty,
				Addressline1 = address?.Addressline1 ?? string.Empty,
				Addressline2 = address?.Addressline2
			};
		}

		public async Task<AdminProfileDto> UpdateAdminProfileAsync(Guid userId, AdminProfileDto dto)
		{
			var adminObj = await _dbContext.Admins
				.Include(a => a.User)
				.Include(a => a.Clinic)
				.FirstOrDefaultAsync(a => a.User.UserId == userId);
			if (adminObj == null)
			{
				throw new NotFoundException("Clinic Admin profile was not found.");
			}

			adminObj.FirstName = dto.FirstName;
			adminObj.LastName = dto.LastName;
			adminObj.MobileNo = dto.MobileNo;
			adminObj.CreatedDate = DateTime.UtcNow; // Touch update

			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);
			if (address == null)
			{
				address = new Address
				{
					AddressId = Guid.NewGuid(),
					User = adminObj.User,
					Country = dto.Country,
					State = dto.State,
					City = dto.City,
					Area = dto.Area,
					Pincode = dto.Pincode,
					Addressline1 = dto.Addressline1,
					Addressline2 = dto.Addressline2 ?? string.Empty
				};
				_dbContext.Addresses.Add(address);
			}
			else
			{
				address.Country = dto.Country;
				address.State = dto.State;
				address.City = dto.City;
				address.Area = dto.Area;
				address.Pincode = dto.Pincode;
				address.Addressline1 = dto.Addressline1;
				address.Addressline2 = dto.Addressline2 ?? string.Empty;
			}

			await _dbContext.SaveChangesAsync();
			dto.ClinicName = adminObj.Clinic != null ? adminObj.Clinic.ClinicName : "N/A";
			return dto;
		}

		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}
	}
}
