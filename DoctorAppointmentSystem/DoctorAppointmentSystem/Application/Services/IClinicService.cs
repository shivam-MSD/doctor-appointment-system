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
		Task VerifyClinicAsync(Guid clinicId);
		Task VerifyAdminAsync(Guid adminId);
		Task RejectClinicAsync(Guid clinicId, string rejectionReason);
		Task UpdateClinicAsync(Guid clinicId, Guid doctorUserId, UpdateClinicDto dto);
	}
}
