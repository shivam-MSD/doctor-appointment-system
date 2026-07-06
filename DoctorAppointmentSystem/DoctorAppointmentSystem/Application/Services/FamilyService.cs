using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class FamilyService : IFamilyService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IDistributedCache _distributedCache;

		public FamilyService(ApplicationDbContext dbContext, IDistributedCache distributedCache)
		{
			_dbContext = dbContext;
			_distributedCache = distributedCache;
		}

		public async Task<Guid> InitiateAddFamilyMemberAsync(Guid userId, AddFamilyMemberDto dto)
		{
			// 1. Validate relationship type
			if (!Enum.TryParse<ERelationshipType>(dto.RelationshipType, true, out var relType))
			{
				throw new BadRequestException($"RelationshipType '{dto.RelationshipType}' is invalid. Allowed: Spouse, Child, Parent, Grandparent, Sibling, Other.");
			}

			// 2. Validate User exists
			var userExists = await _dbContext.Users.AnyAsync(u => u.UserId == userId);
			if (!userExists)
			{
				throw new NotFoundException($"User with ID '{userId}' was not found.");
			}

			// 3. Generate a secure random OTP (Simulated 4-digit code)
			var random = new Random();
			var otpCode = random.Next(1000, 9999).ToString();

			// 4. Serialize pending patient details
			var patientJsonData = new
			{
				dto.FirstName,
				dto.LastName,
				dto.MobileNo,
				dto.Gender,
				dto.DOB
			};

			var verificationId = Guid.NewGuid();

			var cacheData = new PendingVerificationCache
			{
				UserId = userId,
				MobileNo = dto.MobileNo,
				OtpCode = otpCode,
				RelationshipType = relType,
				PatientDataJson = JsonSerializer.Serialize(patientJsonData)
			};

			// 5. Save in Distributed Cache (Redis) with 10 minutes sliding/absolute expiration
			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
			};

			await _distributedCache.SetStringAsync(
				verificationId.ToString(),
				JsonSerializer.Serialize(cacheData),
				cacheOptions
			);

			// Output OTP code to console logs for developer testing
			Console.WriteLine($"[SMS GATEWAY] Sending OTP {otpCode} to {dto.MobileNo} for verification (VerificationId: {verificationId}).");

			return verificationId;
		}

		public async Task<PatientDto> VerifyFamilyMemberOtpAsync(Guid userId, VerifyFamilyOtpDto dto)
		{
			// 1. Retrieve pending verification from Cache
			var cacheStr = await _distributedCache.GetStringAsync(dto.VerificationId.ToString());
			if (string.IsNullOrEmpty(cacheStr))
			{
				throw new NotFoundException("Verification session not found or has expired.");
			}

			var cacheData = JsonSerializer.Deserialize<PendingVerificationCache>(cacheStr);
			if (cacheData == null)
			{
				throw new BaseException("Error deserializing cached verification details.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			// 2. Verify OTP code
			if (cacheData.OtpCode != dto.OtpCode)
			{
				throw new BadRequestException("The OTP code entered is incorrect.");
			}

			// 3. Parse pending patient demographic details
			var pendingData = JsonSerializer.Deserialize<PendingPatientData>(cacheData.PatientDataJson);
			if (pendingData == null)
			{
				throw new BaseException("Error deserializing family member details.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			// 4. Check if patient profile with this mobile already exists in DB
			var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.MobileNo == cacheData.MobileNo);

			if (patient != null)
			{
				// Check if already linked to this User
				var alreadyLinked = await _dbContext.UserPatients.AnyAsync(up => up.UserId == userId && up.PatientId == patient.PatientId);
				if (alreadyLinked)
				{
					await _distributedCache.RemoveAsync(dto.VerificationId.ToString());
					throw new ConflictException($"Patient profile with mobile {cacheData.MobileNo} is already linked to your family.");
				}
			}
			else
			{
				// Create a new Patient profile
				patient = new Patient
				{
					PatientId = Guid.NewGuid(),
					FirstName = pendingData.FirstName,
					LastName = pendingData.LastName,
					MobileNo = pendingData.MobileNo,
					Gender = Enum.TryParse<EGender>(pendingData.Gender, true, out var genderEnum) ? genderEnum : EGender.Male,
					DOB = pendingData.DOB,
					EmergencyConactName = null,
					EmergencyConactNumber = null,
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.Patients.Add(patient);
			}

			// 5. Link User to Patient in database many-to-many join table
			var link = new UserPatient
			{
				UserId = userId,
				Patient = patient,
				RelationshipType = cacheData.RelationshipType,
				IsVerified = true,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.UserPatients.Add(link);
			await _dbContext.SaveChangesAsync();

			// 6. Remove OTP session from distributed cache
			await _distributedCache.RemoveAsync(dto.VerificationId.ToString());

			return new PatientDto
			{
				PatientId = patient.PatientId,
				UserId = userId,
				Email = string.Empty,
				BloodGroup = patient.BloodGroup.ToString(),
				EmergencyContactName = patient.EmergencyConactName,
				EmergencyContactNumber = patient.EmergencyConactNumber
			};
		}

		public async Task<IEnumerable<PatientDto>> GetFamilyMembersAsync(Guid userId)
		{
			var links = await _dbContext.UserPatients
				.Include(up => up.Patient)
				.Where(up => up.UserId == userId && up.IsVerified)
				.ToListAsync();

			return links.Select(up => new PatientDto
			{
				PatientId = up.Patient.PatientId,
				UserId = up.UserId,
				Email = string.Empty,
				FirstName = up.Patient.FirstName,
				LastName = up.Patient.LastName,
				MobileNo = up.Patient.MobileNo,
				Gender = up.Patient.Gender.ToString(),
				DOB = up.Patient.DOB,
				BloodGroup = up.Patient.BloodGroup.ToString(),
				EmergencyContactName = up.Patient.EmergencyConactName,
				EmergencyContactNumber = up.Patient.EmergencyConactNumber
			});
		}

		#region Helper Cache Mapping Classes
		private class PendingVerificationCache
		{
			public Guid UserId { get; set; }
			public string MobileNo { get; set; }
			public string OtpCode { get; set; }
			public ERelationshipType RelationshipType { get; set; }
			public string PatientDataJson { get; set; }
		}

		private class PendingPatientData
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public string MobileNo { get; set; }
			public string Gender { get; set; }
			public DateTime DOB { get; set; }
		}
		#endregion
	}
}
