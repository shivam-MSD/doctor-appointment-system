using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class PatientService : IPatientService
	{
		private readonly ApplicationDbContext _dbContext;

		public PatientService(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<PatientDto> GetPatientProfileAsync(Guid userId, Guid patientId)
		{
			// 1. Verify that this User has access to this Patient profile
			var isLinked = await _dbContext.UserPatients.AnyAsync(up => up.UserId == userId && up.PatientId == patientId);
			if (!isLinked)
			{
				throw new ForbiddenException("You do not have permission to access this patient profile.");
			}

			// 2. Fetch the Patient profile
			var patient = await _dbContext.Patients.FindAsync(patientId);
			if (patient == null)
			{
				throw new NotFoundException($"Patient profile with ID '{patientId}' was not found.");
			}

			return MapToDto(userId, patient);
		}

		public async Task<PatientDto> UpdatePatientProfileAsync(Guid userId, Guid patientId, PatientUpdateDto dto)
		{
			// 1. Verify access
			var isLinked = await _dbContext.UserPatients.AnyAsync(up => up.UserId == userId && up.PatientId == patientId);
			if (!isLinked)
			{
				throw new ForbiddenException("You do not have permission to modify this patient profile.");
			}

			// 2. Fetch profile
			var patient = await _dbContext.Patients.FindAsync(patientId);
			if (patient == null)
			{
				throw new NotFoundException($"Patient profile with ID '{patientId}' was not found.");
			}

			// 3. Parse blood group if provided
			if (!string.IsNullOrEmpty(dto.BloodGroup))
			{
				if (Enum.TryParse<EBloodGroup>(dto.BloodGroup, true, out var bloodGroup))
				{
					patient.BloodGroup = bloodGroup;
				}
				else
				{
					throw new BadRequestException($"BloodGroup '{dto.BloodGroup}' is invalid.");
				}
			}

			// 4. Update demographics properties
			patient.FirstName = dto.FirstName;
			patient.LastName = dto.LastName;
			patient.MobileNo = dto.MobileNo;
			patient.Gender = dto.Gender;
			patient.DOB = dto.DOB;
			patient.EmergencyConactName = dto.EmergencyContactName;
			patient.EmergencyConactNumber = dto.EmergencyContactNumber;
			patient.UpdatedDate = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync();

			return MapToDto(userId, patient);
		}

		private PatientDto MapToDto(Guid userId, Patient patient)
		{
			return new PatientDto
			{
				PatientId = patient.PatientId,
				UserId = userId,
				Email = string.Empty, // Shared profile demographics
				BloodGroup = patient.BloodGroup.ToString(),
				EmergencyContactName = patient.EmergencyConactName,
				EmergencyContactNumber = patient.EmergencyConactNumber
			};
		}
	}
}
