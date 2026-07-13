using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DoctorAppointmentSystem.Application.Services
{
	public class AppointmentService : IAppointmentService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly INotificationService _notificationService;
		private readonly IDistributedCache _distributedCache;

		public AppointmentService(
			ApplicationDbContext dbContext,
			INotificationService notificationService,
			IDistributedCache distributedCache)
		{
			_dbContext = dbContext;
			_notificationService = notificationService;
			_distributedCache = distributedCache;
		}

		public async Task<AppointmentDto> BookAppointmentAsync(Guid userId, CreateAppointmentDto dto)
		{
			// 1. Verify ConsultationType parses correctly
			if (!Enum.TryParse<EConsultationType>(dto.ConsultationType, true, out var consultationType))
			{
				throw new BadRequestException($"ConsultationType '{dto.ConsultationType}' is invalid. Allowed: InPerson, VideoConsultation.");
			}

			// 2. Validate patient exists
			var patient = await _dbContext.Patients.FindAsync(dto.PatientId);
			if (patient == null)
			{
				throw new NotFoundException($"Patient with ID '{dto.PatientId}' was not found.");
			}

			// 3. Verify user has ownership/access to this patient profile
			var isLinked = await _dbContext.UserPatients.AnyAsync(up => up.UserId == userId && up.PatientId == dto.PatientId);
			if (!isLinked)
			{
				throw new ForbiddenException("You do not have permission to book appointments for this patient profile.");
			}

			// 4. Validate doctor exists and is verified
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.FirstOrDefaultAsync(d => d.DoctorId == dto.DoctorId);
			if (doctor == null)
			{
				throw new NotFoundException($"Doctor with ID '{dto.DoctorId}' was not found.");
			}

			if (doctor.VerificationStatus != EVerificationStatus.Verified)
			{
				throw new BadRequestException($"Doctor '{doctor.FirstName} {doctor.LastName}' is not verified and cannot accept appointments.");
			}

			// 5. Ensure StartTime is before EndTime
			if (dto.StartTime >= dto.EndTime)
			{
				throw new BadRequestException("Start time must be strictly before end time.");
			}

			// 6. Double-booking check: No overlap for the Doctor
			var doctorOverlap = await _dbContext.Appointments
				.Where(app => app.EAppointmentStatus != EAppointmentStatus.Cancelled)
				.Where(app => EF.Property<Guid>(app, "DoctorId") == dto.DoctorId)
				.AnyAsync(app => dto.StartTime < app.EndTime && dto.EndTime > app.StartTime);

			if (doctorOverlap)
			{
				throw new ConflictException("The doctor already has another appointment booked during this time interval.");
			}

			// 7. Double-booking check: No overlap for the Patient
			var patientOverlap = await _dbContext.Appointments
				.Where(app => app.EAppointmentStatus != EAppointmentStatus.Cancelled)
				.Where(app => EF.Property<Guid>(app, "PatientId") == dto.PatientId)
				.AnyAsync(app => dto.StartTime < app.EndTime && dto.EndTime > app.StartTime);

			if (patientOverlap)
			{
				throw new ConflictException("This patient already has another appointment booked during this time interval.");
			}

			Clinic? clinic = null;
			if (dto.ClinicId.HasValue && dto.ClinicId.Value != Guid.Empty)
			{
				clinic = await _dbContext.Clinics.FindAsync(dto.ClinicId.Value);
			}

			if (dto.AppointmentDate.Date < DateTime.Today)
			{
				throw new BadRequestException("Appointment date cannot be in the past.");
			}

			if (clinic != null && clinic.BookingWindowEndDate.HasValue && dto.AppointmentDate.Date > clinic.BookingWindowEndDate.Value.Date)
			{
				throw new BadRequestException($"Appointments at this branch are only open until {clinic.BookingWindowEndDate.Value:yyyy-MM-dd}. Please choose an earlier date.");
			}

			// 8. Create the appointment
			var appointment = new Appointment
			{
				AppointmentId = Guid.NewGuid(),
				AppointmentDate = dto.AppointmentDate.Date,
				StartTime = dto.StartTime,
				EndTime = dto.EndTime,
				Reason = dto.Reason,
				EConsultationType = consultationType,
				EAppointmentStatus = EAppointmentStatus.Pending,
				CreatedDate = DateTime.UtcNow,
				Clinic = clinic
			};

			_dbContext.Appointments.Add(appointment);

			// Map shadow properties
			_dbContext.Entry(appointment).Property("PatientId").CurrentValue = dto.PatientId;
			_dbContext.Entry(appointment).Property("DoctorId").CurrentValue = dto.DoctorId;

			await _dbContext.SaveChangesAsync();

			// Trigger notifications
			var dateStr = appointment.AppointmentDate.ToString("yyyy-MM-dd");
			var startStr = appointment.StartTime.ToString("HH:mm");
			var msg = $"New appointment booked by Patient {patient.FirstName} {patient.LastName} for {dateStr} at {startStr}.";

			// Notify Doctor
			await _notificationService.CreateNotificationAsync(doctor.User.UserId, msg);

			// Notify Clinic Admin
			if (clinic != null)
			{
				var admin = await _dbContext.Admins
					.Include(a => a.User)
					.FirstOrDefaultAsync(a => a.Clinic.ClinicId == clinic.ClinicId);
				if (admin != null)
				{
					await _notificationService.CreateNotificationAsync(admin.User.UserId, msg);
				}
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");

			return MapToDto(appointment, patient, doctor);
		}

		public async Task CancelAppointmentAsync(Guid userId, Guid appointmentId)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Clinic)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
			{
				throw new NotFoundException($"Appointment with ID '{appointmentId}' was not found.");
			}

			// Resolve shadow PatientId
			var patientIdObj = _dbContext.Entry(appointment).Property("PatientId").CurrentValue;
			if (patientIdObj == null || !(patientIdObj is Guid patientId))
			{
				throw new BaseException("Appointment patient link configuration error.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			// Verify user owns the patient profile
			var isLinked = await _dbContext.UserPatients.AnyAsync(up => up.UserId == userId && up.PatientId == patientId);
			if (!isLinked)
			{
				throw new ForbiddenException("You do not have permission to cancel appointments for this patient profile.");
			}

			appointment.EAppointmentStatus = EAppointmentStatus.Cancelled;
			await _dbContext.SaveChangesAsync();

			// Trigger cancellation notifications
			var doctorIdObj = _dbContext.Entry(appointment).Property("DoctorId").CurrentValue;
			if (doctorIdObj is Guid doctorId)
			{
				var doctor = await _dbContext.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == doctorId);
				if (doctor != null)
				{
					var patientName = await _dbContext.Patients.Where(p => p.PatientId == patientId).Select(p => p.FirstName + " " + p.LastName).FirstOrDefaultAsync() ?? "Patient";
					var msg = $"Appointment with Patient {patientName} on {appointment.AppointmentDate.ToString("yyyy-MM-dd")} has been cancelled.";
					
					// Notify Doctor
					await _notificationService.CreateNotificationAsync(doctor.User.UserId, msg);

					// Notify Clinic Admin
					if (appointment.Clinic != null)
					{
						var admin = await _dbContext.Admins.Include(a => a.User).FirstOrDefaultAsync(a => a.Clinic.ClinicId == appointment.Clinic.ClinicId);
						if (admin != null)
						{
							await _notificationService.CreateNotificationAsync(admin.User.UserId, msg);
						}
					}
					await _notificationService.SendRefreshSignalAsync("Appointments");
				}
			}
		}

		public async Task<PagedResult<AppointmentDto>> GetAdminDoctorDashboardAppointmentsAsync(Guid userId, string? status, DateTime? startDate, DateTime? endDate, string? search, Guid? patientId, int page, int size)
		{
			var query = _dbContext.Appointments
				.Include(app => app.Patient)
				.Include(app => app.Doctor)
				.Include(app => app.Clinic)
				.AsQueryable();

			if (patientId.HasValue)
			{
				query = query.Where(app => EF.Property<Guid>(app, "PatientId") == patientId.Value);
			}

			// Resolve User Role dashboard scope
			var isDoctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == userId);
			if (isDoctor != null)
			{
				query = query.Where(app => app.Doctor.DoctorId == isDoctor.DoctorId);
			}
			else
			{
				var isAdmin = await _dbContext.Admins
					.Include(a => a.Clinic)
					.FirstOrDefaultAsync(a => a.User.UserId == userId);
				if (isAdmin != null)
				{
					query = query.Where(app => app.Clinic != null && app.Clinic.ClinicId == isAdmin.Clinic.ClinicId);
				}
			}

			// Filter by status
			if (!string.IsNullOrEmpty(status))
			{
				if (Enum.TryParse<EAppointmentStatus>(status, true, out var parsedStatus))
				{
					query = query.Where(app => app.EAppointmentStatus == parsedStatus);
				}
			}

			// Filter by date range
			if (startDate.HasValue)
			{
				query = query.Where(app => app.StartTime >= startDate.Value);
			}

			if (endDate.HasValue)
			{
				query = query.Where(app => app.EndTime <= endDate.Value);
			}

			// Filter by search (names)
			if (!string.IsNullOrEmpty(search))
			{
				var searchLower = search.Trim().ToLower();
				query = query.Where(app =>
					app.Patient.FirstName.ToLower().Contains(searchLower) ||
					app.Patient.LastName.ToLower().Contains(searchLower) ||
					(app.Patient.FirstName + " " + app.Patient.LastName).ToLower().Contains(searchLower) ||
					app.Doctor.FirstName.ToLower().Contains(searchLower) ||
					app.Doctor.LastName.ToLower().Contains(searchLower) ||
					(app.Doctor.FirstName + " " + app.Doctor.LastName).ToLower().Contains(searchLower)
				);
			}

			var totalCount = await query.CountAsync();
			var items = await query
				.OrderByDescending(app => app.StartTime)
				.Skip((page - 1) * size)
				.Take(size)
				.ToListAsync();

			var dtos = items.Select(app => MapToDto(app, app.Patient, app.Doctor));
			return new PagedResult<AppointmentDto>(dtos, totalCount, page, size);
		}

		public async Task<PagedResult<PatientDto>> GetDashboardPatientsAsync(Guid userId, string? search, int page, int size)
		{
			// 1. Identify if Doctor
			var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == userId);
			// 2. Identify if Clinic Admin
			var adminObj = await _dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == userId);

			List<Guid> patientIds = new List<Guid>();

			if (doctor != null)
			{
				var clinicIds = await _dbContext.Clinics
					.Where(c => c.Doctor.DoctorId == doctor.DoctorId && c.ParentClinicId == null)
					.Select(c => c.ClinicId)
					.ToListAsync();

				patientIds = await _dbContext.Appointments
					.Where(a => a.Clinic != null && clinicIds.Contains(a.Clinic.ClinicId))
					.Select(a => a.Patient.PatientId)
					.Distinct()
					.ToListAsync();
			}
			else if (adminObj != null && adminObj.Clinic != null)
			{
				patientIds = await _dbContext.Appointments
					.Where(a => a.Clinic != null && a.Clinic.ClinicId == adminObj.Clinic.ClinicId)
					.Select(a => a.Patient.PatientId)
					.Distinct()
					.ToListAsync();
			}
			else
			{
				// Super Admin, Patients, and Unassigned users cannot list other patients.
				return new PagedResult<PatientDto>(new List<PatientDto>(), 0, page, size);
			}

			var query = _dbContext.Patients
				.Where(p => patientIds.Contains(p.PatientId));

			if (!string.IsNullOrEmpty(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(p =>
					p.FirstName.ToLower().Contains(searchLower) ||
					p.LastName.ToLower().Contains(searchLower) ||
					p.MobileNo.Contains(searchLower)
				);
			}

			var totalCount = await query.CountAsync();
			var items = await query
				.OrderBy(p => p.LastName)
				.Skip((page - 1) * size)
				.Take(size)
				.ToListAsync();

			var patientIdsList = items.Select(p => p.PatientId).ToList();
			var userPatients = await _dbContext.UserPatients
				.Where(up => patientIdsList.Contains(up.PatientId))
				.ToListAsync();

			var userIds = userPatients.Select(up => up.UserId).ToList();
			var addresses = await _dbContext.Addresses
				.Where(a => a.User != null && userIds.Contains(a.User.UserId))
				.Include(a => a.User)
				.ToListAsync();

			var dtos = items.Select(p => {
				var up = userPatients.FirstOrDefault(u => u.PatientId == p.PatientId);
				var address = up != null ? addresses.FirstOrDefault(a => a.User.UserId == up.UserId) : null;
				return new PatientDto
				{
					PatientId = p.PatientId,
					UserId = up?.UserId ?? Guid.Empty,
					Email = string.Empty,
					FirstName = p.FirstName,
					LastName = p.LastName,
					MobileNo = p.MobileNo,
					Gender = p.Gender.ToString(),
					DOB = p.DOB,
					BloodGroup = p.BloodGroup.ToString(),
					EmergencyContactName = p.EmergencyConactName,
					EmergencyContactNumber = p.EmergencyConactNumber,
					Country = address?.Country ?? "India",
					State = address?.State ?? string.Empty,
					City = address?.City ?? string.Empty,
					Area = address?.Area ?? string.Empty,
					Pincode = address?.Pincode ?? string.Empty,
					Addressline1 = address?.Addressline1 ?? string.Empty,
					Addressline2 = address?.Addressline2
				};
			}).ToList();

			return new PagedResult<PatientDto>(dtos, totalCount, page, size);
		}

		public async Task<PagedResult<AppointmentDto>> GetPatientDashboardAppointmentsAsync(Guid userId, string? status, int page, int size)
		{
			// Get all patient IDs linked to this User
			var linkedPatientIds = await _dbContext.UserPatients
				.Where(up => up.UserId == userId && up.IsVerified)
				.Select(up => up.PatientId)
				.ToListAsync();

			var query = _dbContext.Appointments
				.Include(app => app.Patient)
				.Include(app => app.Doctor)
				.Include(app => app.Clinic)
				.Where(app => linkedPatientIds.Contains(EF.Property<Guid>(app, "PatientId")))
				.AsQueryable();

			if (!string.IsNullOrEmpty(status))
			{
				if (Enum.TryParse<EAppointmentStatus>(status, true, out var parsedStatus))
				{
					query = query.Where(app => app.EAppointmentStatus == parsedStatus);
				}
			}

			var totalCount = await query.CountAsync();
			var items = await query
				.OrderByDescending(app => app.StartTime) // latest to oldest
				.Skip((page - 1) * size)
				.Take(size)
				.ToListAsync();

			var dtos = items.Select(app => MapToDto(app, app.Patient, app.Doctor));
			return new PagedResult<AppointmentDto>(dtos, totalCount, page, size);
		}

		public async Task<IEnumerable<ConsultedDoctorDto>> GetConsultedDoctorsAsync(Guid userId)
		{
			// Get all patient IDs linked to this User
			var linkedPatientIds = await _dbContext.UserPatients
				.Where(up => up.UserId == userId && up.IsVerified)
				.Select(up => up.PatientId)
				.ToListAsync();

			// Fetch all appointments for these patients
			var appointments = await _dbContext.Appointments
				.Include(app => app.Patient)
				.Include(app => app.Doctor)
				.ThenInclude(d => d.Specialization)
				.Where(app => linkedPatientIds.Contains(EF.Property<Guid>(app, "PatientId")))
				.ToListAsync();

			// Group appointments by Doctor
			var groups = appointments.GroupBy(app => EF.Property<Guid>(app, "DoctorId"));

			var consultedDoctors = new List<ConsultedDoctorDto>();

			foreach (var group in groups)
			{
				var firstApp = group.First();
				var doctor = firstApp.Doctor;

				consultedDoctors.Add(new ConsultedDoctorDto
				{
					DoctorId = doctor.DoctorId,
					DoctorName = $"{doctor.FirstName} {doctor.LastName}",
					Specialization = doctor.Specialization?.SpecializationName ?? "General Physician",
					Appointments = group.Select(app => MapToDto(app, app.Patient, doctor))
				});
			}

			return consultedDoctors;
		}

		public async Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync()
		{
			System.Diagnostics.Debugger.Launch();
			const string cacheKey = "available_doctors_list";
			var cachedData = await _distributedCache.GetStringAsync(cacheKey);

			if (!string.IsNullOrEmpty(cachedData))
			{
				return JsonSerializer.Deserialize<IEnumerable<DoctorDto>>(cachedData) ?? new List<DoctorDto>();
			}

			var doctors = await _dbContext.Doctors
				.Include(d => d.Specialization)
				.Include(d => d.User)
				.Where(d => d.VerificationStatus == EVerificationStatus.Verified)
				.Select(d => new DoctorDto
				{
					DoctorId = d.DoctorId,
					UserId = d.User.UserId,
					Email = d.User.Email,
					SpecializationId = d.Specialization.SpecializationId,
					SpecializationName = d.Specialization.SpecializationName,
					FirstName = d.FirstName,
					LastName = d.LastName,
					MobileNo = d.MobileNo,
					Qualification = d.Qualification,
					LicenceNumber = d.LicenceNumber,
					YearsOfExperience = d.YearsOfExperience,
					ConsultationFee = d.ConsultationFee,
					VerificationStatus = d.VerificationStatus.ToString(),
					AboutDoctor = d.AboutDoctor
				})
				.ToListAsync();

			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Cache for 24 hours since we use eviction
			};

			await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(doctors), cacheOptions);

			return doctors;
		}

		public async Task<IEnumerable<Specialization>> GetSpecializationsAsync()
		{
			const string cacheKey = "specializations_list";
			var cachedData = await _distributedCache.GetStringAsync(cacheKey);

			if (!string.IsNullOrEmpty(cachedData))
			{
				return JsonSerializer.Deserialize<IEnumerable<Specialization>>(cachedData) ?? new List<Specialization>();
			}

			var specializations = await _dbContext.Specializations.OrderBy(s => s.SpecializationName).ToListAsync();

			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Cache for 24 hours
			};

			await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(specializations), cacheOptions);

			return specializations;
		}

		public async Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? state, string? city, Guid? specializationId, string? nameSearch)
		{
			// 1. Start with base query of verified doctors who have at least one verified clinic
			var query = _dbContext.Doctors
				.Include(d => d.Specialization)
				.Include(d => d.User)
				.Where(d => d.VerificationStatus == EVerificationStatus.Verified)
				.Where(d => _dbContext.Clinics.Any(c => c.Doctor.DoctorId == d.DoctorId && c.VerificationStatus == EVerificationStatus.Verified && c.ParentClinicId == null));

			// 2. Filter by location (State & City) if provided
			if (!string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(city))
			{
				var doctorIdsAtLocation = await _dbContext.Clinics
					.Include(c => c.Address)
					.Include(c => c.Doctor)
					.Where(c => c.VerificationStatus == EVerificationStatus.Verified && c.ParentClinicId == null && c.Address.State.ToLower() == state.ToLower() && c.Address.City.ToLower() == city.ToLower())
					.Select(c => c.Doctor.DoctorId)
					.Distinct()
					.ToListAsync();

				query = query.Where(d => doctorIdsAtLocation.Contains(d.DoctorId));
			}

			// 3. Filter by Specialization if provided
			if (specializationId.HasValue && specializationId.Value != Guid.Empty)
			{
				query = query.Where(d => d.Specialization.SpecializationId == specializationId.Value);
			}

			// 4. Apply custom filter (doctor name search) if provided
			if (!string.IsNullOrWhiteSpace(nameSearch))
			{
				var cleanName = nameSearch.Trim().ToLower();
				query = query.Where(d => d.FirstName.ToLower().Contains(cleanName) || d.LastName.ToLower().Contains(cleanName));
			}

			var doctors = await query
				.Select(d => new DoctorDto
				{
					DoctorId = d.DoctorId,
					UserId = d.User.UserId,
					Email = d.User.Email,
					SpecializationId = d.Specialization.SpecializationId,
					SpecializationName = d.Specialization.SpecializationName,
					FirstName = d.FirstName,
					LastName = d.LastName,
					MobileNo = d.MobileNo,
					Qualification = d.Qualification,
					LicenceNumber = d.LicenceNumber,
					YearsOfExperience = d.YearsOfExperience,
					ConsultationFee = d.ConsultationFee,
					VerificationStatus = d.VerificationStatus.ToString(),
					AboutDoctor = d.AboutDoctor
				})
				.ToListAsync();

			return doctors;
		}

		private AppointmentDto MapToDto(Appointment app, Patient patient, Doctor doctor)
		{
			return new AppointmentDto
			{
				AppointmentId = app.AppointmentId,
				PatientId = patient.PatientId,
				PatientName = $"{patient.FirstName} {patient.LastName}",
				DoctorId = doctor.DoctorId,
				DoctorName = $"{doctor.FirstName} {doctor.LastName}",
				ClinicId = app.Clinic?.ClinicId,
				ClinicName = app.Clinic?.ClinicName,
				AppointmentDate = app.AppointmentDate,
				StartTime = app.StartTime,
				EndTime = app.EndTime,
				Status = app.EAppointmentStatus.ToString(),
				Reason = app.Reason,
				ConsultationType = app.EConsultationType.ToString(),
				CreatedDate = app.CreatedDate,
				Comment = app.Comment,
				Report = app.Report,
				RejectionReason = app.RejectionReason
			};
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
					IsVerified = c.VerificationStatus == EVerificationStatus.Verified,
					VerificationStatus = c.VerificationStatus.ToString(),
					RejectionReason = c.RejectionReason,
					Addressline1 = c.Address.Addressline1,
					Addressline2 = c.Address.Addressline2,
					Area = c.Address.Area,
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
					SupportedModes = c.SupportedModes
				})
				.ToListAsync();
		}

		public async Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(Guid doctorId, Guid clinicId, DateTime date, Guid? patientId)
		{
			var targetDate = date.Date;

			var doctorSlotsQuery = _dbContext.Appointments
				.Where(app => app.EAppointmentStatus != EAppointmentStatus.Cancelled)
				.Where(app => EF.Property<Guid>(app, "DoctorId") == doctorId)
				.Where(app => app.Clinic != null && app.Clinic.ClinicId == clinicId)
				.Where(app => app.AppointmentDate == targetDate);

			var doctorSlots = await doctorSlotsQuery
				.Select(app => new BookedSlotDto
				{
					StartTime = app.StartTime.ToString(@"hh\:mm"),
					EndTime = app.EndTime.ToString(@"hh\:mm")
				})
				.ToListAsync();

			if (patientId.HasValue && patientId.Value != Guid.Empty)
			{
				var patientSlots = await _dbContext.Appointments
					.Where(app => app.EAppointmentStatus != EAppointmentStatus.Cancelled)
					.Where(app => EF.Property<Guid>(app, "PatientId") == patientId.Value)
					.Where(app => app.AppointmentDate == targetDate)
					.Select(app => new BookedSlotDto
					{
						StartTime = app.StartTime.ToString(@"hh\:mm"),
						EndTime = app.EndTime.ToString(@"hh\:mm")
					})
					.ToListAsync();

				doctorSlots.AddRange(patientSlots);
			}

			return doctorSlots
				.GroupBy(s => new { s.StartTime, s.EndTime })
				.Select(g => g.First())
				.ToList();
		}

		public async Task<BookingDetailsDto> GetBookingDetailsAsync(Guid doctorId, Guid clinicId)
		{
			// 1. Fetch Doctor
			var doctor = await _dbContext.Doctors
				.Include(d => d.Specialization)
				.Include(d => d.User)
				.FirstOrDefaultAsync(d => d.DoctorId == doctorId);

			if (doctor == null)
			{
				throw new NotFoundException($"Doctor profile with ID '{doctorId}' was not found.");
			}

			if (doctor.VerificationStatus != EVerificationStatus.Verified)
			{
				throw new BadRequestException("The specified doctor profile is pending verification or has been rejected.");
			}

			// 2. Fetch Clinic
			var clinic = await _dbContext.Clinics
				.Include(c => c.Address)
				.Include(c => c.Doctor)
				.FirstOrDefaultAsync(c => c.ClinicId == clinicId);

			if (clinic == null)
			{
				throw new NotFoundException($"Clinic with ID '{clinicId}' was not found.");
			}

			if (clinic.VerificationStatus != EVerificationStatus.Verified)
			{
				throw new BadRequestException("The specified clinic location is pending verification or has been rejected.");
			}

			// 3. CRITICAL SECURITY VALIDATION: Match clinic to doctor!
			if (clinic.Doctor.DoctorId != doctorId)
			{
				throw new BadRequestException("Security Alert: The specified clinic does not belong to this doctor.");
			}

			// 4. Map to DTOs
			var doctorDto = new DoctorDto
			{
				DoctorId = doctor.DoctorId,
				UserId = doctor.User.UserId,
				Email = doctor.User.Email,
				SpecializationId = doctor.Specialization.SpecializationId,
				SpecializationName = doctor.Specialization.SpecializationName,
				FirstName = doctor.FirstName,
				LastName = doctor.LastName,
				MobileNo = doctor.MobileNo,
				Qualification = doctor.Qualification,
				LicenceNumber = doctor.LicenceNumber,
				YearsOfExperience = doctor.YearsOfExperience,
				ConsultationFee = doctor.ConsultationFee,
				VerificationStatus = doctor.VerificationStatus.ToString(),
				AboutDoctor = doctor.AboutDoctor ?? string.Empty
			};

			var clinicDto = new ClinicDto
			{
				ClinicId = clinic.ClinicId,
				ClinicName = clinic.ClinicName,
				ClinicType = clinic.ClinicType,
				DoctorId = clinic.Doctor.DoctorId,
				DoctorName = $"Dr. {clinic.Doctor.FirstName} {clinic.Doctor.LastName}",
				State = clinic.Address.State,
				City = clinic.Address.City,
				Pincode = clinic.Address.Pincode,
				Area = clinic.Address.Area,
				Addressline1 = clinic.Address.Addressline1,
				Addressline2 = clinic.Address.Addressline2,
				IsVerified = clinic.VerificationStatus == EVerificationStatus.Verified,
				VerificationStatus = clinic.VerificationStatus.ToString(),
				RejectionReason = clinic.RejectionReason,
				ParentClinicId = clinic.ParentClinicId,
				OpenDays = clinic.OpenDays,
				StartTime = clinic.StartTime,
				EndTime = clinic.EndTime,
				IsAvailable = clinic.IsAvailable,
				UnavailabilityReason = clinic.UnavailabilityReason,
				IsDoctorAvailable = clinic.IsDoctorAvailable,
				DoctorUnavailabilityReason = clinic.DoctorUnavailabilityReason,
				BookingWindowStartDate = clinic.BookingWindowStartDate,
				BookingWindowEndDate = clinic.BookingWindowEndDate,
				SupportedModes = clinic.SupportedModes
			};

			return new BookingDetailsDto
			{
				Doctor = doctorDto,
				Clinic = clinicDto
			};
		}

		public async Task ApproveAppointmentAsync(Guid appointmentId, string? comment)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
			{
				throw new NotFoundException($"Appointment with ID '{appointmentId}' not found.");
			}

			appointment.EAppointmentStatus = EAppointmentStatus.Confirmed;
			if (!string.IsNullOrWhiteSpace(comment))
			{
				appointment.Comment = comment.Trim();
			}

			await _dbContext.SaveChangesAsync();

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} has been approved.");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task RejectAppointmentAsync(Guid appointmentId, string reason)
		{
			if (string.IsNullOrWhiteSpace(reason))
			{
				throw new BadRequestException("A rejection reason must be provided.");
			}

			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
			{
				throw new NotFoundException($"Appointment with ID '{appointmentId}' not found.");
			}

			appointment.EAppointmentStatus = EAppointmentStatus.Rejected;
			appointment.RejectionReason = reason.Trim();

			await _dbContext.SaveChangesAsync();

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} was rejected. Reason: {reason}");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task CompleteAppointmentAsync(Guid appointmentId, string? comment, string? report)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
			{
				throw new NotFoundException($"Appointment with ID '{appointmentId}' not found.");
			}

			appointment.EAppointmentStatus = EAppointmentStatus.Completed;
			if (!string.IsNullOrWhiteSpace(comment))
			{
				appointment.Comment = comment.Trim();
			}
			if (!string.IsNullOrWhiteSpace(report))
			{
				appointment.Report = report.Trim();
			}

			await _dbContext.SaveChangesAsync();

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} has been marked as Completed.");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task MovePendingAppointmentAsync(Guid appointmentId, string? comment)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
			{
				throw new NotFoundException($"Appointment with ID '{appointmentId}' not found.");
			}

			appointment.EAppointmentStatus = EAppointmentStatus.Pending;
			if (!string.IsNullOrWhiteSpace(comment))
			{
				appointment.Comment = comment.Trim();
			}

			await _dbContext.SaveChangesAsync();

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} has been marked as Pending.");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task<PatientDto> GetPatientDetailsAsync(Guid userId, Guid patientId)
		{
			// Check if Doctor
			var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == userId);
			// Check if Clinic Admin
			var adminObj = await _dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == userId);

			if (doctor == null && adminObj == null)
			{
				throw new ForbiddenException("Only clinical staff and doctors can access patient details.");
			}

			// Fetch patient
			var p = await _dbContext.Patients.FindAsync(patientId);
			if (p == null)
			{
				throw new NotFoundException($"Patient with ID '{patientId}' was not found.");
			}

			var userPatient = await _dbContext.UserPatients.FirstOrDefaultAsync(up => up.PatientId == patientId);
			Address? address = null;
			if (userPatient != null)
			{
				address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userPatient.UserId);
			}

			return new PatientDto
			{
				PatientId = p.PatientId,
				UserId = userPatient?.UserId ?? Guid.Empty,
				Email = string.Empty,
				FirstName = p.FirstName,
				LastName = p.LastName,
				MobileNo = p.MobileNo,
				Gender = p.Gender.ToString(),
				DOB = p.DOB,
				BloodGroup = p.BloodGroup.ToString(),
				EmergencyContactName = p.EmergencyConactName,
				EmergencyContactNumber = p.EmergencyConactNumber,
				Country = address?.Country ?? "India",
				State = address?.State ?? string.Empty,
				City = address?.City ?? string.Empty,
				Area = address?.Area ?? string.Empty,
				Pincode = address?.Pincode ?? string.Empty,
				Addressline1 = address?.Addressline1 ?? string.Empty,
				Addressline2 = address?.Addressline2
			};
		}
	}
}
