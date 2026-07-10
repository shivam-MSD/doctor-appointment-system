using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IClinicService
	{
		Task RegisterClinicAndAdminAsync(Guid doctorUserId, RegisterClinicDto dto);
		Task RegisterClinicAsync(Guid doctorUserId, CreateClinicDto dto);
		Task RegisterAdminForClinicAsync(Guid doctorUserId, RegisterAdminForClinicDto dto);
		Task<IEnumerable<ClinicDto>> GetDoctorClinicsAsync(Guid doctorUserId);
		Task<IEnumerable<ClinicAdminDto>> GetDoctorAdminsAsync(Guid doctorUserId);
		Task<IEnumerable<ClinicDto>> GetPendingClinicsAsync();
		Task<IEnumerable<ClinicAdminDto>> GetPendingAdminsAsync();
		Task<string> VerifyClinicAsync(Guid clinicId);
		Task<string> VerifyAdminAsync(Guid adminId);
		Task<string> RejectAdminAsync(Guid adminId);
		Task<string> RejectClinicAsync(Guid clinicId, string rejectionReason);
		Task UpdateClinicAsync(Guid clinicId, Guid doctorUserId, UpdateClinicDto dto);
		Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId);
		Task AdminUpdateClinicAsync(Guid adminUserId, UpdateClinicDto dto);
		Task<ClinicDto> GetAdminClinicAsync(Guid adminUserId);
		Task<IEnumerable<ClinicAuditLogDto>> GetClinicHistoryAsync(Guid clinicId);
	}
}
