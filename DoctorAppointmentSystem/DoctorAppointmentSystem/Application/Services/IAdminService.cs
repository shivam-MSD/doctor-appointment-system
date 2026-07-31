using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Service interface defining Super Admin operations: doctor onboarding verification, system-wide audits, and multi-clinic admin assignments.
	/// </summary>
	public interface IAdminService
	{
		/// <summary>Verifies or rejects a doctor onboarding application, generating login credentials upon approval.</summary>
		Task<string> VerifyDoctorAsync(Guid doctorId, string status, string? rejectionReason = null);

		/// <summary>Retrieves all pending doctor onboarding applications awaiting Super Admin review.</summary>
		Task<IEnumerable<DoctorDto>> GetPendingDoctorsAsync();

		/// <summary>Retrieves all registered doctors in the system with optional search and date filters.</summary>
		Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync(string? search, string? status, DateTime? registerDate, DateTime? approveDate);

		/// <summary>Retrieves all clinic branches in the network with location and verification status filters.</summary>
		Task<IEnumerable<ClinicDto>> GetAllClinicsAsync(string? search, string? state, string? city, bool? isVerified);

		/// <summary>Retrieves all clinic administrators in the network.</summary>
		Task<IEnumerable<ClinicAdminDto>> GetAllAdminsAsync(string? search, bool? isVerified);

		/// <summary>Retrieves system-wide audit logs with entity, action, date, and pagination filters.</summary>
		Task<PagedResult<SystemAuditLogDto>> GetSystemAuditLogsAsync(string? entityType, string? action, DateTime? startDate, DateTime? endDate, int page, int size);

		/// <summary>Assigns a Clinic Administrator to manage one or multiple clinic branches.</summary>
		Task<IEnumerable<ClinicBasicInfoDto>> AssignAdminToClinicsAsync(Guid adminId, IEnumerable<Guid> clinicIds);

		/// <summary>Retrieves all clinic branches currently assigned to a Clinic Administrator.</summary>
		Task<IEnumerable<ClinicBasicInfoDto>> GetClinicsForAdminAsync(Guid adminId);
	}
}
