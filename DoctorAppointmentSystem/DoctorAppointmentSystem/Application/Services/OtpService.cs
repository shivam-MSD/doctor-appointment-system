using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Service for generating, hashing, and validating OTPs.
	/// Provides a centralized, reusable OTP generation mechanism across the application.
	/// </summary>
	public interface IOtpService
	{
		/// <summary>Generate a random OTP (default 6 digits)</summary>
		string GenerateOtp(int length = 6);

		/// <summary>Hash the OTP using BCrypt for secure storage</summary>
		string HashOtp(string otp);

		/// <summary>Verify the OTP against its hash</summary>
		bool VerifyOtp(string plainOtp, string hashedOtp);
	}

	public class OtpService : IOtpService
	{
		private readonly IPasswordHasher<object> _passwordHasher;

		public OtpService(IPasswordHasher<object> passwordHasher)
		{
			_passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
		}

		/// <summary>
		/// Generate a cryptographically secure random OTP.
		/// Default: 6-digit numeric OTP (000000 to 999999)
		/// </summary>
		/// <param name="length">Length of the OTP (default 6)</param>
		/// <returns>Random OTP as string</returns>
		public string GenerateOtp(int length = 6)
		{
			if (length <= 0 || length > 12)
				throw new ArgumentException("OTP length must be between 1 and 12", nameof(length));

			using (var rng = new RNGCryptoServiceProvider())
			{
				byte[] tokenData = new byte[length];
				rng.GetBytes(tokenData);

				// Convert to numeric string (0-9)
				StringBuilder sb = new StringBuilder();
				foreach (byte b in tokenData)
				{
					sb.Append((b % 10).ToString());
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Hash OTP using Identity framework's PasswordHasher.
		/// Uses PBKDF2 with SHA1, 10,000 iterations.
		/// </summary>
		/// <param name="otp">Plain text OTP to hash</param>
		/// <returns>Hashed OTP</returns>
		public string HashOtp(string otp)
		{
			if (string.IsNullOrWhiteSpace(otp))
				throw new ArgumentException("OTP cannot be null or empty", nameof(otp));

			// Use a dummy object since PasswordHasher<T> is generic
			return _passwordHasher.HashPassword(null, otp);
		}

		/// <summary>
		/// Verify a plain OTP against its hash.
		/// </summary>
		/// <param name="plainOtp">Plain text OTP submitted by user</param>
		/// <param name="hashedOtp">Hashed OTP stored in database</param>
		/// <returns>True if match, false otherwise</returns>
		public bool VerifyOtp(string plainOtp, string hashedOtp)
		{
			if (string.IsNullOrWhiteSpace(plainOtp) || string.IsNullOrWhiteSpace(hashedOtp))
				return false;

			var result = _passwordHasher.VerifyHashedPassword(null, hashedOtp, plainOtp);
			return result == PasswordVerificationResult.Success;
		}
	}
}
