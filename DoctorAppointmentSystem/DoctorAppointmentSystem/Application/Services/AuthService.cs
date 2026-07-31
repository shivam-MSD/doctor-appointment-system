using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using static System.Net.WebRequestMethods;

namespace DoctorAppointmentSystem.Application.Services
{
	public class PendingRegistrationCacheItem
	{
		public string Email { get; set; }
		public string Otp { get; set; }
		public string Role { get; set; }
		public string RegistrationJson { get; set; } // Serialized RegisterDto or DoctorRegisterDto
	}

	//public class EmailSendEventArgs :EventArgs
	//{
	//	public Guid? UserId {  get; set; }
	//	public string Email { get; set; }
	//	public string FirstName { get; set;  }
	//	public string LastName {  get; set; }
	//	public string Body { get; set; }
	//	public string Subject { get; set; }
	//}

	public class AuthService : IAuthService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IEmailService _emailService;
		private readonly INotificationService _notificationService;
		private readonly IConfiguration _configuration;
		private readonly IDistributedCache _distributedCache;
		private readonly IOtpService _otpService;
		private readonly IPasswordHasher<object> _passwordHasher;
		private readonly IPasswordSecurityService _passwordSecurityService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly Microsoft.Extensions.Logging.ILogger<AuthService> _logger;

		public AuthService(
			ApplicationDbContext dbContext,
			IEmailService emailService,
			INotificationService notificationService,
			IConfiguration configuration,
			IDistributedCache distributedCache,
			IOtpService otpService,
			IPasswordHasher<object> passwordHasher,
			IPasswordSecurityService passwordSecurityService,
			IHttpContextAccessor httpContextAccessor,
			Microsoft.Extensions.Logging.ILogger<AuthService> logger)
		{
			_dbContext = dbContext;
			_emailService = emailService;
			_notificationService = notificationService;
			_configuration = configuration;
			_distributedCache = distributedCache;
			_otpService = otpService;
			_passwordHasher = passwordHasher;
			_passwordSecurityService = passwordSecurityService;
			_httpContextAccessor = httpContextAccessor;
			_logger = logger;
		}

		public event EmailSendEventHandler? EmailSendEvent;

		public void OnEmailSendHandle(object o, EmailSendEventArgs emailSendEvent)
		{
			try
			{
				Hangfire.BackgroundJob.Enqueue<IEmailService>(service =>
					service.SendEmailAsync(emailSendEvent.Email, emailSendEvent.Subject, emailSendEvent.Body)
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[AuthService] Failed to enqueue background email via OnEmailSendHandle for {Email}", emailSendEvent.Email);
			}
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
			var otp = _otpService.GenerateOtp();

			// 4. Save registration payload to Distributed Cache
			var cacheKey = $"pending_reg:{registerDto.Email.ToLower().Trim()}";
			var cacheItem = new PendingRegistrationCacheItem
			{
				Email = registerDto.Email.ToLower().Trim(),
				Otp = _otpService.HashOtp(otp),
				Role = parsedRole.ToString(),
				RegistrationJson = JsonSerializer.Serialize(registerDto)
			};
			
			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
			};
			await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheItem), cacheOptions);

			try
			{
				await _emailService.SendOtpVerificationEmailAsync(registerDto.Email, registerDto.FirstName, registerDto.LastName, otp);
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

			if (!await VerifyPasswordAsync(user, loginDto.Password))
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

			// Extract IP, Device, and Location Details for Security & Audit Logging
			var httpCtx = _httpContextAccessor.HttpContext;
			string ipAddress = SecurityHelper.GetClientIpAddress(httpCtx);
			string deviceInfo = SecurityHelper.GetDeviceAndBrowserInfo(httpCtx);
			string nowUtcStr = DateTime.UtcNow.ToString("f");

			// 1. Structured Logging
			_logger.LogInformation("[AuthService] User {Email} ({RoleName}) logged in successfully from IP: {IpAddress}, Device: {DeviceInfo}", user.Email, roleName, ipAddress, deviceInfo);

			// 2. Enqueue Login Security Email Alert via Hangfire
			try
			{
				string emailHtml = SecurityHelper.BuildLoginSecurityEmailHtml(firstName, roleName, nowUtcStr, ipAddress, deviceInfo);
				Hangfire.BackgroundJob.Enqueue<IEmailService>(service => service.SendEmailAsync(user.Email, "🔐 Security Alert: New Login to your HealSync Account", emailHtml));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[AuthService] Failed to enqueue login security email to Hangfire for {Email}", user.Email);
			}

			// 3. Save Login Event into Role Audit Logs
			try
			{
				if (role?.Role == ERole.Admin || role?.Role == ERole.SuperAdmin)
				{
					_dbContext.AdminAuditLogs.Add(new AdminAuditLog
					{
						LogId = Guid.NewGuid(),
						AdminId = profileId ?? user.UserId,
						Action = "LOGIN",
						ActorUserId = user.UserId,
						ActorName = $"{firstName} {lastName}".Trim(),
						Timestamp = DateTime.UtcNow,
						OldDataJson = "{}",
						NewDataJson = "{}",
						Notes = $"Successful Login | Role: {roleName} | IP: {ipAddress} | Device: {deviceInfo}"
					});
				}
				else if (role?.Role == ERole.Doctor && profileId.HasValue)
				{
					_dbContext.DoctorAuditLogs.Add(new DoctorAuditLog
					{
						LogId = Guid.NewGuid(),
						DoctorId = profileId.Value,
						Action = "LOGIN",
						ActorUserId = user.UserId,
						ActorName = $"{firstName} {lastName}".Trim(),
						Timestamp = DateTime.UtcNow,
						OldDataJson = "{}",
						NewDataJson = "{}",
						Notes = $"Doctor Logged In | IP: {ipAddress} | Device: {deviceInfo}"
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[AuthService] Failed to record login audit log for user {UserId}", user.UserId);
			}

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
			var otp = _otpService.GenerateOtp();

			// 3. Save doctor registration payload to Distributed Cache
			var cacheKey = $"pending_reg:{dto.Email.ToLower().Trim()}";
			var cacheItem = new PendingRegistrationCacheItem
			{
				Email = dto.Email.ToLower().Trim(),
				Otp = _otpService.HashOtp(otp),
				Role = ERole.Doctor.ToString(),
				RegistrationJson = JsonSerializer.Serialize(dto)
			};
			
			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
			};
			await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheItem), cacheOptions);

			try
			{
				await _emailService.SendOtpVerificationEmailAsync(dto.Email, dto.FirstName, dto.LastName, otp);
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
		private async Task<bool> VerifyPasswordAsync(User user, string plainPassword)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(plainPassword)) return false;

			var isCorrect = await _passwordSecurityService.VerifyPasswordAsync(user.UserId, plainPassword, _passwordHasher);
			if (isCorrect) return true;

			// Fallback/Legacy verification for upgraded hashes
			var storedHash = await _passwordSecurityService.GetPasswordAsync(user.UserId);
			if (storedHash != null && VerifyLegacyPassword(plainPassword, storedHash))
			{
				var newHash = _passwordHasher.HashPassword(null, plainPassword);
				await _passwordSecurityService.StorePasswordAsync(user.UserId, newHash);
				return true;
			}

			return false;
		}

		private bool VerifyLegacyPassword(string password, string passwordHash)
		{
			if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
			{
				return false;
			}

			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			var computedHash = Convert.ToBase64String(hashedBytes);
			return string.Equals(computedHash, passwordHash, StringComparison.Ordinal);
		}

		private string HashUserPassword(string password)
		{
			if (string.IsNullOrWhiteSpace(password))
			{
				throw new ArgumentException("Password cannot be null or empty", nameof(password));
			}

			return _passwordHasher.HashPassword(null, password);
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
			var otp = _otpService.GenerateOtp();
			user.EmailVerificationOtp = _otpService.HashOtp(otp);
			user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15);
			user.IsEmailVerified = false;
			await _dbContext.SaveChangesAsync();

			try
			{
				await _emailService.SendOtpVerificationEmailAsync(user.Email, "", "", otp);
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
				if (cacheItem == null || !_otpService.VerifyOtp(dto.Otp, cacheItem.Otp))
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
				string passwordHash = "";

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
						IsActive = true,
						IsEmailVerified = true,
						CreatedDate = DateTime.UtcNow,
						LastLoginDate = DateTime.UtcNow
					};
					_dbContext.Users.Add(user);
					_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

					passwordHash = _passwordHasher.HashPassword(null, regDto.Password);

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
						IsActive = true,
						IsEmailVerified = true,
						CreatedDate = DateTime.UtcNow,
						LastLoginDate = DateTime.UtcNow
					};
					_dbContext.Users.Add(user);
					_dbContext.Entry(user).Property("RoleId").CurrentValue = role.RoleId;

					passwordHash = _passwordHasher.HashPassword(null, tempPassword);

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

					try
					{
						await _emailService.SendDoctorOnboardingReceivedEmailAsync(user.Email, firstName, lastName);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[Email Error]: Failed to send onboarding confirmation email: {ex.Message}");
					}
				}

				await _dbContext.SaveChangesAsync();
				await _passwordSecurityService.StorePasswordAsync(user.UserId, passwordHash);

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

			if (dbUser.EmailVerificationOtpExpiry == null || dbUser.EmailVerificationOtpExpiry < DateTime.UtcNow ||
				!(_otpService.VerifyOtp(dto.Otp, dbUser.EmailVerificationOtp) || string.Equals(dbUser.EmailVerificationOtp, dto.Otp, StringComparison.Ordinal)))
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
						Hangfire.BackgroundJob.Enqueue<IEmailService>(service => service.SendEmailAsync(dbUser.Email, emailSubject, emailBody));
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "[Email Error]: Failed to enqueue onboarding confirmation email for {Email}", dbUser.Email);
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
			var userWithRole = await _dbContext.Users
				.Where(u => u.Email == dto.Email.ToLower().Trim())
				.Select(u => new {
					User = u,
					RoleId = EF.Property<Guid>(u, "RoleId")
				})
				.FirstOrDefaultAsync();

			if (userWithRole == null)
			{
				throw new NotFoundException("No account found with this email address.");
			}

			var role = await _dbContext.Roles.FindAsync(userWithRole.RoleId);
			var roleName = role?.Role.ToString();

			if (roleName == null || !string.Equals(roleName, dto.Role, StringComparison.OrdinalIgnoreCase))
			{
				throw new BadRequestException($"This email address is not registered under the '{dto.Role}' role.");
			}

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

			var user = userWithRole.User;
			// Generate OTP and send to email
			var otp = _otpService.GenerateOtp();
			user.EmailVerificationOtp = _otpService.HashOtp(otp);
			user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15);
			await _dbContext.SaveChangesAsync();

			try
			{
				await _emailService.SendPasswordResetEmailAsync(user.Email, "", "", otp);
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
			var userWithRole = await _dbContext.Users
				.Where(u => u.Email == dto.Email.ToLower().Trim())
				.Select(u => new {
					User = u,
					RoleId = EF.Property<Guid>(u, "RoleId")
				})
				.FirstOrDefaultAsync();

			if (userWithRole == null)
			{
				throw new NotFoundException("No account found with this email address.");
			}

			var role = await _dbContext.Roles.FindAsync(userWithRole.RoleId);
			var roleName = role?.Role.ToString();

			if (roleName == null || !string.Equals(roleName, dto.Role, StringComparison.OrdinalIgnoreCase))
			{
				throw new BadRequestException($"This email address is not registered under the '{dto.Role}' role.");
			}

			var user = userWithRole.User;
			if (user.EmailVerificationOtpExpiry == null || user.EmailVerificationOtpExpiry < DateTime.UtcNow ||
				!(_otpService.VerifyOtp(dto.Otp, user.EmailVerificationOtp) || string.Equals(user.EmailVerificationOtp, dto.Otp, StringComparison.Ordinal)))
			{
				throw new BadRequestException("Invalid or expired OTP code.");
			}

			var newHash = _passwordHasher.HashPassword(null, dto.NewPassword);
			await _passwordSecurityService.StorePasswordAsync(user.UserId, newHash);
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

			if (!await VerifyPasswordAsync(user, dto.CurrentPassword))
			{
				throw new BadRequestException("Current password is incorrect.");
			}

			// Current password is correct. Generate OTP for extra security.
			var otp = _otpService.GenerateOtp();
			user.EmailVerificationOtp = _otpService.HashOtp(otp);
			user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(15);
			await _dbContext.SaveChangesAsync();

			try
			{
				await _emailService.SendOtpVerificationEmailAsync(user.Email, "", "", otp);
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

			if (user.EmailVerificationOtpExpiry == null || user.EmailVerificationOtpExpiry < DateTime.UtcNow ||
				!(_otpService.VerifyOtp(dto.Otp, user.EmailVerificationOtp) || string.Equals(user.EmailVerificationOtp, dto.Otp, StringComparison.Ordinal)))
			{
				throw new BadRequestException("Invalid or expired OTP code.");
			}

			var newHash = _passwordHasher.HashPassword(null, dto.NewPassword);
			await _passwordSecurityService.StorePasswordAsync(user.UserId, newHash);
			user.EmailVerificationOtp = null;
			user.EmailVerificationOtpExpiry = null;
			await _dbContext.SaveChangesAsync();
		}

		#endregion
	}
}
