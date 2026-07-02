using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<DoctorAppointmentSystem.Persistent.Context.ApplicationDbContext>(options =>
	options.UseInMemoryDatabase("DoctorAppointmentDb"));

builder.Services.AddExceptionHandler<DoctorAppointmentSystem.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IAuthService, DoctorAppointmentSystem.Application.Services.AuthService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IFamilyService, DoctorAppointmentSystem.Application.Services.FamilyService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IUserService, DoctorAppointmentSystem.Application.Services.UserService>();
builder.Services.AddScoped<DoctorAppointmentSystem.Application.Services.IPatientService, DoctorAppointmentSystem.Application.Services.PatientService>();
builder.Services.AddDistributedMemoryCache();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
