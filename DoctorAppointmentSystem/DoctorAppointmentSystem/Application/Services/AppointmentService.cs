using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DoctorAppointmentSystem.Application.Services
{
	using System.Collections.Concurrent;

	public class AppointmentService : IAppointmentService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly INotificationService _notificationService;
		private readonly IDistributedCache _distributedCache;
		private readonly IServiceProvider _serviceProvider;
		private static readonly ConcurrentDictionary<string, SemaphoreSlim> _bookingLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

		public delegate void AppointmentActionLoggedEventHandler(object sender, AppointmentActionEventArgs e);
		public event AppointmentActionLoggedEventHandler? OnAppointmentActionLogged;

		private static SemaphoreSlim GetLock(Guid? clinicId, DateTime date)
		{
			string key = $"{(clinicId.HasValue ? clinicId.Value.ToString() : "Direct")}_{date:yyyy-MM-dd}";
			return _bookingLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
		}

		public AppointmentService(
			ApplicationDbContext dbContext,
			INotificationService notificationService,
			IDistributedCache distributedCache,
			IServiceProvider serviceProvider)
		{
			_dbContext = dbContext;
			_notificationService = notificationService;
			_distributedCache = distributedCache;
			_serviceProvider = serviceProvider;

			this.OnAppointmentActionLogged += HandleAppointmentActionLogged;
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

			// 5. Date must not be in the past
			if (dto.AppointmentDate.Date < DateTime.Today)
			{
				throw new BadRequestException("Appointment date cannot be in the past.");
			}

			Clinic? clinic = null;
			if (dto.ClinicId.HasValue && dto.ClinicId.Value != Guid.Empty)
			{
				clinic = await _dbContext.Clinics.FindAsync(dto.ClinicId.Value);
			}

			if (clinic != null && clinic.BookingWindowEndDate.HasValue && dto.AppointmentDate.Date > clinic.BookingWindowEndDate.Value.Date)
			{
				throw new BadRequestException($"Appointments at this branch are only open until {clinic.BookingWindowEndDate.Value:yyyy-MM-dd}. Please choose an earlier date.");
			}

			// Get a specific lock for this clinic and date
			var localLock = GetLock(clinic?.ClinicId, dto.AppointmentDate.Date);

			// --- START CRITICAL SECTION ---
			await localLock.WaitAsync();
			try
			{
				// 6. Daily limit check — count active (Pending + Confirmed) appointments for this clinic on this date
				if (clinic != null && clinic.MaxAppointmentsPerDay.HasValue)
				{
					var bookedCount = await _dbContext.Appointments
						.Where(app => app.Clinic != null && app.Clinic.ClinicId == clinic.ClinicId)
						.Where(app => app.AppointmentDate == dto.AppointmentDate.Date)
					.Where(app => app.EAppointmentStatus == EAppointmentStatus.Pending || app.EAppointmentStatus == EAppointmentStatus.Confirmed)
					.CountAsync();

					if (bookedCount >= clinic.MaxAppointmentsPerDay.Value)
					{
						throw new ConflictException($"This clinic is fully booked for {dto.AppointmentDate:yyyy-MM-dd}. Maximum {clinic.MaxAppointmentsPerDay.Value} appointments per day. Please choose another date.");
					}
				}

				// 7. Assign queue number — next sequential position for this clinic on this date
				int queueNumber = 1;
				if (clinic != null)
				{
					var maxQueue = await _dbContext.Appointments
						.Where(app => app.Clinic != null && app.Clinic.ClinicId == clinic.ClinicId)
						.Where(app => app.AppointmentDate == dto.AppointmentDate.Date)
						.Where(app => app.EAppointmentStatus == EAppointmentStatus.Pending || app.EAppointmentStatus == EAppointmentStatus.Confirmed)
						.MaxAsync(app => (int?)app.QueueNumber) ?? 0;
					queueNumber = maxQueue + 1;
				}

				// 8. Create the appointment with queue number
				var appointment = new Appointment
				{
					AppointmentId = Guid.NewGuid(),
					AppointmentDate = dto.AppointmentDate.Date,
					QueueNumber = queueNumber,
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
				TriggerAppointmentActionLog(appointment.AppointmentId, "Created", userId, "Patient", "Booked appointment.");

				// Trigger notifications
				var dateStr = appointment.AppointmentDate.ToString("yyyy-MM-dd");
				var msg = $"New appointment #{appointment.QueueNumber} booked by {patient.FirstName} {patient.LastName} for {dateStr}.";

				// Notify Doctor
				await _notificationService.CreateNotificationAsync(doctor.User.UserId, msg);

				// Notify Clinic Admin
				if (clinic != null)
				{
					var adminUserIdObj = await _dbContext.AdminClinics
						.Where(ac => ac.ClinicId == clinic.ClinicId)
						.Select(ac => (Guid?)ac.Admin.User.UserId)
						.FirstOrDefaultAsync();
					if (adminUserIdObj.HasValue)
					{
						await _notificationService.CreateNotificationAsync(adminUserIdObj.Value, msg);
					}
				}
				await _notificationService.SendRefreshSignalAsync("Appointments");

				return MapToDto(appointment, patient, doctor);
			}
			finally
			{
				localLock.Release();
			}
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
			appointment.CancelledDate = DateTime.UtcNow;
			appointment.CancelledBy = "Patient";
			await _dbContext.SaveChangesAsync();
			TriggerAppointmentActionLog(appointment.AppointmentId, "Cancelled", userId, "Patient", "Patient cancelled appointment.");

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
						var adminUserIdObj = await _dbContext.AdminClinics
							.Where(ac => ac.ClinicId == appointment.Clinic.ClinicId)
							.Select(ac => (Guid?)ac.Admin.User.UserId)
							.FirstOrDefaultAsync();
						if (adminUserIdObj.HasValue)
						{
							await _notificationService.CreateNotificationAsync(adminUserIdObj.Value, msg);
						}
					}
					await _notificationService.SendRefreshSignalAsync("Appointments");
				}
			}
		}
    // Auto-expire any pending appointments whose date has passed
    public async Task AutoExpirePastPendingAppointmentsAsync()
    {
        var today = DateTime.Today;
        var staleAppointments = await _dbContext.Appointments
            .Where(app => app.EAppointmentStatus == EAppointmentStatus.Pending && app.AppointmentDate < today)
            .ToListAsync();
        if (staleAppointments.Any())
        {
            foreach (var app in staleAppointments)
            {
                app.EAppointmentStatus = EAppointmentStatus.Expired;
                // Notify patient about expiration
                var patient = await _dbContext.Patients
                    .FirstOrDefaultAsync(p => p.PatientId == EF.Property<Guid>(app, "PatientId"));
                if (patient != null)
                {
                    var msg = $"Your appointment on {app.AppointmentDate:yyyy-MM-dd} has expired because it was not confirmed in time. Please book a new appointment.";
                    var patientUser = await _dbContext.UserPatients.FirstOrDefaultAsync(up => up.PatientId == patient.PatientId);
                if (patientUser != null)
                {
                    await _notificationService.CreateNotificationAsync(patientUser.UserId, msg);
                }
                }
            }
            await _dbContext.SaveChangesAsync();
            
            foreach (var app in staleAppointments)
            {
                TriggerAppointmentActionLog(app.AppointmentId, "Skipped", null, "System", "Auto-expired past pending appointment.");
            }
            await _notificationService.SendRefreshSignalAsync("Appointments");
        }
    }

    // Doctor or clinic admin cancels a confirmed appointment with a reason
    public async Task DoctorCancelAppointmentAsync(Guid userId, Guid appointmentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BadRequestException("A cancellation reason must be provided.");
        }
        var appointment = await _dbContext.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        if (appointment == null)
        {
            throw new NotFoundException($"Appointment with ID '{appointmentId}' not found.");
        }
        // Verify the user is the doctor for this appointment or an admin of the clinic
        var isDoctor = await _dbContext.Doctors.AnyAsync(d => d.DoctorId == appointment.Doctor.DoctorId && d.User.UserId == userId);
        var isAdmin = false;
        if (appointment.Clinic != null)
        {
            isAdmin = await _dbContext.AdminClinics.AnyAsync(ac => ac.ClinicId == appointment.Clinic.ClinicId && ac.Admin.User.UserId == userId);
        }
        if (!isDoctor && !isAdmin)
        {
            throw new ForbiddenException("You do not have permission to cancel this appointment.");
        }
        appointment.EAppointmentStatus = EAppointmentStatus.Cancelled;
        appointment.CancelledDate = DateTime.UtcNow;
        appointment.CancelledBy = isDoctor ? "Doctor" : "Admin";
        // Optionally store cancellation reason if a property exists
        // appointment.CancellationReason = reason;
        await _dbContext.SaveChangesAsync();
        TriggerAppointmentActionLog(appointment.AppointmentId, "Cancelled", userId, isDoctor ? "Doctor" : "Admin", string.IsNullOrWhiteSpace(reason) ? "Cancelled by clinic." : reason);
        var msg = $"Your appointment on {appointment.AppointmentDate:yyyy-MM-dd} with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} has been cancelled. Reason: {reason}";
        var patientUser = await _dbContext.UserPatients.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
                if (patientUser != null)
                {
                    await _notificationService.CreateNotificationAsync(patientUser.UserId, msg);
                }
        await _notificationService.SendRefreshSignalAsync("Appointments");
    }

    
		public async Task<PagedResult<AppointmentDto>> GetAdminDoctorDashboardAppointmentsAsync(Guid userId, string? status, DateTime? startDate, DateTime? endDate, string? search, Guid? patientId, int page, int size)
		{
			var query = _dbContext.Appointments
				.Include(app => app.Patient)
				.Include(app => app.Doctor).ThenInclude(d => d.Specialization)
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
					.Include(a => a.AdminClinics)
						.ThenInclude(ac => ac.Clinic)
					.FirstOrDefaultAsync(a => a.User.UserId == userId);
				if (isAdmin != null)
				{
					var adminClinicIds = isAdmin.AdminClinics.Select(ac => ac.ClinicId).ToList();
					query = query.Where(app => app.Clinic != null && adminClinicIds.Contains(app.Clinic.ClinicId));
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

			// Filter by date range (use AppointmentDate instead of StartTime)
			if (startDate.HasValue)
			{
				query = query.Where(app => app.AppointmentDate >= startDate.Value.Date);
			}

			if (endDate.HasValue)
			{
				query = query.Where(app => app.AppointmentDate <= endDate.Value.Date);
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
			// Custom Sort: Active (Confirmed/Pending) first (ascending), then Inactive (Completed/Cancelled/Rejected) (descending)
			var items = await query
				.OrderByDescending(app => app.EAppointmentStatus == EAppointmentStatus.Confirmed || app.EAppointmentStatus == EAppointmentStatus.Pending) // Active first
				.ThenBy(app => (app.EAppointmentStatus == EAppointmentStatus.Confirmed || app.EAppointmentStatus == EAppointmentStatus.Pending) ? app.AppointmentDate : DateTime.MaxValue) // Active ascending
				.ThenByDescending(app => (app.EAppointmentStatus == EAppointmentStatus.Confirmed || app.EAppointmentStatus == EAppointmentStatus.Pending) ? DateTime.MinValue : app.AppointmentDate) // Inactive descending
				.ThenBy(app => app.QueueNumber)
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
			var adminObj = await _dbContext.Admins
				.Include(a => a.AdminClinics)
				.FirstOrDefaultAsync(a => a.User.UserId == userId);

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
			else if (adminObj != null && adminObj.AdminClinics.Any())
			{
				var adminClinicIds = adminObj.AdminClinics.Select(ac => ac.ClinicId).ToList();
				patientIds = await _dbContext.Appointments
					.Where(a => a.Clinic != null && adminClinicIds.Contains(a.Clinic.ClinicId))
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

		public async Task<PagedResult<AppointmentDto>> GetPatientDashboardAppointmentsAsync(Guid userId, string? status, bool isHistory, int page, int size)
		{
			// Get all patient IDs linked to this User
			var linkedPatientIds = await _dbContext.UserPatients
				.Where(up => up.UserId == userId && up.IsVerified)
				.Select(up => up.PatientId)
				.ToListAsync();

			var query = _dbContext.Appointments
				.Include(app => app.Patient)
				.Include(app => app.Doctor).ThenInclude(d => d.Specialization)
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

			var today = DateTime.Today;
			if (isHistory)
			{
				query = query.Where(app => app.AppointmentDate < today);
			}
			else
			{
				query = query.Where(app => app.AppointmentDate >= today);
			}

			var totalCount = await query.CountAsync();
			
			IQueryable<Appointment> sortedQuery;
			if (isHistory)
			{
				// History: Sort strictly by Date descending (most recent first)
				sortedQuery = query.OrderByDescending(app => app.AppointmentDate);
			}
			else
			{
				// Dashboard: Active first, then Inactive
				sortedQuery = query
					.OrderByDescending(app => app.EAppointmentStatus == EAppointmentStatus.Confirmed || app.EAppointmentStatus == EAppointmentStatus.Pending || app.EAppointmentStatus == EAppointmentStatus.RescheduleProposed)
					.ThenBy(app => app.AppointmentDate)
					.ThenBy(app => app.QueueNumber);
			}

			var items = await sortedQuery
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
				.Include(app => app.Doctor).ThenInclude(d => d.Specialization)
				.Include(app => app.Doctor).ThenInclude(d => d.Clinics).ThenInclude(c => c.Address)
				.Where(app => linkedPatientIds.Contains(app.Patient.PatientId))
				.ToListAsync();

			// Group appointments by Doctor
			var groups = appointments.GroupBy(app => app.Doctor.DoctorId);

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
					ConsultationFee = doctor.ConsultationFee,
					AboutDoctor = doctor.AboutDoctor,
					Age = DateTime.UtcNow.Year - doctor.DOB.Year,
					YearsOfExperience = doctor.YearsOfExperience,
					Qualification = doctor.Qualification,
					LicenceNumber = doctor.LicenceNumber,
					Clinics = doctor.Clinics.Select(c => new ClinicBasicDto
					{
						ClinicId = c.ClinicId,
						ClinicName = c.ClinicName,
						ClinicType = c.ClinicType,
						State = c.Address.State,
						City = c.Address.City,
						Area = c.Address.Area,
						ContactNumber = c.ContactNumber
					}).ToList(),
					Appointments = group.Select(app => MapToDto(app, app.Patient, doctor))
				});
			}

			return consultedDoctors;
		}

		public async Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync()
		{
			const string cacheKey = "available_doctors_list";
			string? cachedData = null;

			try
			{
				cachedData = await _distributedCache.GetStringAsync(cacheKey);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in GetAvailableDoctorsAsync: {ex.Message}. Falling back to Database query.");
			}

			if (!string.IsNullOrEmpty(cachedData))
			{
				try
				{
					return JsonSerializer.Deserialize<IEnumerable<DoctorDto>>(cachedData) ?? new List<DoctorDto>();
				}
				catch
				{
					// Ignore deserialization issues and query db
				}
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
					AboutDoctor = d.AboutDoctor,
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

			try
			{
				var cacheOptions = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Cache for 24 hours since we use eviction
				};
				await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(doctors), cacheOptions);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in GetAvailableDoctorsAsync while saving cache: {ex.Message}.");
			}

			return doctors;
		}

		public async Task<IEnumerable<Specialization>> GetSpecializationsAsync()
		{
			const string cacheKey = "specializations_list";
			string? cachedData = null;

			try
			{
				cachedData = await _distributedCache.GetStringAsync(cacheKey);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in GetSpecializationsAsync: {ex.Message}. Falling back to Database query.");
			}

			if (!string.IsNullOrEmpty(cachedData))
			{
				try
				{
					return JsonSerializer.Deserialize<IEnumerable<Specialization>>(cachedData) ?? new List<Specialization>();
				}
				catch
				{
					// Ignore deserialization issues and query db
				}
			}

			var specializations = await _dbContext.Specializations.OrderBy(s => s.SpecializationName).ToListAsync();

			try
			{
				var cacheOptions = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Cache for 24 hours
				};
				await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(specializations), cacheOptions);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in GetSpecializationsAsync while saving cache: {ex.Message}.");
			}

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
					AboutDoctor = d.AboutDoctor,
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

			return doctors;
		}

		private AppointmentDto MapToDto(Appointment app, Patient patient, Doctor doctor)
		{
			return new AppointmentDto
			{
				AppointmentId = app.AppointmentId,
				PatientId = patient.PatientId,
				PatientName = $"{patient.FirstName} {patient.LastName}",
				PatientAge = CalculateAge(patient.DOB),
				PatientGender = patient.Gender.ToString(),
				DoctorId = doctor.DoctorId,
				DoctorName = $"{doctor.FirstName} {doctor.LastName}",
				DoctorSpecialization = doctor.Specialization?.SpecializationName ?? string.Empty,
				ClinicId = app.Clinic?.ClinicId,
				ClinicName = app.Clinic?.ClinicName,
				AppointmentDate = app.AppointmentDate,
				StartTime = app.StartTime,
				EndTime = app.EndTime,
				Status = app.EAppointmentStatus.ToString(),
				Reason = app.Reason,
				ConsultationType = app.EConsultationType.ToString(),
				CreatedDate = DateTime.SpecifyKind(app.CreatedDate, DateTimeKind.Utc),
				Comment = app.Comment,
				Report = app.Report,
				RejectionReason = app.RejectionReason,
				QueueNumber = app.QueueNumber,
				DoctorAssignedTime = app.DoctorAssignedTime,
				RescheduleProposedDate = app.RescheduleProposedDate,
				RescheduleProposedTime = app.RescheduleProposedTime,
				RescheduleReason = app.RescheduleReason,
				ConfirmedDate = app.ConfirmedDate.HasValue ? DateTime.SpecifyKind(app.ConfirmedDate.Value, DateTimeKind.Utc) : null,
				RescheduleProposedAt = app.RescheduleProposedAt.HasValue ? DateTime.SpecifyKind(app.RescheduleProposedAt.Value, DateTimeKind.Utc) : null,
				CancelledDate = app.CancelledDate.HasValue ? DateTime.SpecifyKind(app.CancelledDate.Value, DateTimeKind.Utc) : null,
				CancelledBy = app.CancelledBy
			};
		}

		public async Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId)
		{
			var list = await _dbContext.Clinics
				.Include(c => c.Doctor)
				.Include(c => c.Address)
				.Where(c => c.Doctor.DoctorId == doctorId && c.VerificationStatus == EVerificationStatus.Verified && c.ParentClinicId == null)
				.Select(c => new
				{
					Clinic = c,
					AdminInfo = _dbContext.AdminClinics
						.Where(ac => ac.ClinicId == c.ClinicId)
						.Select(ac => new
						{
							AdminName = ac.Admin.FirstName + " " + ac.Admin.LastName,
							AdminEmail = ac.Admin.User.Email,
							AdminMobileNo = ac.Admin.MobileNo,
							AdminIsVerified = ac.Admin.IsVerified
						})
						.FirstOrDefault()
				})
				.ToListAsync();

			return list.Select(x => new ClinicDto
			{
				ClinicId = x.Clinic.ClinicId,
				ClinicName = x.Clinic.ClinicName,
				ClinicType = x.Clinic.ClinicType,
				DoctorId = x.Clinic.Doctor.DoctorId,
				DoctorName = $"Dr. {x.Clinic.Doctor.FirstName} {x.Clinic.Doctor.LastName}",
				State = x.Clinic.Address.State,
				City = x.Clinic.Address.City,
				Pincode = x.Clinic.Address.Pincode,
				IsVerified = x.Clinic.VerificationStatus == EVerificationStatus.Verified,
				VerificationStatus = x.Clinic.VerificationStatus.ToString(),
				RejectionReason = x.Clinic.RejectionReason,
				Addressline1 = x.Clinic.Address.Addressline1,
				Addressline2 = x.Clinic.Address.Addressline2,
				Area = x.Clinic.Address.Area,
				ContactNumber = x.Clinic.ContactNumber,
				HasAdmin = x.AdminInfo != null,
				AdminName = x.AdminInfo != null ? x.AdminInfo.AdminName : null,
				AdminEmail = x.AdminInfo != null ? x.AdminInfo.AdminEmail : null,
				AdminMobileNo = x.AdminInfo != null ? x.AdminInfo.AdminMobileNo : null,
				AdminIsVerified = x.AdminInfo != null && x.AdminInfo.AdminIsVerified,
				OpenDays = x.Clinic.OpenDays,
				StartTime = x.Clinic.StartTime,
				EndTime = x.Clinic.EndTime,
				IsAvailable = x.Clinic.IsAvailable,
				UnavailabilityReason = x.Clinic.UnavailabilityReason,
				IsDoctorAvailable = x.Clinic.IsDoctorAvailable,
				DoctorUnavailabilityReason = x.Clinic.DoctorUnavailabilityReason,
				BookingWindowEndDate = x.Clinic.BookingWindowEndDate,
				BookingWindowStartDate = x.Clinic.BookingWindowStartDate,
				SupportedModes = x.Clinic.SupportedModes,
				MaxAppointmentsPerDay = x.Clinic.MaxAppointmentsPerDay
			});
		}

		public async Task<DayAvailabilityDto> GetDayAvailabilityAsync(Guid clinicId, DateTime date)
		{
			var targetDate = date.Date;
			var clinic = await _dbContext.Clinics.FindAsync(clinicId);

			var bookedCount = await _dbContext.Appointments
				.Where(app => app.Clinic != null && app.Clinic.ClinicId == clinicId)
				.Where(app => app.AppointmentDate == targetDate)
				.Where(app => app.EAppointmentStatus == EAppointmentStatus.Pending || app.EAppointmentStatus == EAppointmentStatus.Confirmed)
				.CountAsync();

			int? maxCap = clinic?.MaxAppointmentsPerDay;
			int? remaining = maxCap.HasValue ? Math.Max(0, maxCap.Value - bookedCount) : (int?)null;
			bool isFull = maxCap.HasValue && bookedCount >= maxCap.Value;

			return new DayAvailabilityDto
			{
				Date = targetDate,
				ClinicId = clinicId,
				BookedCount = bookedCount,
				MaxCapacity = maxCap,
				RemainingSlots = remaining,
				IsFull = isFull
			};
		}

		public async Task AssignAppointmentTimeAsync(Guid userId, Guid appointmentId, AssignAppointmentTimeDto dto)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.Include(a => a.Clinic)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
				throw new NotFoundException($"Appointment '{appointmentId}' not found.");

			// Only the doctor or the clinic admin can assign a time
			var isDoctor = await _dbContext.Doctors.AnyAsync(d => d.DoctorId == appointment.Doctor.DoctorId && d.User.UserId == userId);
			var isAdmin = false;
			if (appointment.Clinic != null)
				isAdmin = await _dbContext.AdminClinics.AnyAsync(ac => ac.ClinicId == appointment.Clinic.ClinicId && ac.Admin.User.UserId == userId);

			if (!isDoctor && !isAdmin)
				throw new ForbiddenException("Only the treating doctor or clinic admin can assign appointment times.");

			appointment.DoctorAssignedTime = dto.DoctorAssignedTime;
			appointment.EAppointmentStatus = EAppointmentStatus.Confirmed;
			appointment.ConfirmedDate = DateTime.UtcNow;
			if (!string.IsNullOrWhiteSpace(dto.Comment))
				appointment.Comment = dto.Comment.Trim();

			await _dbContext.SaveChangesAsync();
			TriggerAppointmentActionLog(appointment.AppointmentId, "Confirmed", userId, null, "Assigned time: " + dto.DoctorAssignedTime.ToString("h:mm tt"));

			// Notify patient
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				var timeStr = dto.DoctorAssignedTime.ToString("hh:mm tt");
				await _notificationService.CreateNotificationAsync(
					userPatient.UserId,
					$"Your appointment #{appointment.QueueNumber} on {appointment.AppointmentDate:dd MMM yyyy} has been assigned at {timeStr}."
				);
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
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
				AboutDoctor = doctor.AboutDoctor ?? string.Empty,
				Age = DateTime.UtcNow.Year - doctor.DOB.Year,
				Clinics = doctor.Clinics.Select(c => new ClinicBasicDto
				{
					ClinicId = c.ClinicId,
					ClinicName = c.ClinicName,
					ClinicType = c.ClinicType,
					State = c.Address.State,
					City = c.Address.City,
					Area = c.Address.Area,
					ContactNumber = c.ContactNumber
				}).ToList()
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
				ContactNumber = clinic.ContactNumber,
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
				SupportedModes = clinic.SupportedModes,
				MaxAppointmentsPerDay = clinic.MaxAppointmentsPerDay
			};

			return new BookingDetailsDto
			{
				Doctor = doctorDto,
				Clinic = clinicDto
			};
		}

		public async Task ApproveAppointmentAsync(Guid userId, Guid appointmentId, string? comment, DateTime? assignedTime = null)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

			if (appointment == null)
			{
				throw new NotFoundException($"Appointment with ID '{appointmentId}' not found.");
			}

			if (!assignedTime.HasValue)
			{
				throw new BadRequestException("An assigned time is required to approve the appointment.");
			}

			appointment.EAppointmentStatus = EAppointmentStatus.Confirmed;
			appointment.ConfirmedDate = DateTime.UtcNow;
			if (!string.IsNullOrWhiteSpace(comment))
			{
				appointment.Comment = comment.Trim();
			}
			
			if (assignedTime.HasValue)
			{
				appointment.DoctorAssignedTime = assignedTime.Value;
			}

			await _dbContext.SaveChangesAsync();
			TriggerAppointmentActionLog(appointment.AppointmentId, "Confirmed", userId, null, comment ?? "Approved appointment.");

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} has been approved.");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task RejectAppointmentAsync(Guid userId, Guid appointmentId, string reason)
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
			TriggerAppointmentActionLog(appointment.AppointmentId, "Rejected", userId, null, reason ?? "Rejected appointment.");

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} was rejected. Reason: {reason}");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task CompleteAppointmentAsync(Guid userId, Guid appointmentId, string? comment, string? report)
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
			TriggerAppointmentActionLog(appointment.AppointmentId, "Completed", userId, null, "Completed consultation.");

			// Refresh SignalR hubs
			var userPatient = await _dbContext.UserPatients
				.FirstOrDefaultAsync(up => up.PatientId == appointment.Patient.PatientId);
			if (userPatient != null)
			{
				await _notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} has been marked as Completed.");
			}
			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public async Task MovePendingAppointmentAsync(Guid userId, Guid appointmentId, string? comment)
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
			TriggerAppointmentActionLog(appointment.AppointmentId, "Skipped", userId, null, "Marked as no-show/late.");

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
		public async Task ProposeRescheduleAsync(Guid userId, ProposeRescheduleDto dto)
		{
			var isDoctor = await _dbContext.Doctors.AnyAsync(d => d.User.UserId == userId);
			var isAdmin = await _dbContext.Admins.AnyAsync(a => a.User.UserId == userId);

			if (!isDoctor && !isAdmin)
				throw new UnauthorizedAccessException("Only doctors and clinic admins can propose a reschedule.");

			var appointment = await _dbContext.Appointments
				.Include(a => a.Doctor)
					.ThenInclude(d => d.User)
				.Include(a => a.Clinic)
				.FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId);

			if (appointment == null)
				throw new NotFoundException("Appointment not found.");

			if (isDoctor && appointment.Doctor.User.UserId != userId)
				throw new UnauthorizedAccessException("You can only reschedule your own appointments.");

			var clinic = appointment.Clinic;

			if (dto.ProposedDate.Date < DateTime.Today)
			{
				throw new BadRequestException("Proposed reschedule date cannot be in the past.");
			}

			if (!dto.ProposedTime.HasValue)
			{
				throw new BadRequestException("A proposed time is required when rescheduling an appointment.");
			}

			// Validation: Booking Window
			if (clinic != null && clinic.BookingWindowEndDate.HasValue && dto.ProposedDate.Date > clinic.BookingWindowEndDate.Value.Date)
			{
				throw new BadRequestException($"Appointments at this branch are only open until {clinic.BookingWindowEndDate.Value:yyyy-MM-dd}. Please propose an earlier date.");
			}

			// Get a specific lock for this clinic and proposed date
			var localLock = GetLock(clinic?.ClinicId, dto.ProposedDate.Date);

			// --- START CRITICAL SECTION ---
			await localLock.WaitAsync();
			try
			{
				// Validation: Daily Max Limits
				if (clinic != null && clinic.MaxAppointmentsPerDay.HasValue)
				{
					var bookedCount = await _dbContext.Appointments
						.Where(app => app.Clinic != null && app.Clinic.ClinicId == clinic.ClinicId)
						.Where(app => app.AppointmentDate == dto.ProposedDate.Date)
					.Where(app => app.EAppointmentStatus == EAppointmentStatus.Pending || app.EAppointmentStatus == EAppointmentStatus.Confirmed)
					.CountAsync();

				if (bookedCount >= clinic.MaxAppointmentsPerDay.Value)
				{
					throw new ConflictException($"This clinic is fully booked for {dto.ProposedDate:yyyy-MM-dd}. Maximum {clinic.MaxAppointmentsPerDay.Value} appointments per day. Please propose another date.");
				}
			}

			appointment.EAppointmentStatus = EAppointmentStatus.RescheduleProposed;
			appointment.RescheduleProposedDate = dto.ProposedDate;
			appointment.RescheduleProposedTime = dto.ProposedTime;
			appointment.RescheduleReason = dto.Reason;
			appointment.RescheduleProposedAt = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync();
			TriggerAppointmentActionLog(appointment.AppointmentId, "RescheduleProposed", userId, isDoctor ? "Doctor" : "Admin", $"Proposed reschedule to {dto.ProposedDate:yyyy-MM-dd} at {dto.ProposedTime:h:mm tt}. Reason: {dto.Reason}");

			var patientId = _dbContext.Entry(appointment).Property<Guid>("PatientId").CurrentValue;
			var userPatients = await _dbContext.UserPatients.Where(up => up.PatientId == patientId).ToListAsync();
			
			foreach (var up in userPatients)
			{
				await _notificationService.CreateNotificationAsync(up.UserId, $"A new reschedule time has been proposed for your appointment on {appointment.AppointmentDate:MMM dd, yyyy}. Please review it.");
			}

			await _notificationService.SendRefreshSignalAsync("Appointments");
			}
			finally
			{
				localLock.Release();
			}
		}

		public async Task RespondToRescheduleAsync(Guid userId, RespondRescheduleDto dto)
		{
			var appointment = await _dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Clinic)
				.FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId);

			if (appointment == null)
				throw new NotFoundException("Appointment not found.");

			var userPatient = await _dbContext.UserPatients.FirstOrDefaultAsync(up => up.UserId == userId && up.PatientId == appointment.Patient.PatientId);
			if (userPatient == null)
				throw new UnauthorizedAccessException("You can only respond to your own appointments.");

			if (appointment.EAppointmentStatus != EAppointmentStatus.RescheduleProposed)
				throw new BadRequestException("This appointment does not have a pending reschedule proposal.");

			if (dto.Accept)
			{
				var localLock = GetLock(appointment.Clinic?.ClinicId, appointment.RescheduleProposedDate.Value.Date);
				await localLock.WaitAsync();
				try
				{
					// Re-check status inside lock to prevent race conditions
					var currentStatus = await _dbContext.Appointments
						.Where(a => a.AppointmentId == dto.AppointmentId)
						.Select(a => a.EAppointmentStatus)
						.FirstOrDefaultAsync();

					if (currentStatus != EAppointmentStatus.RescheduleProposed)
						throw new BadRequestException("This appointment was already updated.");

					appointment.EAppointmentStatus = EAppointmentStatus.Confirmed;
					appointment.ConfirmedDate = DateTime.UtcNow;
					appointment.AppointmentDate = appointment.RescheduleProposedDate.Value;
					appointment.DoctorAssignedTime = appointment.RescheduleProposedTime;
					
					appointment.RescheduleProposedDate = null;
					appointment.RescheduleProposedTime = null;
					appointment.RescheduleReason = null;

					await _dbContext.SaveChangesAsync();
					TriggerAppointmentActionLog(appointment.AppointmentId, "Confirmed", userId, "Patient", "Patient accepted rescheduled time.");
				}
				finally
				{
					localLock.Release();
				}
				
				// Notify the Doctor
				var doctorIdObj = _dbContext.Entry(appointment).Property("DoctorId").CurrentValue;
				if (doctorIdObj != null)
				{
					var doctor = await _dbContext.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == (Guid)doctorIdObj);
					if (doctor != null)
					{
						await _notificationService.CreateNotificationAsync(doctor.User.UserId, $"Patient {appointment.Patient.FirstName} {appointment.Patient.LastName} accepted the rescheduled time for {appointment.AppointmentDate:MMM dd, yyyy}.");
					}
				}
			}
			else
			{
				appointment.EAppointmentStatus = EAppointmentStatus.Cancelled;
				appointment.CancelledDate = DateTime.UtcNow;
				appointment.CancelledBy = "Patient";
				appointment.Comment = "Patient declined the proposed reschedule date.";
				await _dbContext.SaveChangesAsync();
				TriggerAppointmentActionLog(appointment.AppointmentId, "Cancelled", userId, "Patient", "Patient declined the proposed reschedule date.");
				
				// Notify the Doctor
				var doctorIdObj = _dbContext.Entry(appointment).Property("DoctorId").CurrentValue;
				if (doctorIdObj != null)
				{
					var doctor = await _dbContext.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == (Guid)doctorIdObj);
					if (doctor != null)
					{
						await _notificationService.CreateNotificationAsync(doctor.User.UserId, $"Patient {appointment.Patient.FirstName} {appointment.Patient.LastName} declined the rescheduled time and the appointment was cancelled.");
					}
				}
			}

			await _notificationService.SendRefreshSignalAsync("Appointments");
		}

		public class AppointmentActionEventArgs : EventArgs
		{
			public Guid AppointmentId { get; set; }
			public string Action { get; set; } = null!;
			public Guid? ActorUserId { get; set; }
			public string? ActorRole { get; set; }
			public string? Notes { get; set; }
		}

		private void TriggerAppointmentActionLog(Guid appointmentId, string action, Guid? actorUserId, string? actorRole, string? notes)
		{
			var args = new AppointmentActionEventArgs
			{
				AppointmentId = appointmentId,
				Action = action,
				ActorUserId = actorUserId,
				ActorRole = actorRole,
				Notes = notes
			};
			OnAppointmentActionLogged?.Invoke(this, args);
		}

		private void HandleAppointmentActionLogged(object? sender, AppointmentActionEventArgs e)
		{
			Task.Run(async () =>
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

					var actorName = "System";
					if (e.ActorUserId.HasValue)
					{
						var user = await dbContext.Users.FindAsync(e.ActorUserId.Value);
						if (user != null)
						{
							var patient = await dbContext.Patients.FirstOrDefaultAsync(p => dbContext.UserPatients.Any(up => up.PatientId == p.PatientId && up.UserId == user.UserId));
							var doctor = await dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == user.UserId);
							var admin = await dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == user.UserId);
							
							if (patient != null) { actorName = patient.FirstName + " " + patient.LastName; e.ActorRole ??= "Patient"; }
							else if (doctor != null) { actorName = doctor.FirstName + " " + doctor.LastName; e.ActorRole ??= "Doctor"; }
							else if (admin != null) { actorName = admin.FirstName + " " + admin.LastName; e.ActorRole ??= "Admin"; }
							else { actorName = user.Email; e.ActorRole ??= "System"; }
						}
					}

					var log = new DoctorAppointmentSystem.Domain.Entities.AppointmentAuditLog
					{
						LogId = Guid.NewGuid(),
						AppointmentId = e.AppointmentId,
						Action = e.Action,
						Timestamp = DateTime.UtcNow,
						ActorUserId = e.ActorUserId,
						ActorName = actorName,
						ActorRole = e.ActorRole ?? "System",
						Notes = e.Notes
					};

					dbContext.AppointmentAuditLogs.Add(log);
					await dbContext.SaveChangesAsync();

					// Resolve notification service directly from scope to broadcast refresh signal
					var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
					await notificationService.SendRefreshSignalAsync("AuditLogs");
				}
				catch (Exception ex)
				{
					// Fire-and-forget: In a real app we would log to ILogger
					Console.WriteLine($"[Audit Log Event Failed] {ex.Message}");
				}
			});
		}

		public async Task<PagedResult<AppointmentAuditLogDto>> GetAppointmentAuditLogsAsync(Guid userId, Guid? clinicId, Guid? appointmentId, int page, int size)
		{
			var query = _dbContext.AppointmentAuditLogs
				.Include(l => l.Appointment)
				.ThenInclude(a => a.Patient)
				.AsQueryable();

			if (appointmentId.HasValue)
			{
				query = query.Where(l => l.AppointmentId == appointmentId.Value);
			}
			else
			{
				// If no specific appointment is requested, we need to apply role-based filtering (Global Audit Logs view)
				var user = await _dbContext.Users.FindAsync(userId);
				var isSuperAdmin = false;
				if (user != null)
				{
					var roleId = _dbContext.Entry(user).Property<Guid>("RoleId").CurrentValue;
					isSuperAdmin = await _dbContext.Roles.AnyAsync(r => r.RoleId == roleId && r.Role == ERole.SuperAdmin);
				}
				
				if (!isSuperAdmin)
				{
					var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == userId);
					var admin = await _dbContext.Admins.Include(a => a.AdminClinics).ThenInclude(ac => ac.Clinic).FirstOrDefaultAsync(a => a.User.UserId == userId);

					if (doctor != null)
					{
						query = query.Where(l => l.Appointment.Doctor.DoctorId == doctor.DoctorId);
						if (clinicId.HasValue)
						{
							query = query.Where(l => l.Appointment.Clinic.ClinicId == clinicId.Value);
						}
					}
					else if (admin != null)
					{
						var adminClinicIds = admin.AdminClinics.Select(ac => ac.ClinicId).ToList();
						query = query.Where(l => adminClinicIds.Contains(l.Appointment.Clinic.ClinicId));
					}
					else
					{
						// Patients or Unassigned can only see logs for appointments where they are the patient.
						var linkedPatientIds = await _dbContext.UserPatients
							.Where(up => up.UserId == userId)
							.Select(up => up.PatientId)
							.ToListAsync();
						
						query = query.Where(l => linkedPatientIds.Contains(l.Appointment.Patient.PatientId));
					}
				}
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(l => l.Timestamp)
				.Skip((page - 1) * size)
				.Take(size)
				.Select(l => new AppointmentAuditLogDto
				{
					LogId = l.LogId,
					AppointmentId = l.AppointmentId,
					PatientName = l.Appointment.Patient.FirstName + " " + l.Appointment.Patient.LastName,
					Action = l.Action,
					Timestamp = l.Timestamp,
					ActorName = l.ActorName,
					ActorRole = l.ActorRole,
					Notes = l.Notes
				})
				.ToListAsync();

			// SQL Server datetime2 drops DateTimeKind. Explicitly specify Utc so the JSON serializer appends 'Z'
			// This ensures the Angular frontend correctly parses it as UTC and converts it to the user's local timezone.
			foreach (var item in items)
			{
				item.Timestamp = DateTime.SpecifyKind(item.Timestamp, DateTimeKind.Utc);
			}

			return new PagedResult<AppointmentAuditLogDto>(items, totalCount, page, size);
		}
		private static int CalculateAge(DateTime dob)
		{
			var today = DateTime.Today;
			var age = today.Year - dob.Year;
			if (dob.Date > today.AddYears(-age)) age--;
			return age;
		}
	}
}

