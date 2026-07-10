using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IAdminService
	{
		Task<string> VerifyDoctorAsync(Guid doctorId, string status);
		Task<IEnumerable<DoctorDto>> GetPendingDoctorsAsync();
		Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync(string? search, string? status, DateTime? registerDate, DateTime? approveDate);
		Task<IEnumerable<ClinicDto>> GetAllClinicsAsync(string? search, string? state, string? city, bool? isVerified);
		Task<IEnumerable<ClinicAdminDto>> GetAllAdminsAsync(string? search, bool? isVerified);
	}
}
