# Quick Reference & Cheat Sheet

## 🚀 For Developers: Getting Started

### I Need To...

#### Generate & Verify OTPs
```csharp
// Inject
private readonly IOtpService _otpService;

// Generate a 6-digit random OTP
var otp = _otpService.GenerateOtp(6);  // Returns "123456"

// Hash it before storage
var hashedOtp = _otpService.HashOtp(otp);  // Returns BCrypt hash
user.EmailVerificationOtpHash = hashedOtp;

// Verify user's submitted OTP
if (!_otpService.VerifyOtp(userSubmittedOtp, user.EmailVerificationOtpHash))
    throw new BadRequestException("Invalid OTP");
```

#### Store & Verify Passwords
```csharp
// Inject
private readonly IPasswordSecurityService _passwordSecurityService;

// Hash password
var hasher = new PasswordHasher<object>();
var hashedPassword = hasher.HashPassword(null, plainPassword);

// Store in Redis (not database)
await _passwordSecurityService.StorePasswordAsync(userId, hashedPassword);

// Verify during login
bool isValid = await _passwordSecurityService.VerifyPasswordAsync(
    userId, submittedPassword, hasher);

// Clear on logout
await _passwordSecurityService.InvalidatePasswordAsync(userId);
```

#### Send Emails with Templates
```csharp
// Inject
private readonly IEmailService _emailService;

// OTP email
await _emailService.SendOtpVerificationEmailAsync(email, "123456", 10);

// Password reset
await _emailService.SendPasswordResetEmailAsync(email, resetToken, "John");

// Appointment confirmation
var details = "Dr. Smith, Monday 3:00 PM, Clinic Name";
await _emailService.SendAppointmentConfirmationAsync(email, details);

// Doctor verification
await _emailService.SendDoctorVerificationEmailAsync(email, "Dr. Smith", true);

// Appointment cancellation
await _emailService.SendAppointmentCancellationAsync(email, "Appointment cancelled");
```

#### Subscribe to Email Events (Only in AuthService)
```csharp
public class AuthService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IEmailService emailService, ILogger<AuthService> logger)
    {
        _emailService = emailService;
        _logger = logger;
        
        // Subscribe to events
        _emailService.EmailSendEvent += OnEmailSendHandle;
    }

    public void OnEmailSendHandle(object sender, EmailSendEventArgs args)
    {
        Task.Run(async () =>
        {
            try
            {
                await _emailService.SendEmailAsync(
                    args.Email, args.Subject, args.Body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email send failed");
            }
        });
    }
}
```

---

## 📚 File Locations

### New Service Files
```
DoctorAppointmentSystem/
├── Application/
│   └── Services/
│       ├── OtpService.cs                    ← Cryptographic OTP
│       ├── PasswordSecurityService.cs       ← Redis password storage
│       └── EmailService.cs                  ← (Updated) Event-driven emails
```

### Configuration
```
appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "Redis": "localhost:6379"
  },
  "MailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Mail": "your-email@example.com",
    "Password": "app-password"
  }
}
```

---

## 🔧 Setup Checklist

**Before you can use the services:**

- [ ] `appsettings.json` has Redis connection string
- [ ] `appsettings.json` has MailSettings configured
- [ ] Redis is running (check: `redis-cli`)
- [ ] Services registered in `Program.cs` or `Startup.cs`
- [ ] `using` statements imported (see below)

**Using statements needed:**
```csharp
using DoctorAppointmentSystem.Application.Services;
using Microsoft.AspNetCore.Identity;
```

---

## 🐛 Common Errors & Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| `IOtpService not found` | Service not registered | Add to DI: `services.AddScoped<IOtpService, OtpService>()` |
| `Redis connection refused` | Redis not running | Start Redis: `redis-server` |
| `IEmailService circular dependency` | Event subscriber issue | Only subscribe in AuthService |
| `NullReferenceException` on `VerifyOtp` | Hash is null | Check if `EmailVerificationOtpHash` was set |
| `The specified string index length is invalid` | Password hash format wrong | Ensure using BCrypt hash, not plain text |
| `SMTP authentication failed` | Wrong email credentials | Check MailSettings in appsettings.json |

---

## 🎯 Before/After Patterns

### OTP Pattern

**❌ Before:**
```csharp
var otp = Random.Shared.Next(100000, 999999).ToString();
user.EmailVerificationOtp = otp;  // Plain text!
if (user.EmailVerificationOtp == submittedOtp) { }  // Direct comparison
```

**✅ After:**
```csharp
var otp = _otpService.GenerateOtp(6);  // Crypto-secure
var hashedOtp = _otpService.HashOtp(otp);
user.EmailVerificationOtpHash = hashedOtp;
if (_otpService.VerifyOtp(submittedOtp, user.EmailVerificationOtpHash)) { }
```

### Password Pattern

**❌ Before:**
```csharp
var hasher = new PasswordHasher<object>();
user.PasswordHash = hasher.HashPassword(null, password);  // In database!
```

**✅ After:**
```csharp
var hasher = new PasswordHasher<object>();
var hashedPassword = hasher.HashPassword(null, password);
await _passwordSecurityService.StorePasswordAsync(userId, hashedPassword);  // In Redis!
```

### Email Pattern

**❌ Before:**
```csharp
Task.Run(async () =>
{
    await _emailService.SendEmailAsync(
        email, "OTP", $"Your OTP is: {otp}");
});
```

**✅ After:**
```csharp
await _emailService.SendOtpVerificationEmailAsync(email, otp, 10);
```

---

## 📱 Integration Examples

### User Registration
```csharp
public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request)
{
    // Hash password (new way)
    var hasher = new PasswordHasher<object>();
    var hashedPassword = hasher.HashPassword(null, request.Password);
    
    // Create user
    var user = new User
    {
        Email = request.Email,
        FirstName = request.FirstName,
        // PasswordHash = hashedPassword;  // DON'T store in DB
    };
    
    _dbContext.Users.Add(user);
    await _dbContext.SaveChangesAsync();
    
    // Store password in Redis
    await _passwordSecurityService.StorePasswordAsync(user.UserId, hashedPassword);
    
    // Request email verification
    var otp = _otpService.GenerateOtp(6);
    user.EmailVerificationOtpHash = _otpService.HashOtp(otp);
    user.EmailVerificationOtpExpiry = DateTime.UtcNow.AddMinutes(10);
    await _dbContext.SaveChangesAsync();
    
    // Send OTP email (triggers EmailSendEvent)
    await _emailService.SendOtpVerificationEmailAsync(request.Email, otp, 10);
    
    return new RegistrationResponse { Message = "Check your email for OTP" };
}
```

### Email Verification
```csharp
public async Task<VerifyEmailResponse> VerifyEmailAsync(string email, string otp)
{
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
        throw new NotFoundException("User not found");
    
    // Check expiry
    if (user.EmailVerificationOtpExpiry < DateTime.UtcNow)
        throw new BadRequestException("OTP expired");
    
    // Check attempts
    if (user.OtpAttempts >= 5)
        throw new BadRequestException("Too many attempts");
    
    // Verify OTP
    if (!_otpService.VerifyOtp(otp, user.EmailVerificationOtpHash))
    {
        user.OtpAttempts++;
        await _dbContext.SaveChangesAsync();
        throw new BadRequestException("Invalid OTP");
    }
    
    // Mark verified
    user.IsEmailVerified = true;
    user.EmailVerificationOtpHash = null;
    user.OtpAttempts = 0;
    await _dbContext.SaveChangesAsync();
    
    return new VerifyEmailResponse { Message = "Email verified successfully" };
}
```

### Login
```csharp
public async Task<LoginResponse> LoginAsync(string email, string password)
{
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null)
        throw new UnauthorizedAccessException("Invalid credentials");
    
    if (!user.IsActive)
        throw new BadRequestException("Account inactive");
    
    var hasher = new PasswordHasher<object>();
    
    // Verify from Redis (not database)
    bool isValid = await _passwordSecurityService.VerifyPasswordAsync(
        user.UserId, password, hasher);
    
    if (!isValid)
        throw new UnauthorizedAccessException("Invalid credentials");
    
    user.LastLoginDate = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();
    
    var token = GenerateJwtToken(user);
    
    return new LoginResponse
    {
        Token = token,
        Email = user.Email,
        IsEmailVerified = user.IsEmailVerified
    };
}
```

---

## 🔐 Security Checklist

- [x] OTPs are cryptographically random (not `Random.Shared`)
- [x] OTPs are hashed before storage (BCrypt, not plain text)
- [x] OTPs have expiration times (10 minutes default)
- [x] OTPs have attempt limiting (5 attempts max)
- [x] Passwords stored in Redis (not database)
- [x] Passwords hashed with PasswordHasher (PBKDF2)
- [x] Email events are async (fire-and-forget)
- [x] No passwords printed in logs
- [x] Redis connection encrypted (for production)
- [x] Email templates HTML formatted

---

## 🧪 Quick Test Commands

```bash
# Test 1: Services compile
dotnet build

# Test 2: Migrations work
dotnet ef migrations add TestMigration --project DoctorAppointmentSystem
dotnet ef database update --project DoctorAppointmentSystem

# Test 3: Redis connection
redis-cli ping  # Should return "PONG"

# Test 4: Run unit tests
dotnet test

# Test 5: Start application
dotnet run --project DoctorAppointmentSystem
```

---

## 📞 Support

**For implementation help:**
1. Check [INTEGRATION_CHECKLIST.md](./INTEGRATION_CHECKLIST.md) for step-by-step guidance
2. Read [SECURITY_IMPLEMENTATION_GUIDE.md](./SECURITY_IMPLEMENTATION_GUIDE.md) for detailed examples
3. Review created service files for actual implementation
4. Check [USER_MODEL_MIGRATION_PLAN.md](./USER_MODEL_MIGRATION_PLAN.md) for database changes

**For debugging:**
1. Enable logging: Add `services.AddLogging()`
2. Check Redis: `redis-cli --scan` (should show keys like "password:*")
3. Check SMTP: Test with `telnet smtp.gmail.com 587`
4. Check OTP: Verify BCrypt hash at `https://bcrypt.online`

