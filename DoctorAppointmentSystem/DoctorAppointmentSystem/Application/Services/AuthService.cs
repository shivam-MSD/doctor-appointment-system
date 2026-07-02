using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class AuthService : IAuthService
	{
		private readonly ApplicationDbContext _dbContext;

		public AuthService(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
		{
			// 1. Check if email already exists
			var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == registerDto.Email);
			if (emailExists)
			{
				throw new EmailAlreadyExistsException(registerDto.Email);
			}

			// 2. Parse and validate role
			if (!Enum.TryParse<ERole>(registerDto.Role, true, out var parsedRole))
			{
				throw new BadRequestException($"Role '{registerDto.Role}' is invalid. Allowed roles are: Doctor, Patient, Admin.");
			}

			// 3. Find or create the Role in database
			var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == parsedRole);
			if (role == null)
			{
				role = new Roles
				{
					RoleId = Guid.NewGuid(),
					Role = parsedRole
				};
				_dbContext.Roles.Add(role);
				await _dbContext.SaveChangesAsync();
			}

			// 4. Create the User credentials entity (decoupled from demographics)
			var user = new User
			{
				UserId = Guid.NewGuid(),
				Email = registerDto.Email,
				PasswordHash = HashPassword(registerDto.Password),
				IsActive = true,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.Users.Add(user);
			
			// Set the shadow RoleId foreign key property
			_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

			// 5. Initialize specialized profiles (Patient/Doctor) and map demographics
			if (parsedRole == ERole.Patient)
			{
				var patient = new Patient
				{
					PatientId = Guid.NewGuid(),
					FirstName = registerDto.FirstName,
					LastName = registerDto.LastName,
					MobileNo = registerDto.MobileNo,
					Gender = "Not Set",
					DOB = DateTime.MinValue,
					EmergencyConactName = "Not Set",
					EmergencyConactNumber = "Not Set",
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.Patients.Add(patient);

				// Create Many-to-Many link (Self)
				var userPatient = new UserPatient
				{
					User = user,
					Patient = patient,
					RelationshipType = ERelationshipType.Self,
					IsVerified = true, // Primary account is verified by default
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.UserPatients.Add(userPatient);
			}
			else if (parsedRole == ERole.Doctor)
			{
				var defaultSpecialization = await _dbContext.Specializations.FirstOrDefaultAsync();
				if (defaultSpecialization == null)
				{
					defaultSpecialization = new Specialization
					{
						SpecializationId = Guid.NewGuid(),
						SpecializationName = "General Physician"
					};
					_dbContext.Specializations.Add(defaultSpecialization);
					await _dbContext.SaveChangesAsync();
				}

				var doctor = new Doctor
				{
					DoctorId = Guid.NewGuid(),
					User = user,
					Specialization = defaultSpecialization,
					FirstName = registerDto.FirstName,
					LastName = registerDto.LastName,
					MobileNo = registerDto.MobileNo,
					Gender = "Not Set",
					DOB = DateTime.MinValue,
					Qualification = "Not Set",
					LicenceNumber = "Not Set",
					HospitalName = "Not Set",
					YearsOfExperience = 0,
					ConsultationFee = 0.0,
					VerificationStatus = EVerificationStatus.Pending,
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.Doctors.Add(doctor);
			}

			await _dbContext.SaveChangesAsync();

			return new AuthResponseDto
			{
				UserId = user.UserId,
				Email = user.Email,
				Role = parsedRole.ToString(),
				Token = GenerateMockJwtToken(user.Email, parsedRole.ToString()),
				RefreshToken = Guid.NewGuid().ToString()
			};
		}

		public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
			if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
			{
				throw new InvalidCredentialsException();
			}

			if (!user.IsActive)
			{
				throw new ForbiddenException("Your account is deactivated. Please contact support.");
			}

			var roleIdObj = _dbContext.Entry(user).Property("RoleId").CurrentValue;
			if (roleIdObj == null || !(roleIdObj is Guid roleId))
			{
				throw new BaseException("User role configuration error.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			var role = await _dbContext.Roles.FindAsync(roleId);
			var roleName = role?.Role.ToString() ?? "Patient";

			user.LastLoginDate = DateTime.UtcNow;
			await _dbContext.SaveChangesAsync();

			return new AuthResponseDto
			{
				UserId = user.UserId,
				Email = user.Email,
				Role = roleName,
				Token = GenerateMockJwtToken(user.Email, roleName),
				RefreshToken = Guid.NewGuid().ToString()
			};
		}

		#region Helper Hashing Methods
		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}

		private bool VerifyPassword(string password, string passwordHash)
		{
			return HashPassword(password) == passwordHash;
		}

		private string GenerateMockJwtToken(string email, string role)
		{
			return $"mock-jwt-token-for-{email}-role-{role}-{DateTime.UtcNow.AddHours(2).Ticks}";
		}
		#endregion
	}
}
