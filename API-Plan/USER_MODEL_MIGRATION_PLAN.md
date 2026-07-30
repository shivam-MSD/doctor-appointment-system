# User Model Migration Plan

## Current State
```csharp
public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }  // ❌ ISSUE: Stored in DB as plain/hashed
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationOtp { get; set; }  // ❌ ISSUE: Plain text OTP!
    public DateTime? EmailVerificationOtpExpiry { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginDate { get; set; }
}
```

## Target State (Security Enhanced)
```csharp
public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    
    // ❌ REMOVED: Password now stored in Redis only
    // public string PasswordHash { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;
    
    // ✅ CHANGED: Plain text OTP → Hashed OTP
    public string? EmailVerificationOtpHash { get; set; }  // Now hashed with BCrypt
    public DateTime? EmailVerificationOtpExpiry { get; set; }
    
    // ✅ NEW: Track failed OTP attempts (rate limiting)
    public int OtpAttempts { get; set; } = 0;
    
    // ✅ NEW: Track password change history
    public DateTime? LastPasswordChangedDate { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginDate { get; set; }
}
```

---

## Step-by-Step Migration

### Phase 1: Add New Fields (Non-Breaking)
Create a migration that adds new fields WITHOUT removing old ones.

```bash
dotnet ef migrations add AddSecurityFields --project DoctorAppointmentSystem
```

**Migration Code:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add new columns
    migrationBuilder.AddColumn<string>(
        name: "EmailVerificationOtpHash",
        table: "Users",
        type: "nvarchar(500)",
        nullable: true);

    migrationBuilder.AddColumn<int>(
        name: "OtpAttempts",
        table: "Users",
        type: "int",
        nullable: false,
        defaultValue: 0);

    migrationBuilder.AddColumn<DateTime>(
        name: "LastPasswordChangedDate",
        table: "Users",
        type: "datetime2",
        nullable: true);

    // Drop old column (if PasswordHash should be removed immediately)
    // migrationBuilder.DropColumn(
    //     name: "PasswordHash",
    //     table: "Users");
    
    // Rename old column (safer alternative)
    // migrationBuilder.RenameColumn(
    //     name: "EmailVerificationOtp",
    //     table: "Users",
    //     newName: "EmailVerificationOtpHash");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "EmailVerificationOtpHash",
        table: "Users");

    migrationBuilder.DropColumn(
        name: "OtpAttempts",
        table: "Users");

    migrationBuilder.DropColumn(
        name: "LastPasswordChangedDate",
        table: "Users");
}
```

### Phase 2: Update Applicationchanges (Code Changes)

**Update User.cs:**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }

        // NOTE: PasswordHash is now stored in Redis, not in database
        // This field can be removed after migration to Redis is complete
        // For now, we keep it for backward compatibility during transition
        [Obsolete("Use IPasswordSecurityService for password verification")]
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public bool RequiresPasswordChange { get; set; } = false;

        [Required]
        public bool IsEmailVerified { get; set; } = false;

        // ✅ NEW: Hashed OTP (using BCrypt via OtpService)
        [MaxLength(500)]
        public string? EmailVerificationOtpHash { get; set; }

        public DateTime? EmailVerificationOtpExpiry { get; set; }

        // ✅ NEW: Track failed OTP attempts for rate limiting
        public int OtpAttempts { get; set; } = 0;

        // ✅ NEW: Track when password was last changed
        public DateTime? LastPasswordChangedDate { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginDate { get; set; }
    }
}
```

### Phase 3: Update ApplicationDbContext (If Needed)
```csharp
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    // ... existing code ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.IsEmailVerified)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValue(DateTime.UtcNow);

            entity.Property(e => e.OtpAttempts)
                .HasDefaultValue(0);

            // Create index for email lookups
            entity.HasIndex(e => e.Email).IsUnique();

            // Create index for verification
            entity.HasIndex(e => e.IsEmailVerified);
        });

        // ... rest of configuration ...
    }
}
```

### Phase 4: Implement Data Migration Script (Optional)
If you need to migrate existing passwords to Redis:

```csharp
// RunAfterMigration.cs
public async Task MigrateExistingPasswordsToRedis()
{
    var users = await _dbContext.Users
        .Where(u => !string.IsNullOrEmpty(u.PasswordHash))
        .ToListAsync();

    foreach (var user in users)
    {
        // Store existing password hash in Redis for 90 days
        await _passwordSecurityService.StorePasswordAsync(
            user.UserId,
            user.PasswordHash,
            TimeSpan.FromDays(90));
    }

    // After all migrated, set PasswordHash to null
    foreach (var user in users)
    {
        user.PasswordHash = null;
    }

    await _dbContext.SaveChangesAsync();
}
```

---

## ⏱️ Timeline

| Phase | Task | Duration | Breaking |
|-------|------|----------|----------|
| Week 1 | Add OtpService, PasswordSecurityService | 1-2 days | No |
| Week 1 | Deploy new EmailService with templates | 1-2 days | No |
| Week 1 | Update AuthService to use new services | 2-3 days | No |
| Week 2 | Create migration (add new fields) | 1 day | No |
| Week 2 | Deploy migration to staging | 1 day | No |
| Week 2 | Test OTP hashing flow end-to-end | 2-3 days | No |
| Week 3 | Migrate passwords to Redis (scheduled task) | 1 day | Requires downtime |
| Week 3 | Remove PasswordHash from User table | 1 day | Breaking |
| Week 3 | Deploy to production | 1 day | Yes |

---

## 🔄 Rollback Plan

If something goes wrong:

**Easy Rollback (Weeks 1-2):**
```bash
# Simply don't use new services, keep using old flow
dotnet ef migrations remove  # Undo last migration
```

**Hard Rollback (After Phase 3):**
```bash
# Restore from backup
# Re-run old queries to populate PasswordHash from Redis
# Redeploy previous version
```

---

## ✅ Validation Checklist

After each phase, verify:

- [ ] New OTP is generated as random digits
- [ ] OTP is hashed before database storage
- [ ] Existing OTP hash verification works correctly
- [ ] Password can be stored in Redis
- [ ] Password can be verified from Redis
- [ ] Email events fire without blocking
- [ ] Migration runs without errors
- [ ] No user data is lost
- [ ] Old API endpoints still work
- [ ] New API endpoints return proper errors
- [ ] OTP expiry is enforced
- [ ] Rate limiting on OTP attempts works

---

## 🚀 Deployment Commands

```bash
# 1. Build and test locally
dotnet build
dotnet test

# 2. Create migration
dotnet ef migrations add AddSecurityFields --project DoctorAppointmentSystem

# 3. Deploy to staging
dotnet ef database update --environment Staging

# 4. Run integration tests
dotnet test --filter "Category=Security"

# 5. Deploy to production
dotnet ef database update --environment Production

# 6. Monitor logs
tail -f /var/log/doctorappointmentsystem/error.log
```

---

## 📚 Related Documentation

- [SECURITY_IMPLEMENTATION_GUIDE.md](./SECURITY_IMPLEMENTATION_GUIDE.md) - Complete implementation guide
- [DATABASE_ANALYSIS_AND_RECOMMENDATIONS.md](./DATABASE_ANALYSIS_AND_RECOMMENDATIONS.md) - Full database analysis
- [OtpService.cs](./DoctorAppointmentSystem/Application/Services/OtpService.cs) - OTP service implementation
- [PasswordSecurityService.cs](./DoctorAppointmentSystem/Application/Services/PasswordSecurityService.cs) - Password service implementation
- [EmailService.cs](./DoctorAppointmentSystem/Application/Services/EmailService.cs) - Email service with templates

