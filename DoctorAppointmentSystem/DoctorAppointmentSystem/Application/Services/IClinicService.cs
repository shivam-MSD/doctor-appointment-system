using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Service interface defining clinic branch registration, administrator assignment, verification workflows, and operating hours management.
	/// </summary>
	public interface IClinicService
	{
		/// <summary>Registers a new clinic branch along with a dedicated clinic administrator account.</summary>
		Task RegisterClinicAndAdminAsync(Guid doctorUserId, RegisterClinicDto dto);

		/// <summary>Registers a new clinic branch for a doctor.</summary>
		Task RegisterClinicAsync(Guid doctorUserId, CreateClinicDto dto);

		/// <summary>Registers a new clinic administrator assigned to an existing clinic branch.</summary>
		Task RegisterAdminForClinicAsync(Guid doctorUserId, RegisterAdminForClinicDto dto);

		/// <summary>Retrieves all clinic branches belonging to a doctor.</summary>
		Task<IEnumerable<ClinicDto>> GetDoctorClinicsAsync(Guid doctorUserId);

		/// <summary>Retrieves all clinic administrators assigned to a doctor's clinics.</summary>
		Task<IEnumerable<ClinicAdminDto>> GetDoctorAdminsAsync(Guid doctorUserId);

		/// <summary>Retrieves all pending clinic branch registration requests for Super Admin review.</summary>
		Task<IEnumerable<ClinicDto>> GetPendingClinicsAsync();

		/// <summary>Retrieves all pending clinic administrator registration requests for Super Admin review.</summary>
		Task<IEnumerable<ClinicAdminDto>> GetPendingAdminsAsync();

		/// <summary>Approves and verifies a clinic branch registration.</summary>
		Task<string> VerifyClinicAsync(Guid clinicId);

		/// <summary>Approves and verifies a clinic administrator account, generating temporary credentials.</summary>
		Task<string> VerifyAdminAsync(Guid adminId);

		/// <summary>Rejects a clinic administrator registration request with a reason.</summary>
		Task<string> RejectAdminAsync(Guid adminId, string rejectionReason);

		/// <summary>Rejects a clinic branch registration request with a reason.</summary>
		Task<string> RejectClinicAsync(Guid clinicId, string rejectionReason);

		/// <summary>Updates clinic operating hours, daily capacity, and location details.</summary>
		Task UpdateClinicAsync(Guid clinicId, Guid doctorUserId, UpdateClinicDto dto);

		/// <summary>Retrieves verified clinic branches for a specific doctor ID.</summary>
		Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId);

		/// <summary>Allows a Clinic Administrator to update their managed clinic branch details.</summary>
		Task AdminUpdateClinicAsync(Guid adminUserId, UpdateClinicDto dto);

		/// <summary>Retrieves clinic branch details managed by a specific Clinic Administrator.</summary>
		Task<ClinicDto> GetAdminClinicAsync(Guid adminUserId);

		/// <summary>Retrieves audit log history entries for a specific clinic branch.</summary>
		Task<IEnumerable<ClinicAuditLogDto>> GetClinicHistoryAsync(Guid clinicId);
	}
}
