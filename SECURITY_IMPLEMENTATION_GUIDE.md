# Security Implementation Guide

## 🔒 Summary of Security Enhancements

This document provides complete implementation guidance for integrating three critical security services:

1. **OTP Service** - Cryptographically secure OTP generation with hashing
2. **Password Security Service** - Redis-based password storage 
3. **Email Service** - Event-driven email sending with templates

---

## 1️⃣ OTP SERVICE (IOtpService)

### Usage in Dependencies

```csharp
// In Program.cs or Startup.cs:
services.AddScoped<IOtpService, OtpService>();
```

### Usage in AuthService

**Before (Plain Text OTP):**
```csharp
public async Task RequestEmailVerificationAsync(string email)
{
    var otp = Random.Shared.Next(100000, 999999).ToString();  // ❌ Weak
    user.EmailVerificationOtp = otp;  // ❌ Plain text storage
    await _dbContext.SaveChangesAsync();
}
```

**After (Hashed OTP):**
```csharp
private readonly IOtpService _otpService;
private readonly IOtpAttemptService _otpAttemptService;  // For rate limiting

public async Task RequestEmailVerificationAsync(string email)
{
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
        throw new NotFoundException("User not found");

    // ✅ Generate cryptographically secure OTP
    var otp = _otpService.GenerateOtp(6);  // "123456"
    
    // ✅ Hash the OTP before storing
    var hashedOtp = _otpService.HashOtp(otp);
    
    user.EmailVerificationOtpHash = hashedOtp;  // Update User model
    user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(10);
    user.OtpAttempts = 0;  // Reset attempts
    
    await _dbContext.SaveChangesAsync();
    
    // ✅ Send plain OTP to user email (not the hash!)
    await _emailService.SendOtpVerificationEmailAsync(email, otp, 10);
}
```

### Usage in Verification

```csharp
public async Task VerifyEmailOtpAsync(string email, string submittedOtp)
{
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
        throw new NotFoundException("User not found");

    // ✅ Check expiry
    if (user.EmailVerificationOtpExpiry < DateTime.UtcNow)
        throw new BadRequestException("OTP has expired. Request a new one.");

    // ✅ Check attempts (rate limiting)
    if (user.OtpAttempts >= 5)
        throw new BadRequestException("Too many failed attempts. Request a new OTP.");

    // ✅ Verify against hash (not plain text comparison)
    if (!_otpService.VerifyOtp(submittedOtp, user.EmailVerificationOtpHash))
    {
        user.OtpAttempts++;
        await _dbContext.SaveChangesAsync();
        throw new BadRequestException("Invalid OTP");
    }

    // ✅ Mark as verified
    user.IsEmailVerified = true;
    user.EmailVerificationOtpHash = null;  // Clear hash
    user.EmailVerificationOtpExpiry = null;
    user.OtpAttempts = 0;
    
    await _dbContext.SaveChangesAsync();
}
```

---

## 2️⃣ PASSWORD SECURITY SERVICE (IPasswordSecurityService)

### Why Redis?

✅ **Advantages:**
- Passwords NOT in database (major security win)
- Fast retrieval (in-memory cache)
- Automatic expiration (no manual cleanup)
- Can be secured separately from database

### Setup in Program.cs

```csharp
// Configure Redis distributed cache
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis") 
        ?? "localhost:6379";
});

// Register password security service
services.AddScoped<IPasswordSecurityService, PasswordSecurityService>();
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...",
    "Redis": "localhost:6379"
  }
}
```

### Usage in AuthService - Change Password Event

```csharp
public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
{
    var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);
    if (user == null)
        throw new NotFoundException("User not found");

    var passwordHasher = new PasswordHasher<object>();
    
    // ✅ Step 1: Verify current password against Redis cache
    bool isValid = await _passwordSecurityService.VerifyPasswordAsync(
        userId, currentPassword, passwordHasher);
    
    if (!isValid)
        throw new BadRequestException("Current password is incorrect");

    // ✅ Step 2: Hash new password
    string newPasswordHash = passwordHasher.HashPassword(null, newPassword);

    // ✅ Step 3: Store hashed password in Redis (not database)
    await _passwordSecurityService.StorePasswordAsync(
        userId, newPasswordHash, TimeSpan.FromDays(365));

    // ✅ Step 4: Update User table (no PasswordHash stored anymore)
    user.RequiresPasswordChange = false;
    user.LastPasswordChangedDate = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();

    // ✅ Step 5: Clear old password cache on other sessions
    // (Optional: use distributed cache to track user sessions)
}
```

### Usage in Login

```csharp
public async Task<LoginResponse> LoginAsync(string email, string password)
{
    var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);
    if (user == null)
        throw new UnauthorizedAccessException("Invalid credentials");

    if (!user.IsActive)
        throw new BadRequestException("Account is inactive");

    var passwordHasher = new PasswordHasher<object>();

    // ✅ Verify password against Redis cache (not database)
    bool isValid = await _passwordSecurityService.VerifyPasswordAsync(
        user.UserId, password, passwordHasher);

    if (!isValid)
        throw new UnauthorizedAccessException("Invalid credentials");

    // ✅ Password verified — proceed with login
    user.LastLoginDate = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();

    // Generate JWT token
    var token = GenerateJwtToken(user);
    
    return new LoginResponse 
    { 
        Token = token, 
        Email = user.Email,
        IsEmailVerified = user.IsEmailVerified
    };
}
```

### Usage on Logout

```csharp
public async Task LogoutAsync(Guid userId)
{
    // ✅ Clear password from Redis to require re-authentication
    await _passwordSecurityService.InvalidatePasswordAsync(userId);
}
```

---

## 3️⃣ EMAIL SERVICE (IEmailService) - Event-Driven

### Setup in Program.cs

```csharp
services.AddScoped<IEmailService, EmailService>();
```

### Subscribe to Email Events in AuthService

The **AuthService should subscribe to EmailSendEvent** and handle actual sending:

```csharp
public class AuthService
{
    private readonly IEmailService _emailService;

    public AuthService(IEmailService emailService)
    {
        _emailService = emailService;
        
        // ✅ Subscribe to email events
        _emailService.EmailSendEvent += OnEmailSendHandle;
    }

    /// <summary>
    /// Handle email sending asynchronously.
    /// This runs in the background without blocking the main operation.
    /// </summary>
    public void OnEmailSendHandle(object o, EmailSendEventArgs emailSendEvent)
    {
        try
        {
            // Fire-and-forget: send email without blocking
            Task.Run(async () =>
            {
                try
                {
                    // Send email via SMTP (configured in appsettings.json)
                    await _emailService.SendEmailAsync(
                        emailSendEvent.Email,
                        emailSendEvent.Subject,
                        emailSendEvent.Body);

                    // Log successful send (optional)
                    _logger.LogInformation($"Email sent to {emailSendEvent.Email}");
                }
                catch (Exception ex)
                {
                    // Log failure but don't throw
                    _logger.LogError(ex, $"Failed to send email to {emailSendEvent.Email}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in email handler");
            throw;
        }
    }
}
```

### Usage: Sending OTP Email

```csharp
public async Task RequestEmailVerificationAsync(string email)
{
    // ... create OTP ...
    var otp = _otpService.GenerateOtp(6);
    
    // ✅ Use template method (raises event internally)
    await _emailService.SendOtpVerificationEmailAsync(email, otp, 10);
    
    // The event is raised automatically
    // Subscribers handle actual SMTP sending asynchronously
}
```

### Usage: Sending Password Reset Email

```csharp
public async Task ForgotPasswordAsync(string email)
{
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
        throw new NotFoundException("User not found");

    var resetToken = GeneratePasswordResetToken(user);

    // ✅ Send via template
    await _emailService.SendPasswordResetEmailAsync(email, resetToken, user.FirstName);
}
```

### Usage: Appointment Confirmation

```csharp
public async Task ConfirmAppointmentAsync(Guid appointmentId)
{
    var appointment = await _dbContext.Appointments
        .Include(a => a.Patient)
        .Include(a => a.Doctor)
        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

    if (appointment == null)
        throw new NotFoundException("Appointment not found");

    var patient = appointment.Patient;
    var patientUser = await GetPatientUserAsync(patient.PatientId);

    // Build appointment details HTML
    var appointmentDetails = $@"
Doctor: Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}
Date: {appointment.AppointmentDate:dddd, MMMM d, yyyy}
Time: {appointment.DoctorAssignedTime:h:mm tt}
Location: {appointment.Clinic?.ClinicName}
";

    // ✅ Send via template
    await _emailService.SendAppointmentConfirmationAsync(
        patientUser.Email, appointmentDetails);
}
```

### Usage: Doctor Verification Status

```csharp
public async Task ApproveDoctor(Guid doctorId)
{
    var doctor = await _dbContext.Doctors
        .Include(d => d.User)
        .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

    if (doctor == null)
        throw new NotFoundException("Doctor not found");

    doctor.VerificationStatus = EVerificationStatus.Verified;
    await _dbContext.SaveChangesAsync();

    // ✅ Send approval email via template
    await _emailService.SendDoctorVerificationEmailAsync(
        doctor.User.Email, doctor.FirstName, true);
}

public async Task RejectDoctor(Guid doctorId, string rejectionReason)
{
    var doctor = await _dbContext.Doctors
        .Include(d => d.User)
        .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

    if (doctor == null)
        throw new NotFoundException("Doctor not found");

    doctor.VerificationStatus = EVerificationStatus.Rejected;
    await _dbContext.SaveChangesAsync();

    // ✅ Send rejection email with reason
    await _emailService.SendDoctorVerificationEmailAsync(
        doctor.User.Email, doctor.FirstName, false, rejectionReason);
}
```

---

## 📝 User Model Updates Required

```csharp
public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    
    // ❌ REMOVE: public string PasswordHash { get; set; }
    // ✅ ADD: Password is now stored in Redis only
    
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; } = false;
    
    public bool IsEmailVerified { get; set; } = false;
    
    // ✅ NEW: Hashed OTP (not plain text)
    public string? EmailVerificationOtpHash { get; set; }
    public DateTime? EmailVerificationOtpExpiry { get; set; }
    
    // ✅ NEW: Track OTP attempts (rate limiting)
    public int OtpAttempts { get; set; } = 0;
    
    // ✅ NEW: Track password changes
    public DateTime? LastPasswordChangedDate { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginDate { get; set; }
}
```

---

## 📊 Implementation Checklist

- [ ] Create OtpService class
- [ ] Create PasswordSecurityService class
- [ ] Update EmailService with event handling and templates
- [ ] Register services in Program.cs/Startup.cs
- [ ] Configure Redis connection string
- [ ] Update User entity (remove PasswordHash, add new fields)
- [ ] Update AuthService to subscribe to EmailSendEvent
- [ ] Update AuthService to use OtpService
- [ ] Update AuthService/UserService to use PasswordSecurityService
- [ ] Update all email sending calls to use new template methods
- [ ] Create database migration for User model changes
- [ ] Test OTP generation, hashing, and verification
- [ ] Test password storage in Redis
- [ ] Test email event firing
- [ ] Write unit tests for each service
- [ ] Update documentation

---

## ⚠️ Migration Path (From Old to New)

### Step 1: Deploy Services (No Breaking Changes)
```bash
# Deploy OTP and Email services first
# Existing code continues to work
git commit -m "feat: add OTP hashing and email event services"
```

### Step 2: Update AuthService to Use New Services
```bash
# Gradually update methods to use new services
# Keep old methods working in parallel
git commit -m "refactor: use OtpService and new EmailService"
```

### Step 3: Migrate User Table
```bash
# Create migration
dotnet ef migrations add UpdateUserPasswordFields

# Migration should:
# - Remove PasswordHash column
# - Add EmailVerificationOtpHash
# - Add OtpAttempts
# - Add LastPasswordChangedDate

git commit -m "chore: migrate user table for Redis password storage"
```

### Step 4: Deprecate Old Password Queries
```bash
# Remove direct user.PasswordHash usage
# Update all login/verification logic
git commit -m "refactor: remove database password hash queries"
```

---

## 🔐 Security Best Practices Implemented

✅ **OTP Security:**
- Cryptographically secure random generation
- PBKDF2 hashing using Identity framework
- Expiration time enforcement
- Attempt limiting (rate limiting)

✅ **Password Security:**
- Passwords stored in Redis (not database)
- Automatic expiration
- Session invalidation on logout
- No plain text storage

✅ **Email Security:**
- Event-driven (async, fire-and-forget)
- Separation of concerns
- No blocking operations
- HTML email templates
- Easy to add logging/auditing

---

*This guide provides complete implementation details for security enhancement as requested.*
