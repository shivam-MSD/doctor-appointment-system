using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

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
});

builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<DoctorAppointmentSystem.Persistent.Context.ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

builder.Services.AddExceptionHandler<DoctorAppointmentSystem.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAuthService, DoctorAppointmentSystem.Application.Services.AuthService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IFamilyService, DoctorAppointmentSystem.Application.Services.FamilyService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IUserService, DoctorAppointmentSystem.Application.Services.UserService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IPatientService, DoctorAppointmentSystem.Application.Services.PatientService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAdminService, DoctorAppointmentSystem.Application.Services.AdminService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAppointmentService, DoctorAppointmentSystem.Application.Services.AppointmentService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IClinicService, DoctorAppointmentSystem.Application.Services.ClinicService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IEmailService, DoctorAppointmentSystem.Application.Services.EmailService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.INotificationService, DoctorAppointmentSystem.Application.Services.NotificationService>();

// Register background services
builder.Services.AddHostedService<DoctorAppointmentSystem.Application.BackgroundServices.AppointmentCleanupService>();

builder.Services.AddSignalR();
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
	options.InstanceName = "HealSync_";
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/openapi/v1.json", "Doctor Appointment System API v1");
		options.RoutePrefix = "swagger";
	});
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DoctorAppointmentSystem.Application.Hubs.NotificationHub>("/notificationHub");

// Automatically apply pending database migrations on startup
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<DoctorAppointmentSystem.Persistent.Context.ApplicationDbContext>();
	db.Database.Migrate();
	await DoctorAppointmentSystem.Persistent.DbInitializer.SeedAsync(db);
}

app.Run();
