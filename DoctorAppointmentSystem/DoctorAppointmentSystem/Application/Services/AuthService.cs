using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class PendingRegistrationCacheItem
	{
		public string Email { get; set; }
		public string Otp { get; set; }
		public string Role { get; set; }
		public string RegistrationJson { get; set; } // Serialized RegisterDto or DoctorRegisterDto
	}

	public class AuthService : IAuthService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IEmailService _emailService;
		private readonly INotificationService _notificationService;
		private readonly IConfiguration _configuration;
		private readonly IDistributedCache _distributedCache;

		public AuthService(
			ApplicationDbContext dbContext,
			IEmailService emailService,
			INotificationService notificationService,
			IConfiguration configuration,
			IDistributedCache distributedCache)
		{
			_dbContext = dbContext;
			_emailService = emailService;
			_notificationService = notificationService;
			_configuration = configuration;
			_distributedCache = distributedCache;
		}

		public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
		{
			// 1. Check if email already exists in DB
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

			// 3. Generate verification OTP
			var otp = new Random().Next(100000, 999999).ToString();

			// 4. Save registration payload to Distributed Cache
			var cacheKey = $"pending_reg:{registerDto.Email.ToLower().Trim()}";
			var cacheItem = new PendingRegistrationCacheItem
			{
				Email = registerDto.Email.ToLower().Trim(),
				Otp = otp,
				Role = parsedRole.ToString(),
				RegistrationJson = JsonSerializer.Serialize(registerDto)
			};
			
			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
			};
			await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheItem), cacheOptions);

			// 5. Send OTP Email
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

			try
			{
				await _emailService.SendEmailAsync(registerDto.Email, subject, body);
				Console.WriteLine($"[EMAIL SENDER] Sent real email OTP {otp} to {registerDto.Email}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[EMAIL ERROR] Failed to send real email to {registerDto.Email}. Error: {ex.Message}");
				Console.WriteLine($"[EMAIL FALLBACK] Sent simulated OTP {otp} to {registerDto.Email}");
			}

			throw new EmailVerificationRequiredException(registerDto.Email);
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
			if (!user.IsEmailVerified && user.Email != "admin@doctorapp.com" && user.Email != "superadmin@doctorapp.com")
			{
				await GenerateAndSendOtpAsync(user);
			}

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
						throw new ForbiddenException("Your medical profile is currently under review by our administration team. You will be able to access the portal once your credentials have been verified and approved. An email notification will be sent to you as soon as this process is complete.");
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
				RefreshToken = Guid.NewGuid().ToString(),
				RequiresPasswordChange = user.RequiresPasswordChange
			};
		}

		public async Task<AuthResponseDto> RegisterDoctorAsync(DoctorSignUpDto dto)
		{
			// 1. Check if email already exists
			var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == dto.Email);
			if (emailExists)
			{
				throw new EmailAlreadyExistsException(dto.Email);
			}

			// 2. Generate verification OTP
			var otp = new Random().Next(100000, 999999).ToString();

			// 3. Save doctor registration payload to Distributed Cache
			var cacheKey = $"pending_reg:{dto.Email.ToLower().Trim()}";
			var cacheItem = new PendingRegistrationCacheItem
			{
				Email = dto.Email.ToLower().Trim(),
				Otp = otp,
				Role = ERole.Doctor.ToString(),
				RegistrationJson = JsonSerializer.Serialize(dto)
			};
			
			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
			};
			await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheItem), cacheOptions);

			// 4. Send OTP Email
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

			try
			{
				await _emailService.SendEmailAsync(dto.Email, subject, body);
				Console.WriteLine($"[EMAIL SENDER] Sent real email OTP {otp} to {dto.Email}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[EMAIL ERROR] Failed to send real email to {dto.Email}. Error: {ex.Message}");
				Console.WriteLine($"[EMAIL FALLBACK] Sent simulated OTP {otp} to {dto.Email}");
			}

			throw new EmailVerificationRequiredException(dto.Email);
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
			var cacheKey = $"pending_reg:{dto.Email.ToLower().Trim()}";
			var cachedData = await _distributedCache.GetStringAsync(cacheKey);

			if (cachedData != null)
			{
				var cacheItem = JsonSerializer.Deserialize<PendingRegistrationCacheItem>(cachedData);
				if (cacheItem == null || cacheItem.Otp != dto.Otp)
				{
					throw new BadRequestException("Invalid or expired OTP code.");
				}

				// Check again if email was registered meanwhile
				var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == cacheItem.Email);
				if (emailExists)
				{
					throw new EmailAlreadyExistsException(cacheItem.Email);
				}

				User user = null;
				Guid? profileId = null;
				string firstName = "";
				string lastName = "";
				string roleName = cacheItem.Role;

				if (cacheItem.Role == ERole.Patient.ToString())
				{
					var regDto = JsonSerializer.Deserialize<RegisterDto>(cacheItem.RegistrationJson);
					if (regDto == null) throw new BadRequestException("Invalid registration data.");

					var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == ERole.Patient);
					if (role == null)
					{
						role = new Roles { RoleId = Guid.NewGuid(), Role = ERole.Patient };
						_dbContext.Roles.Add(role);
					}

					user = new User
					{
						UserId = Guid.NewGuid(),
						Email = regDto.Email,
						PasswordHash = HashPassword(regDto.Password),
						IsActive = true,
						IsEmailVerified = true,
						CreatedDate = DateTime.UtcNow,
						LastLoginDate = DateTime.UtcNow
					};
					_dbContext.Users.Add(user);
					_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

					var patient = new Patient
					{
						PatientId = Guid.NewGuid(),
						FirstName = regDto.FirstName,
						LastName = regDto.LastName,
						MobileNo = regDto.MobileNo,
						Gender = EGender.Male,
						DOB = DateTime.MinValue,
						CreatedDate = DateTime.UtcNow
					};
					_dbContext.Patients.Add(patient);
					profileId = patient.PatientId;
					firstName = regDto.FirstName;
					lastName = regDto.LastName;

					var userPatient = new UserPatient
					{
						User = user,
						Patient = patient,
						RelationshipType = ERelationshipType.Self,
						IsVerified = true,
						CreatedDate = DateTime.UtcNow
					};
					_dbContext.UserPatients.Add(userPatient);
				}
				else if (cacheItem.Role == ERole.Doctor.ToString())
				{
					var docDto = JsonSerializer.Deserialize<DoctorSignUpDto>(cacheItem.RegistrationJson);
					if (docDto == null) throw new BadRequestException("Invalid doctor registration data.");

					var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == ERole.Doctor);
					if (role == null)
					{
						role = new Roles { RoleId = Guid.NewGuid(), Role = ERole.Doctor };
						_dbContext.Roles.Add(role);
					}

					var tempPassword = string.IsNullOrEmpty(docDto.Password)
						? Guid.NewGuid().ToString("N").Substring(0, 12)
						: docDto.Password;

					user = new User
					{
						UserId = Guid.NewGuid(),
						Email = docDto.Email,
						PasswordHash = HashPassword(tempPassword),
						IsActive = true,
						IsEmailVerified = true,
						CreatedDate = DateTime.UtcNow,
						LastLoginDate = DateTime.UtcNow
					};
					_dbContext.Users.Add(user);
					_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

					var specialization = await _dbContext.Specializations.FindAsync(docDto.SpecializationId);
					if (specialization == null)
					{
						specialization = await _dbContext.Specializations.FirstOrDefaultAsync() ?? new Specialization
						{
							SpecializationId = Guid.NewGuid(),
							SpecializationName = "General Physician"
						};
					}

					var doctor = new Doctor
					{
						DoctorId = Guid.NewGuid(),
						User = user,
						Specialization = specialization,
						FirstName = docDto.FirstName,
						LastName = docDto.LastName,
						MobileNo = docDto.MobileNo,
						Gender = Enum.TryParse<EGender>(docDto.Gender, true, out var genderEnum) ? genderEnum : EGender.Male,
						DOB = docDto.DOB,
						Qualification = docDto.Qualification,
						LicenceNumber = docDto.LicenceNumber,
						YearsOfExperience = docDto.YearsOfExperience,
						ConsultationFee = docDto.ConsultationFee,
						VerificationStatus = EVerificationStatus.Pending,
						CreatedDate = DateTime.UtcNow
					};
					_dbContext.Doctors.Add(doctor);
					profileId = doctor.DoctorId;
					firstName = docDto.FirstName;
					lastName = docDto.LastName;

					// Send Application Received email on email verification success
					var emailSubject = "HealSync - Doctor Onboarding Application Received";
					var emailBody = $@"
						<h3>Hello Dr. {doctor.FirstName} {doctor.LastName},</h3>
						<p>Thank you for verifying your email address.</p>
						<p>We have successfully received your medical onboarding application. Our administration team is currently verifying your credentials and medical licensing details.</p>
						<p>Once approved, your secure temporary password will be sent to this email address within 24-48 hours. You will then be able to log in and update your password.</p>
						<p>Best regards,<br/>HealSync Administration Team</p>";

					try
					{
						await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[Email Error]: Failed to send onboarding confirmation email: {ex.Message}");
					}
				}

				await _dbContext.SaveChangesAsync();

				// Evict cache item
				await _distributedCache.RemoveAsync(cacheKey);

				if (cacheItem.Role == ERole.Doctor.ToString())
				{
					// Trigger notification to SuperAdmins
					await _notificationService.CreateNotificationForRoleAsync("SuperAdmin", $"New doctor Dr. {firstName} {lastName} has registered and requires profile verification.");
					await _notificationService.SendRefreshSignalAsync("Doctors");
				}

				return new AuthResponseDto
				{
					UserId = user.UserId,
					Email = user.Email,
					Role = roleName,
					FirstName = firstName,
					LastName = lastName,
					ProfileId = profileId,
					Token = GenerateJwtToken(user, roleName),
					RefreshToken = Guid.NewGuid().ToString(),
					RequiresPasswordChange = user.RequiresPasswordChange
				};
			}

			var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
			if (dbUser == null)
			{
				throw new BadRequestException("Verification session has expired or is invalid. Please sign up again.");
			}

			if (dbUser.EmailVerificationOtp != dto.Otp || dbUser.EmailVerificationOtpExpiry < DateTime.UtcNow)
			{
				throw new BadRequestException("Invalid or expired OTP code.");
			}

			dbUser.IsEmailVerified = true;
			dbUser.EmailVerificationOtp = null;
			dbUser.EmailVerificationOtpExpiry = null;
			await _dbContext.SaveChangesAsync();

			// Construct AuthResponseDto on success
			var dbRoleIdObj = _dbContext.Entry(dbUser).Property("RoleId").CurrentValue;
			if (dbRoleIdObj == null || !(dbRoleIdObj is Guid dbRoleId))
			{
				throw new BaseException("User role configuration error.", System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
			}

			var dbRole = await _dbContext.Roles.FindAsync(dbRoleId);
			var dbRoleName = dbRole?.Role.ToString() ?? "Patient";

			string dbFirstName = "User";
			string dbLastName = "";
			Guid? dbProfileId = null;

			if (dbRole?.Role == ERole.Patient)
			{
				var userPatient = await _dbContext.UserPatients
					.Include(up => up.Patient)
					.FirstOrDefaultAsync(up => up.UserId == dbUser.UserId && up.RelationshipType == ERelationshipType.Self);
				if (userPatient?.Patient != null)
				{
					dbFirstName = userPatient.Patient.FirstName;
					dbLastName = userPatient.Patient.LastName;
					dbProfileId = userPatient.Patient.PatientId;
				}
			}
			else if (dbRole?.Role == ERole.Doctor)
			{
				var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == dbUser.UserId);
				if (doctor != null)
				{
					dbFirstName = doctor.FirstName;
					dbLastName = doctor.LastName;
					dbProfileId = doctor.DoctorId;

					// Send Application Received email on email verification success
					var emailSubject = "HealSync - Doctor Onboarding Application Received";
					var emailBody = $@"
						<h3>Hello Dr. {doctor.FirstName} {doctor.LastName},</h3>
						<p>Thank you for verifying your email address.</p>
						<p>We have successfully received your medical onboarding application. Our administration team is currently verifying your credentials and medical licensing details.</p>
						<p>Once approved, your secure temporary password will be sent to this email address within 24-48 hours. You will then be able to log in and update your password.</p>
						<p>Best regards,<br/>HealSync Administration Team</p>";

					try
					{
						await _emailService.SendEmailAsync(dbUser.Email, emailSubject, emailBody);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[Email Error]: Failed to send onboarding confirmation email: {ex.Message}");
					}
				}
			}
			else if (dbRole?.Role == ERole.Admin)
			{
				var adminObj = await _dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == dbUser.UserId);
				if (adminObj != null)
				{
					dbFirstName = adminObj.FirstName;
					dbLastName = adminObj.LastName;
					dbProfileId = adminObj.AdminId;
				}
			}

			dbUser.LastLoginDate = DateTime.UtcNow;
			await _dbContext.SaveChangesAsync();

			return new AuthResponseDto
			{
				UserId = dbUser.UserId,
				Email = dbUser.Email,
				Role = dbRoleName,
				FirstName = dbFirstName,
				LastName = dbLastName,
				ProfileId = dbProfileId,
				Token = GenerateJwtToken(dbUser, dbRoleName),
				RefreshToken = Guid.NewGuid().ToString(),
				RequiresPasswordChange = dbUser.RequiresPasswordChange
			};
		}
		#endregion

		#region Password Management Methods

		public async Task<string?> CheckEmailRoleAsync(string email)
		{
			if (string.IsNullOrWhiteSpace(email)) return null;
			
			var userWithRole = await _dbContext.Users
				.Where(u => u.Email == email.ToLower().Trim())
				.Select(u => new {
					User = u,
					RoleId = EF.Property<Guid>(u, "RoleId")
				})
				.FirstOrDefaultAsync();

			if (userWithRole == null) return null;

			var role = await _dbContext.Roles.FindAsync(userWithRole.RoleId);
			var roleName = role?.Role.ToString();

			if (roleName == "Doctor")
			{
				var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.User.UserId == userWithRole.User.UserId);
				if (doctor != null && doctor.VerificationStatus != EVerificationStatus.Verified)
				{
					throw new BadRequestException("Your profile is currently under review by our administration team. Password reset is not permitted until your account is approved.");
				}
			}
			else if (roleName == "Admin")
			{
				var adminObj = await _dbContext.Admins.FirstOrDefaultAsync(a => a.User.UserId == userWithRole.User.UserId);
				if (adminObj != null && !adminObj.IsVerified)
				{
					throw new BadRequestException("Your administrative profile is currently pending approval. Password reset is not permitted until your account is approved.");
				}
			}

			return roleName;
		}

		public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
			if (user == null)
			{
				throw new NotFoundException("No account found with this email address.");
			}

			// Generate OTP and send to email
			var otp = new Random().Next(100000, 999999).ToString();
			user.EmailVerificationOtp = otp;
			user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15);
			await _dbContext.SaveChangesAsync();

			try
			{
				var subject = "HealSync Password Reset Code";
				var body = $@"
					<div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #ffffff;'>
						<h2 style='color: #ef4444; text-align: center;'>Password Reset Request</h2>
						<hr style='border: none; border-top: 1px solid #eeeeee;' />
						<p>Hello,</p>
						<p>We received a request to reset your password. Use the following 6-digit OTP to set a new password:</p>
						<div style='text-align: center; margin: 32px 0;'>
							<span style='font-size: 2.2rem; font-weight: bold; letter-spacing: 6px; padding: 12px 24px; background-color: #fef2f2; border-radius: 8px; color: #dc2626; border: 1px solid #fecaca;'>{otp}</span>
						</div>
						<p style='color: #6b7280; font-size: 0.85rem;'>This OTP is valid for 15 minutes. If you did not request this, please ignore this email.</p>
						<hr style='border: none; border-top: 1px solid #eeeeee;' />
						<p style='font-size: 0.8rem; color: #9ca3af; text-align: center;'>HealSync Medical Network App</p>
					</div>";

				await _emailService.SendEmailAsync(user.Email, subject, body);
				Console.WriteLine($"[EMAIL SENDER] Sent password reset OTP {otp} to {user.Email}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[EMAIL ERROR] Failed to send password reset email to {user.Email}. Error: {ex.Message}");
				Console.WriteLine($"[EMAIL FALLBACK] Password Reset OTP {otp} for {user.Email}");
			}
		}

		public async Task ResetPasswordAsync(ResetPasswordDto dto)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
			if (user == null)
			{
				throw new NotFoundException("No account found with this email address.");
			}

			if (user.EmailVerificationOtp != dto.Otp || user.EmailVerificationOtpExpiry < DateTime.UtcNow)
			{
				throw new BadRequestException("Invalid or expired OTP code.");
			}

			user.PasswordHash = HashPassword(dto.NewPassword);
			user.EmailVerificationOtp = null;
			user.EmailVerificationOtpExpiry = null;
			user.IsEmailVerified = true; // Also verify email if not yet verified
			await _dbContext.SaveChangesAsync();
		}

		public async Task InitiatePasswordUpdateAsync(Guid userId, InitiatePasswordUpdateDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException("User not found.");
			}

			if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
			{
				throw new BadRequestException("Current password is incorrect.");
			}

			// Current password is correct. Generate OTP for extra security.
			var otp = new Random().Next(100000, 999999).ToString();
			user.EmailVerificationOtp = otp;
			user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15);
			await _dbContext.SaveChangesAsync();

			try
			{
				var subject = "HealSync Password Update Verification";
				var body = $@"
					<div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 12px; background-color: #ffffff;'>
						<h2 style='color: #f59e0b; text-align: center;'>Password Update Verification</h2>
						<hr style='border: none; border-top: 1px solid #eeeeee;' />
						<p>Hello,</p>
						<p>You are updating your password. Please confirm by entering this 6-digit code:</p>
						<div style='text-align: center; margin: 32px 0;'>
							<span style='font-size: 2.2rem; font-weight: bold; letter-spacing: 6px; padding: 12px 24px; background-color: #fffbeb; border-radius: 8px; color: #d97706; border: 1px solid #fde68a;'>{otp}</span>
						</div>
						<p style='color: #6b7280; font-size: 0.85rem;'>This OTP is valid for 15 minutes. If you did not initiate this, please secure your account immediately.</p>
						<hr style='border: none; border-top: 1px solid #eeeeee;' />
						<p style='font-size: 0.8rem; color: #9ca3af; text-align: center;'>HealSync Medical Network App</p>
					</div>";

				await _emailService.SendEmailAsync(user.Email, subject, body);
				Console.WriteLine($"[EMAIL SENDER] Sent password update OTP {otp} to {user.Email}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[EMAIL ERROR] Failed to send password update email to {user.Email}. Error: {ex.Message}");
				Console.WriteLine($"[EMAIL FALLBACK] Password Update OTP {otp} for {user.Email}");
			}
		}

		public async Task UpdatePasswordAsync(Guid userId, UpdatePasswordDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException("User not found.");
			}

			if (user.EmailVerificationOtp != dto.Otp || user.EmailVerificationOtpExpiry < DateTime.UtcNow)
			{
				throw new BadRequestException("Invalid or expired OTP code.");
			}

			user.PasswordHash = HashPassword(dto.NewPassword);
			user.EmailVerificationOtp = null;
			user.EmailVerificationOtpExpiry = null;
			await _dbContext.SaveChangesAsync();
		}

		#endregion
	}
}
