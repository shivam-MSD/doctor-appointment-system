using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
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
		private readonly IEmailService _emailService;
		private readonly INotificationService _notificationService;
		private readonly IConfiguration _configuration;

		public AuthService(
			ApplicationDbContext dbContext,
			IEmailService emailService,
			INotificationService notificationService,
			IConfiguration configuration)
		{
			_dbContext = dbContext;
			_emailService = emailService;
			_notificationService = notificationService;
			_configuration = configuration;
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
				IsEmailVerified = true,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.Users.Add(user);
			
			// Set the shadow RoleId foreign key property
			_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

			// 5. Initialize specialized profiles (Patient/Doctor) and map demographics
			Guid? profileId = null;
			if (parsedRole == ERole.Patient)
			{
				var patient = new Patient
				{
					PatientId = Guid.NewGuid(),
					FirstName = registerDto.FirstName,
					LastName = registerDto.LastName,
					MobileNo = registerDto.MobileNo,
					Gender = EGender.Male,
					DOB = DateTime.MinValue,
					EmergencyConactName = null,
					EmergencyConactNumber = null,
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.Patients.Add(patient);
				profileId = patient.PatientId;

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
					Gender = EGender.Male,
					DOB = DateTime.MinValue,
					Qualification = "",
					LicenceNumber = "",
					YearsOfExperience = 0,
					ConsultationFee = 0.0,
					VerificationStatus = EVerificationStatus.Pending,
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.Doctors.Add(doctor);
				profileId = doctor.DoctorId;
			}

			await _dbContext.SaveChangesAsync();

			// Force verification check immediately
			// await GenerateAndSendOtpAsync(user);

			return new AuthResponseDto
			{
				UserId = user.UserId,
				Email = user.Email,
				Role = parsedRole.ToString(),
				FirstName = registerDto.FirstName,
				LastName = registerDto.LastName,
				ProfileId = profileId,
				Token = GenerateJwtToken(user, parsedRole.ToString()),
				RefreshToken = Guid.NewGuid().ToString()
			};
		}

		public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
			if (user == null)
			{
				throw new BadRequestException("Incorrect email address. Account not found.");
			}

			var roleIdObj = _dbContext.Entry(user).Property("RoleId").CurrentValue;
			if (roleIdObj == null || !(roleIdObj is Guid roleId))
			{
				throw new BaseException("User role configuration error.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			var role = await _dbContext.Roles.FindAsync(roleId);
			var roleName = role?.Role.ToString() ?? "Patient";

			if (!string.IsNullOrEmpty(loginDto.Role) && !string.Equals(roleName, loginDto.Role, StringComparison.OrdinalIgnoreCase))
			{
				throw new UnauthorizedException("Unauthorized access. Invalid credentials for this portal.");
			}

			if (!VerifyPassword(loginDto.Password, user.PasswordHash))
			{
				throw new BadRequestException("Incorrect password. Please verify and try again.");
			}

			if (!user.IsActive)
			{
				throw new ForbiddenException("Your account is deactivated. Please contact support.");
			}

			// Block unverified email logins (except seeded admins/superadmins)
			// if (!user.IsEmailVerified && user.Email != "admin@doctorapp.com" && user.Email != "superadmin@doctorapp.com")
			// {
			// 	await GenerateAndSendOtpAsync(user);
			// }

			string firstName = "User";
			string lastName = "";
			Guid? profileId = null;

			if (role?.Role == ERole.Patient)
			{
				var userPatient = await _dbContext.UserPatients
					.Include(up => up.Patient)
					.FirstOrDefaultAsync(up => up.UserId == user.UserId && up.RelationshipType == ERelationshipType.Self);
				if (userPatient?.Patient != null)
				{
					firstName = userPatient.Patient.FirstName;
					lastName = userPatient.Patient.LastName;
					profileId = userPatient.Patient.PatientId;
				}
			}
			else if (role?.Role == ERole.Doctor)
			{
				var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == user.UserId);
				if (doctor != null)
				{
					if (doctor.VerificationStatus != EVerificationStatus.Verified)
					{
						throw new ForbiddenException($"Your doctor account is '{doctor.VerificationStatus}'. You can only log in once verified and approved by the Super Admin.");
					}
					firstName = doctor.FirstName;
					lastName = doctor.LastName;
					profileId = doctor.DoctorId;
				}
			}
			else if (role?.Role == ERole.Admin)
			{
				var adminObj = await _dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == user.UserId);
				if (adminObj != null)
				{
					if (!adminObj.IsVerified)
					{
						throw new ForbiddenException("Your Clinic Admin account is pending verification by the Super Admin. Please wait for approval.");
					}
					firstName = adminObj.FirstName;
					lastName = adminObj.LastName;
					profileId = adminObj.AdminId;
				}
				else
				{
					firstName = "Clinic";
					lastName = "Admin";
				}
			}
			else if (role?.Role == ERole.SuperAdmin)
			{
				firstName = "Super";
				lastName = "Admin";
			}
			else
			{
				firstName = "System";
				lastName = "User";
			}

			user.LastLoginDate = DateTime.UtcNow;
			await _dbContext.SaveChangesAsync();

			return new AuthResponseDto
			{
				UserId = user.UserId,
				Email = user.Email,
				Role = roleName,
				FirstName = firstName,
				LastName = lastName,
				ProfileId = profileId,
				Token = GenerateJwtToken(user, roleName),
				RefreshToken = Guid.NewGuid().ToString()
			};
		}

		public async Task<AuthResponseDto> RegisterDoctorAsync(DoctorSignUpDto dto)
		{
			var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == dto.Email);
			if (emailExists)
			{
				throw new EmailAlreadyExistsException(dto.Email);
			}

			var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == ERole.Doctor);
			if (role == null)
			{
				role = new Roles
				{
					RoleId = Guid.NewGuid(),
					Role = ERole.Doctor
				};
				_dbContext.Roles.Add(role);
				await _dbContext.SaveChangesAsync();
			}

			var user = new User
			{
				UserId = Guid.NewGuid(),
				Email = dto.Email,
				PasswordHash = HashPassword(dto.Password),
				IsActive = true,
				IsEmailVerified = true,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.Users.Add(user);
			_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

			var specialization = await _dbContext.Specializations.FindAsync(dto.SpecializationId);
			if (specialization == null)
			{
				specialization = await _dbContext.Specializations.FirstOrDefaultAsync();
				if (specialization == null)
				{
					specialization = new Specialization
					{
						SpecializationId = Guid.NewGuid(),
						SpecializationName = "General Physician"
					};
					_dbContext.Specializations.Add(specialization);
					await _dbContext.SaveChangesAsync();
				}
			}

			var doctor = new Doctor
			{
				DoctorId = Guid.NewGuid(),
				User = user,
				Specialization = specialization,
				FirstName = dto.FirstName,
				LastName = dto.LastName,
				MobileNo = dto.MobileNo,
				Gender = Enum.TryParse<EGender>(dto.Gender, true, out var genderEnum) ? genderEnum : EGender.Male,
				DOB = dto.DOB,
				Qualification = dto.Qualification,
				LicenceNumber = dto.LicenceNumber,
				YearsOfExperience = dto.YearsOfExperience,
				ConsultationFee = dto.ConsultationFee,
				VerificationStatus = EVerificationStatus.Pending,
				CreatedDate = DateTime.UtcNow
			};
			_dbContext.Doctors.Add(doctor);

			await _dbContext.SaveChangesAsync();

			// Trigger notification to SuperAdmins
			await _notificationService.CreateNotificationForRoleAsync("SuperAdmin", $"New doctor Dr. {dto.FirstName} {dto.LastName} has registered and requires profile verification.");
			await _notificationService.SendRefreshSignalAsync("Doctors");

			// Force verification check immediately
			// await GenerateAndSendOtpAsync(user);

			return new AuthResponseDto
			{
				UserId = user.UserId,
				Email = user.Email,
				Role = ERole.Doctor.ToString(),
				FirstName = dto.FirstName,
				LastName = dto.LastName,
				ProfileId = doctor.DoctorId,
				Token = GenerateJwtToken(user, ERole.Doctor.ToString()),
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

		private string GenerateJwtToken(User user, string role)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."));

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
					new Claim(ClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, role)
				}),
				Expires = DateTime.UtcNow.AddDays(7),
				Issuer = _configuration["Jwt:Issuer"],
				Audience = _configuration["Jwt:Audience"],
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		private async Task GenerateAndSendOtpAsync(User user)
		{
			var otp = new Random().Next(100000, 999999).ToString();
			user.EmailVerificationOtp = otp;
			user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15);
			user.IsEmailVerified = false;
			await _dbContext.SaveChangesAsync();

			try
			{
				var subject = "HealSync Email Verification Code";
				var body = $@"
					<div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #ffffff;'>
						<h2 style='color: #06b6d4; text-align: center;'>HealSync Verification</h2>
						<hr style='border: none; border-top: 1px solid #eeeeee;' />
						<p>Hello,</p>
						<p>Thank you for signing up with HealSync. Please use the following 6-digit one-time passcode (OTP) to verify your email address and activate your profile:</p>
						<div style='text-align: center; margin: 32px 0;'>
							<span style='font-size: 2.2rem; font-weight: bold; letter-spacing: 6px; padding: 12px 24px; background-color: #f3f4f6; border-radius: 8px; color: #111827; border: 1px solid #e5e7eb;'>{otp}</span>
						</div>
						<p style='color: #6b7280; font-size: 0.85rem;'>This OTP is valid for 15 minutes. Please do not share this code with anyone.</p>
						<hr style='border: none; border-top: 1px solid #eeeeee;' />
						<p style='font-size: 0.8rem; color: #9ca3af; text-align: center;'>HealSync Medical Network App</p>
					</div>";

				await _emailService.SendEmailAsync(user.Email, subject, body);
				Console.WriteLine($"[EMAIL SENDER] Sent real email OTP {otp} to {user.Email}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[EMAIL ERROR] Failed to send real email to {user.Email}. Error: {ex.Message}");
				Console.WriteLine($"[EMAIL FALLBACK] Sent simulated OTP {otp} to {user.Email}");
			}

			throw new EmailVerificationRequiredException(user.Email);
		}

		public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
			if (user == null)
			{
				throw new NotFoundException("User was not found.");
			}

			if (user.EmailVerificationOtp != dto.Otp || user.EmailVerificationOtpExpiry < DateTime.UtcNow)
			{
				throw new BadRequestException("Invalid or expired OTP code.");
			}

			user.IsEmailVerified = true;
			user.EmailVerificationOtp = null;
			user.EmailVerificationOtpExpiry = null;
			await _dbContext.SaveChangesAsync();

			// Construct AuthResponseDto on success
			var roleIdObj = _dbContext.Entry(user).Property("RoleId").CurrentValue;
			if (roleIdObj == null || !(roleIdObj is Guid roleId))
			{
				throw new BaseException("User role configuration error.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			var role = await _dbContext.Roles.FindAsync(roleId);
			var roleName = role?.Role.ToString() ?? "Patient";

			string firstName = "User";
			string lastName = "";
			Guid? profileId = null;

			if (role?.Role == ERole.Patient)
			{
				var userPatient = await _dbContext.UserPatients
					.Include(up => up.Patient)
					.FirstOrDefaultAsync(up => up.UserId == user.UserId && up.RelationshipType == ERelationshipType.Self);
				if (userPatient?.Patient != null)
				{
					firstName = userPatient.Patient.FirstName;
					lastName = userPatient.Patient.LastName;
					profileId = userPatient.Patient.PatientId;
				}
			}
			else if (role?.Role == ERole.Doctor)
			{
				var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == user.UserId);
				if (doctor != null)
				{
					firstName = doctor.FirstName;
					lastName = doctor.LastName;
					profileId = doctor.DoctorId;
				}
			}
			else if (role?.Role == ERole.Admin)
			{
				var adminObj = await _dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == user.UserId);
				if (adminObj != null)
				{
					firstName = adminObj.FirstName;
					lastName = adminObj.LastName;
					profileId = adminObj.AdminId;
				}
			}

			user.LastLoginDate = DateTime.UtcNow;
			await _dbContext.SaveChangesAsync();

			return new AuthResponseDto
			{
				UserId = user.UserId,
				Email = user.Email,
				Role = roleName,
				FirstName = firstName,
				LastName = lastName,
				ProfileId = profileId,
				Token = GenerateJwtToken(user, roleName),
				RefreshToken = Guid.NewGuid().ToString()
			};
		}
		#endregion
	}
}
