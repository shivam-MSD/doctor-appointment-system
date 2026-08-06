using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Hangfire;
using Hangfire.PostgreSql;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);
// Explicitly load base and environment‑specific configuration files
var reloadOnChange = builder.Environment.IsDevelopment();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: reloadOnChange);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: reloadOnChange);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Configure CORS to allow direct frontend connections from any source (with SignalR WebSocket support)
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.SetIsOriginAllowed(_ => true)
			  .AllowAnyHeader()
			  .AllowAnyMethod()
			  .AllowCredentials();
	});
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt Key not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwtIssuer,
		ValidAudience = jwtAudience,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
		ClockSkew = TimeSpan.Zero
	};

	options.Events = new JwtBearerEvents
	{
		OnMessageReceived = context =>
		{
			var accessToken = context.Request.Query["access_token"];
			var path = context.HttpContext.Request.Path;
			if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
			{
				context.Token = accessToken;
			}
			return Task.CompletedTask;
		}
	};
});

builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
{
	var uri = connectionString.Replace("postgresql://", "").Replace("postgres://", "");
	var atIndex = uri.IndexOf('@');
	if (atIndex != -1)
	{
		var credentials = uri.Substring(0, atIndex);
		var hostDbAndOptions = uri.Substring(atIndex + 1);

		var colonIndex = credentials.IndexOf(':');
		var username = colonIndex != -1 ? credentials.Substring(0, colonIndex) : credentials;
		var password = colonIndex != -1 ? credentials.Substring(colonIndex + 1) : "";

		var questionIndex = hostDbAndOptions.IndexOf('?');
		var hostAndDb = questionIndex != -1 ? hostDbAndOptions.Substring(0, questionIndex) : hostDbAndOptions;
		var options = questionIndex != -1 ? hostDbAndOptions.Substring(questionIndex + 1) : "";

		var slashIndex = hostAndDb.IndexOf('/');
		var hostAndPort = slashIndex != -1 ? hostAndDb.Substring(0, slashIndex) : hostAndDb;
		var database = slashIndex != -1 ? hostAndDb.Substring(slashIndex + 1) : "";

		var hostColonIndex = hostAndPort.IndexOf(':');
		var host = hostColonIndex != -1 ? hostAndPort.Substring(0, hostColonIndex) : hostAndPort;
		var port = hostColonIndex != -1 ? hostAndPort.Substring(hostColonIndex + 1) : "5432";

		connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
		if (options.Contains("sslmode=require") || uri.Contains("sslmode=require"))
		{
			connectionString += "SSL Mode=Require;Trust Server Certificate=true;";
		}
	}
}

builder.Services.AddDbContext<DoctorAppointmentSystem.Persistent.Context.ApplicationDbContext>(options =>
{
	if (connectionString.Contains("Host="))
	{
		options.UseNpgsql(connectionString);
	}
	else
	{
		options.UseSqlServer(connectionString);
	}
});

builder.Services.AddExceptionHandler<DoctorAppointmentSystem.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAuthService, DoctorAppointmentSystem.Application.Services.AuthService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IFamilyService, DoctorAppointmentSystem.Application.Services.FamilyService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IUserService, DoctorAppointmentSystem.Application.Services.UserService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IPatientService, DoctorAppointmentSystem.Application.Services.PatientService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAdminService, DoctorAppointmentSystem.Application.Services.AdminService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAppointmentService, DoctorAppointmentSystem.Application.Services.AppointmentService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IClinicService, DoctorAppointmentSystem.Application.Services.ClinicService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IWhatsAppService, DoctorAppointmentSystem.Application.Services.WhatsAppService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IEmailService, DoctorAppointmentSystem.Application.Services.EmailService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IOtpService, DoctorAppointmentSystem.Application.Services.OtpService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<object>, Microsoft.AspNetCore.Identity.PasswordHasher<object>>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IPasswordSecurityService, DoctorAppointmentSystem.Application.Services.PasswordSecurityService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.INotificationService, DoctorAppointmentSystem.Application.Services.NotificationService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IWebPushService, DoctorAppointmentSystem.Application.Services.WebPushService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IHangfireJobService, DoctorAppointmentSystem.Application.Services.HangfireJobService>();

builder.Services.AddHangfire(config => config
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<DoctorAppointmentSystem.Application.Services.IBackgroundQueueService, DoctorAppointmentSystem.Application.Services.BackgroundQueueService>();
builder.Services.AddHostedService<DoctorAppointmentSystem.Application.Services.BackgroundQueueHostedService>();

// Register background services
builder.Services.AddHostedService<DoctorAppointmentSystem.Application.BackgroundServices.AppointmentCleanupService>();
builder.Services.AddHostedService<DoctorAppointmentSystem.Application.BackgroundServices.NotificationCleanupService>();

builder.Services.AddSignalR();
builder.Services.AddStackExchangeRedisCache(options =>
{
	var connStr = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
	var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(connStr);
	configOptions.ConnectTimeout = 250; // Fail fast if Redis is down
	configOptions.SyncTimeout = 250;
	configOptions.AbortOnConnectFail = false;
	options.ConfigurationOptions = configOptions;
	options.InstanceName = "HealSync_";
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
	app.MapOpenApi();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/openapi/v1.json", "Doctor Appointment System API v1");
		options.RoutePrefix = "swagger";
	});
}

app.UseHttpsRedirection();

app.UseCors();

app.UseHangfireDashboard("/hangfire");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();
app.MapHub<DoctorAppointmentSystem.Application.Hubs.NotificationHub>("/notificationHub");

// Automatically apply pending database migrations on startup
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<DoctorAppointmentSystem.Persistent.Context.ApplicationDbContext>();
	try
	{
		Console.WriteLine($"[Database Init] Provider Name: '{db.Database.ProviderName}'");
		await db.Database.ExecuteSqlRawAsync(@"
			ALTER TABLE IF EXISTS doctorappointment.""Users"" ADD COLUMN IF NOT EXISTS ""IsTwoFactorEnabled"" boolean NOT NULL DEFAULT false;
			ALTER TABLE IF EXISTS ""Users"" ADD COLUMN IF NOT EXISTS ""IsTwoFactorEnabled"" boolean NOT NULL DEFAULT false;
			ALTER TABLE IF EXISTS doctorappointment.""UserPatients"" ADD COLUMN IF NOT EXISTS ""ConsentDeclared"" boolean NOT NULL DEFAULT true;
			ALTER TABLE IF EXISTS doctorappointment.""UserPatients"" ADD COLUMN IF NOT EXISTS ""IsDependent"" boolean NOT NULL DEFAULT false;
			ALTER TABLE IF EXISTS doctorappointment.""UserPatients"" ADD COLUMN IF NOT EXISTS ""OtpChannel"" character varying(20);
			ALTER TABLE IF EXISTS doctorappointment.""UserPatients"" ADD COLUMN IF NOT EXISTS ""OtpExpiryTime"" timestamp with time zone;
			ALTER TABLE IF EXISTS doctorappointment.""UserPatients"" ADD COLUMN IF NOT EXISTS ""VerificationOtp"" character varying(10);
		");
		Console.WriteLine("[Database Init] Successfully executed raw DDL schema column checks.");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[Database Init Error] Postgres raw DDL schema check info: {ex.Message}");
	}

	try
	{
		await db.Database.MigrateAsync();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Database Migration Info: {ex.Message}");
	}

	await DoctorAppointmentSystem.Persistent.DbInitializer.SeedAsync(db);
}

// Subscribe static EmailSender event to resolved IEmailService in background tasks
DoctorAppointmentSystem.Application.Services.EmailSender.EmailSendEvent += (sender, e) =>
{
	Task.Run(async () =>
	{
		try
		{
			using var scope = app.Services.CreateScope();
			var emailService = scope.ServiceProvider.GetRequiredService<DoctorAppointmentSystem.Application.Services.IEmailService>();
			await emailService.SendEmailAsync(e.Email, e.Subject, e.Body);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error sending background email via EmailSender event: {ex.Message}");
		}
	});
};

app.Run();
