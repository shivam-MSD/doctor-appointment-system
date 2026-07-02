using System.Security.Cryptography;
using System.Text;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Exceptions;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class UserService : IUserService
	{
		private readonly ApplicationDbContext _dbContext;

		public UserService(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<UserDto> GetUserProfileAsync(Guid userId)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException($"User with ID '{userId}' was not found.");
			}

			return new UserDto
			{
				UserId = user.UserId,
				Email = user.Email,
				IsActive = user.IsActive
			};
		}

		public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
		{
			var user = await _dbContext.Users.FindAsync(userId);
			if (user == null)
			{
				throw new NotFoundException($"User with ID '{userId}' was not found.");
			}

			var currentHashed = HashPassword(dto.CurrentPassword);
			if (user.PasswordHash != currentHashed)
			{
				throw new BadRequestException("The current password entered is incorrect.");
			}

			user.PasswordHash = HashPassword(dto.NewPassword);
			await _dbContext.SaveChangesAsync();
		}

		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}
	}
}
