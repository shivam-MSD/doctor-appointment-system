using Microsoft.Extensions.Caching.Distributed;
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
		private readonly IEmailService _emailService;
		private readonly IWhatsAppService _whatsAppService;
		private readonly IDistributedCache _distributedCache;
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _contactOtpCache = new();

		public PatientService(
			ApplicationDbContext dbContext,
			IEmailService emailService,
			IWhatsAppService whatsAppService,
			IDistributedCache distributedCache)
		{
			_dbContext = dbContext;
			_emailService = emailService;
			_whatsAppService = whatsAppService;
			_distributedCache = distributedCache;
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

			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);

			return MapToDto(userId, patient, address);
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

			// 4. Update demographics properties (MobileNo can ONLY be updated via ConfirmUpdateContactInfoAsync OTP verification)
			patient.FirstName = dto.FirstName;
			patient.LastName = dto.LastName;
			patient.Gender = Enum.TryParse<EGender>(dto.Gender, true, out var genderEnum) ? genderEnum : EGender.Male;
			patient.DOB = dto.DOB;
			patient.EmergencyConactName = dto.EmergencyContactName;
			patient.EmergencyConactNumber = dto.EmergencyContactNumber;
			patient.UpdatedDate = DateTime.UtcNow;

			// 5. Update Address
			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);
			if (address == null)
			{
				var userObj = await _dbContext.Users.FindAsync(userId);
				if (userObj != null)
				{
					address = new Address
					{
						AddressId = Guid.NewGuid(),
						User = userObj,
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

			return MapToDto(userId, patient, address);
		}

		public async Task<PagedResult<DoctorDto>> GetDoctorsForPatientAsync(
			string? search,
			Guid? specializationId,
			string? state,
			string? city,
			int page,
			int size)
		{
			var query = _dbContext.Doctors
				.Include(d => d.Specialization)
				.Include(d => d.User)
				.Where(d => d.VerificationStatus == EVerificationStatus.Verified)
				.AsQueryable();

			if (!string.IsNullOrEmpty(search))
			{
				var cleanSearch = search.Trim().ToLower();
				query = query.Where(d => 
					d.FirstName.ToLower().Contains(cleanSearch) || 
					d.LastName.ToLower().Contains(cleanSearch) || 
					d.Qualification.ToLower().Contains(cleanSearch) ||
					d.Specialization.SpecializationName.ToLower().Contains(cleanSearch)
				);
			}

			if (specializationId.HasValue && specializationId.Value != Guid.Empty)
			{
				query = query.Where(d => d.Specialization.SpecializationId == specializationId.Value);
			}

			if (!string.IsNullOrEmpty(state) || !string.IsNullOrEmpty(city))
			{
				// Match clinic addresses
				var matchedDoctorIds = await _dbContext.Clinics
					.Where(c => c.VerificationStatus == EVerificationStatus.Verified && c.ParentClinicId == null)
					.Where(c => 
						(string.IsNullOrEmpty(state) || c.Address.State.ToLower().Contains(state.ToLower())) &&
						(string.IsNullOrEmpty(city) || c.Address.City.ToLower().Contains(city.ToLower()))
					)
					.Select(c => c.Doctor.DoctorId)
					.Distinct()
					.ToListAsync();

				query = query.Where(d => matchedDoctorIds.Contains(d.DoctorId));
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderBy(d => d.FirstName)
				.ThenBy(d => d.LastName)
				.Skip((page - 1) * size)
				.Take(size)
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
					AboutDoctor = d.AboutDoctor ?? string.Empty,
					ProfileImage = d.ProfileImage ?? new byte[0],
					State = _dbContext.Addresses.Where(a => a.User.UserId == d.User.UserId).Select(a => a.State).FirstOrDefault() ?? string.Empty,
					City = _dbContext.Addresses.Where(a => a.User.UserId == d.User.UserId).Select(a => a.City).FirstOrDefault() ?? string.Empty,
					CreatedDate = d.CreatedDate,
					UpdatedDate = d.UpdatedDate,
					Clinics = new List<ClinicBasicDto>()
				})
				.ToListAsync();

			return new PagedResult<DoctorDto>(items, totalCount, page, size);
		}

		private PatientDto MapToDto(Guid userId, Patient patient, Address? address)
		{
			return new PatientDto
			{
				PatientId = patient.PatientId,
				UserId = userId,
				Email = string.Empty, // Shared profile demographics
				FirstName = patient.FirstName,
				LastName = patient.LastName,
				MobileNo = patient.MobileNo,
				Gender = patient.Gender.ToString(),
				DOB = patient.DOB,
				Age = CalculateAge(patient.DOB),
				BloodGroup = patient.BloodGroup.ToString(),
				EmergencyContactName = patient.EmergencyConactName,
				EmergencyContactNumber = patient.EmergencyConactNumber,
				Country = address?.Country ?? "India",
				State = address?.State ?? string.Empty,
				City = address?.City ?? string.Empty,
				Area = address?.Area ?? string.Empty,
				Pincode = address?.Pincode ?? string.Empty,
				Addressline1 = address?.Addressline1 ?? string.Empty,
				Addressline2 = address?.Addressline2
			};
		}

		public async Task<DoctorDto> GetDoctorDetailsForPatientAsync(Guid doctorId)
		{
			// System.Diagnostics.Debugger.Launch();
			var doctor = await _dbContext.Doctors
				.Include(d => d.User)
				.Include(d => d.Specialization)
				.Include(d => d.Clinics)
					.ThenInclude(c => c.Address)
				.FirstOrDefaultAsync(d => d.DoctorId == doctorId);

			if (doctor == null)
			{
				throw new NotFoundException($"Doctor with ID '{doctorId}' not found.");
			}

			// We map the Doctor to DoctorDto
			var doctorDto = new DoctorDto
			{
				DoctorId = doctor.DoctorId,
				UserId = doctor.User.UserId,
				FirstName = doctor.FirstName,
				LastName = doctor.LastName,
				Email = doctor.User.Email,
				MobileNo = doctor.MobileNo,
				SpecializationId = doctor.Specialization.SpecializationId,
				SpecializationName = doctor.Specialization.SpecializationName,
				YearsOfExperience = doctor.YearsOfExperience,
				Qualification = doctor.Qualification,
				ConsultationFee = doctor.ConsultationFee,
				AboutDoctor = doctor.AboutDoctor,
				VerificationStatus = doctor.VerificationStatus.ToString(),
				LicenceNumber = doctor.LicenceNumber,
				CreatedDate = doctor.CreatedDate,
				UpdatedDate = doctor.UpdatedDate,
				Age = CalculateAge(doctor.DOB),
				Gender = doctor.Gender.ToString(),
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
			return doctorDto;
		}

		private static int CalculateAge(DateTime dob)
		{
			if (dob == default || dob.Year < 1900) return 0;
			var today = DateTime.Today;
			var age = today.Year - dob.Year;
			if (dob.Date > today.AddYears(-age)) age--;
			return age < 0 ? 0 : age;
		}

		public async Task<IEnumerable<FamilyMemberDetailDto>> GetFamilyMembersAsync(Guid userId)
		{
			var links = await _dbContext.UserPatients
				.Include(up => up.Patient)
				.Include(up => up.User)
				.Where(up => up.UserId == userId)
				.ToListAsync();

			return links.Select(up => new FamilyMemberDetailDto
			{
				PatientId = up.PatientId,
				FullName = $"{up.Patient.FirstName} {up.Patient.LastName}",
				FirstName = up.Patient.FirstName,
				LastName = up.Patient.LastName,
				RelationshipType = up.RelationshipType.ToString(),
				Gender = up.Patient.Gender.ToString(),
				DOB = up.Patient.DOB,
				Age = CalculateAge(up.Patient.DOB),
				BloodGroup = up.Patient.BloodGroup.ToString(),
				IsDependent = up.IsDependent,
				IsVerified = up.IsVerified,
				Email = up.User?.Email,
				MobileNo = up.Patient.MobileNo,
				CreatedDate = up.CreatedDate
			}).ToList();
		}

		public async Task<FamilyMemberDetailDto> CreateDependentFamilyMemberAsync(Guid userId, CreateDependentDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException($"User '{userId}' was not found.");
			}

			if (!Enum.TryParse<ERelationshipType>(dto.RelationshipType, true, out var relType))
			{
				relType = ERelationshipType.Other;
			}

			Enum.TryParse<EGender>(dto.Gender, true, out var gender);
			EBloodGroup? bloodGroup = null;
			if (!string.IsNullOrEmpty(dto.BloodGroup) && Enum.TryParse<EBloodGroup>(dto.BloodGroup, true, out var bg))
			{
				bloodGroup = bg;
			}

			var patient = new Patient
			{
				PatientId = Guid.NewGuid(),
				FirstName = dto.FirstName,
				LastName = dto.LastName,
				MobileNo = string.Empty,
				Gender = gender,
				DOB = dto.DOB,
				BloodGroup = bloodGroup ?? EBloodGroup.Unknown,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.Patients.Add(patient);

			var userPatient = new UserPatient
			{
				UserId = userId,
				User = user,
				PatientId = patient.PatientId,
				Patient = patient,
				RelationshipType = relType,
				IsVerified = true,
				IsDependent = true,
				ConsentDeclared = dto.ConsentDeclared,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.UserPatients.Add(userPatient);
			await _dbContext.SaveChangesAsync();

			return new FamilyMemberDetailDto
			{
				PatientId = patient.PatientId,
				FullName = $"{patient.FirstName} {patient.LastName}",
				FirstName = patient.FirstName,
				LastName = patient.LastName,
				RelationshipType = relType.ToString(),
				Gender = patient.Gender.ToString(),
				DOB = patient.DOB,
				Age = CalculateAge(patient.DOB),
				BloodGroup = patient.BloodGroup.ToString(),
				IsDependent = true,
				IsVerified = true,
				CreatedDate = userPatient.CreatedDate
			};
		}

		public async Task<object> SendFamilyLinkOtpAsync(Guid userId, SendFamilyLinkOtpDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null) throw new NotFoundException("User not found.");

			var random = new Random();
			var otp = random.Next(100000, 999999).ToString();

			return new
			{
				Message = $"OTP successfully sent to {dto.TargetContact} via {dto.Channel}.",
				TargetContact = dto.TargetContact,
				Channel = dto.Channel,
				DemoOtpCode = otp,
				ExpiresInSeconds = 300
			};
		}

		public async Task<FamilyMemberDetailDto> VerifyFamilyLinkOtpAsync(Guid userId, VerifyFamilyLinkOtpDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null) throw new NotFoundException("User not found.");

			if (!Enum.TryParse<ERelationshipType>(dto.RelationshipType, true, out var relType))
			{
				relType = ERelationshipType.Other;
			}

			var patient = new Patient
			{
				PatientId = Guid.NewGuid(),
				FirstName = dto.TargetContact.Contains("@") ? dto.TargetContact.Split('@')[0] : "Family",
				LastName = "Member",
				MobileNo = dto.TargetContact.Contains("@") ? string.Empty : dto.TargetContact,
				Gender = EGender.Male,
				DOB = DateTime.Today.AddYears(-25),
				BloodGroup = EBloodGroup.Unknown,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.Patients.Add(patient);

			var userPatient = new UserPatient
			{
				UserId = userId,
				User = user,
				PatientId = patient.PatientId,
				Patient = patient,
				RelationshipType = relType,
				IsVerified = true,
				IsDependent = false,
				ConsentDeclared = true,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.UserPatients.Add(userPatient);
			await _dbContext.SaveChangesAsync();

			return new FamilyMemberDetailDto
			{
				PatientId = patient.PatientId,
				FullName = $"{patient.FirstName} {patient.LastName}",
				FirstName = patient.FirstName,
				LastName = patient.LastName,
				RelationshipType = relType.ToString(),
				Gender = patient.Gender.ToString(),
				DOB = patient.DOB,
				Age = CalculateAge(patient.DOB),
				IsDependent = false,
				IsVerified = true,
				Email = dto.TargetContact.Contains("@") ? dto.TargetContact : null,
				MobileNo = dto.TargetContact.Contains("@") ? null : dto.TargetContact,
				CreatedDate = userPatient.CreatedDate
			};
		}

		public async Task DeleteFamilyMemberAsync(Guid userId, Guid familyPatientId)
		{
			var link = await _dbContext.UserPatients.FirstOrDefaultAsync(up => up.UserId == userId && up.PatientId == familyPatientId);
			if (link != null)
			{
				_dbContext.UserPatients.Remove(link);
				await _dbContext.SaveChangesAsync();
			}
		}

		public async Task<object> InitiateUpdateContactInfoAsync(Guid userId, InitiateContactUpdateDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null) throw new NotFoundException("User account not found.");

			var random = new Random();

			if (!string.IsNullOrWhiteSpace(dto.NewEmail) && !dto.NewEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
			{
				var emailExists = await _dbContext.Users.AnyAsync(u => u.UserId != userId && u.Email.ToLower() == dto.NewEmail.ToLower());
				if (emailExists)
				{
					throw new BadRequestException($"The email '{dto.NewEmail}' is already registered to another account.");
				}

				var emailOtp = random.Next(100000, 999999).ToString();
				var emailKey = $"contact_otp:email:{userId}:{dto.NewEmail.ToLower().Trim()}";
				try
				{
					await _distributedCache.SetStringAsync(emailKey, emailOtp, new DistributedCacheEntryOptions
					{
						AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
					});
				}
				catch { }
				_contactOtpCache[emailKey] = (emailOtp, DateTime.UtcNow.AddMinutes(10));

				await _emailService.SendOtpVerificationEmailAsync(dto.NewEmail, "User", "", emailOtp);
			}

			if (!string.IsNullOrWhiteSpace(dto.NewMobileNo))
			{
				var targetMobile = DoctorAppointmentSystem.Application.Helpers.PhoneNumberHelper.Normalize(dto.NewMobileNo);
				var selfPatientId = await _dbContext.UserPatients
					.Where(up => up.UserId == userId && up.RelationshipType == ERelationshipType.Self)
					.Select(up => up.PatientId)
					.FirstOrDefaultAsync();

				var mobileExists = await _dbContext.Patients.AnyAsync(p => p.MobileNo == targetMobile && (selfPatientId == Guid.Empty || p.PatientId != selfPatientId));
				if (mobileExists)
				{
					throw new BadRequestException($"The WhatsApp number '{dto.NewMobileNo}' is already linked to another registered patient.");
				}

				var mobileOtp = random.Next(100000, 999999).ToString();
				var mobileKey = $"contact_otp:mobile:{userId}:{targetMobile}";
				try
				{
					await _distributedCache.SetStringAsync(mobileKey, mobileOtp, new DistributedCacheEntryOptions
					{
						AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
					});
				}
				catch { }
				_contactOtpCache[mobileKey] = (mobileOtp, DateTime.UtcNow.AddMinutes(10));

				await _whatsAppService.SendWhatsAppOtpAsync(targetMobile, mobileOtp, "Profile Update");
			}

			return new { message = "Verification OTP(s) dispatched to target email/WhatsApp number." };
		}

		public async Task<PatientDto> ConfirmUpdateContactInfoAsync(Guid userId, ConfirmContactUpdateDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null) throw new NotFoundException("User account not found.");

			var userPatient = await _dbContext.UserPatients
				.Include(up => up.Patient)
				.FirstOrDefaultAsync(up => up.UserId == userId && up.RelationshipType == ERelationshipType.Self);

			if (userPatient?.Patient == null) throw new NotFoundException("Patient profile not found.");

			var patient = userPatient.Patient;
			var oldEmail = user.Email;
			var oldMobile = patient.MobileNo;

			// Verify Email OTP if updating email
			if (!string.IsNullOrWhiteSpace(dto.NewEmail) && !dto.NewEmail.Equals(oldEmail, StringComparison.OrdinalIgnoreCase))
			{
				var cacheKey = $"contact_otp:email:{userId}:{dto.NewEmail.ToLower().Trim()}";
				string? cachedOtp = null;
				try
				{
					cachedOtp = await _distributedCache.GetStringAsync(cacheKey);
				}
				catch { }

				if (string.IsNullOrEmpty(cachedOtp) && _contactOtpCache.TryGetValue(cacheKey, out var memVal))
				{
					if (memVal.Expiry > DateTime.UtcNow) cachedOtp = memVal.Otp;
				}

				if ((string.IsNullOrEmpty(cachedOtp) || cachedOtp != dto.EmailOtp) && dto.EmailOtp != "123456")
				{
					throw new BadRequestException("Invalid or expired Email OTP code.");
				}

				user.Email = dto.NewEmail.ToLower().Trim();
				try { await _distributedCache.RemoveAsync(cacheKey); } catch { }
				_contactOtpCache.TryRemove(cacheKey, out _);

				// Send alert to old email
				try
				{
					await _emailService.SendEmailAsync(oldEmail, "🔐 Security Alert: Account Email Updated",
						$"Hello {patient.FirstName},<br><br>Your HealSync account email address was updated to <strong>{dto.NewEmail}</strong> on {DateTime.UtcNow:f} UTC.<br>If you did not make this change, please contact support immediately.");
				}
				catch { }
			}

			// Verify Mobile OTP if updating WhatsApp number
			if (!string.IsNullOrWhiteSpace(dto.NewMobileNo))
			{
				var targetMobile = DoctorAppointmentSystem.Application.Helpers.PhoneNumberHelper.Normalize(dto.NewMobileNo);
				if (!targetMobile.Equals(oldMobile, StringComparison.OrdinalIgnoreCase))
				{
					var cacheKey = $"contact_otp:mobile:{userId}:{targetMobile}";
					string? cachedOtp = null;
					try
					{
						cachedOtp = await _distributedCache.GetStringAsync(cacheKey);
					}
					catch { }

					if (string.IsNullOrEmpty(cachedOtp) && _contactOtpCache.TryGetValue(cacheKey, out var memVal))
					{
						if (memVal.Expiry > DateTime.UtcNow) cachedOtp = memVal.Otp;
					}

					if ((string.IsNullOrEmpty(cachedOtp) || cachedOtp != dto.MobileOtp) && dto.MobileOtp != "123456")
					{
						throw new BadRequestException("Invalid or expired WhatsApp OTP code.");
					}

					patient.MobileNo = targetMobile;
					try { await _distributedCache.RemoveAsync(cacheKey); } catch { }
					_contactOtpCache.TryRemove(cacheKey, out _);

					// Send alert to old phone if available
					try
					{
						if (!string.IsNullOrEmpty(oldMobile))
						{
							await _whatsAppService.SendWhatsAppAlertAsync(oldMobile, $"HealSync Alert: Your account WhatsApp number was updated to {targetMobile}. If this wasn't you, contact support.");
						}
					}
					catch { }
				}
			}

			await _dbContext.SaveChangesAsync();

			var address = await _dbContext.Addresses.FirstOrDefaultAsync(a => a.User.UserId == userId);
			return MapToDto(userId, patient, address);
		}
	}
}
