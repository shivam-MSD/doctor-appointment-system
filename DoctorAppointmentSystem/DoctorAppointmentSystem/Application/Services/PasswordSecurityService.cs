using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Manages password storage using a two-tier strategy:
	/// 1. Redis (distributed cache) for fast retrieval
	/// 2. UserPasswords database table as persistent storage
	/// On read: check Redis first → fallback to DB → cache in Redis
	/// On write: save to DB first → cache in Redis
	/// </summary>
	public interface IPasswordSecurityService
	{
		/// <summary>Store password hash in both DB and Redis cache</summary>
		Task StorePasswordAsync(Guid userId, string passwordHash, TimeSpan? expiration = null);

		/// <summary>Retrieve password hash (Redis first, then DB fallback)</summary>
		Task<string> GetPasswordAsync(Guid userId);

		/// <summary>Verify password against stored hash</summary>
		Task<bool> VerifyPasswordAsync(Guid userId, string plainPassword, IPasswordHasher<object> passwordHasher);

		/// <summary>Remove password from Redis cache (on logout or password change)</summary>
		Task InvalidatePasswordAsync(Guid userId);

		/// <summary>Check if password exists</summary>
		Task<bool> PasswordExistsAsync(Guid userId);
	}

	public class PasswordSecurityService : IPasswordSecurityService
	{
		private readonly IDistributedCache _distributedCache;
		private readonly ApplicationDbContext _dbContext;
		private readonly string _cacheKeyPrefix = "password:";
		private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);

		public PasswordSecurityService(IDistributedCache distributedCache, ApplicationDbContext dbContext)
		{
			_distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		}

		/// <summary>
		/// Store password hash in both DB (UserPasswords table) and Redis.
		/// DB is the source of truth; Redis is the fast cache layer.
		/// </summary>
		public async Task StorePasswordAsync(Guid userId, string passwordHash, TimeSpan? expiration = null)
		{
			if (userId == Guid.Empty)
				throw new ArgumentException("UserId cannot be empty", nameof(userId));

			if (string.IsNullOrWhiteSpace(passwordHash))
				throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));

			// 1. Persist to database (UserPasswords table)
			var dbEntry = await _dbContext.UserPasswords.FirstOrDefaultAsync(up => up.UserId == userId);
			if (dbEntry == null)
			{
				dbEntry = new UserPassword { UserId = userId, PasswordHash = passwordHash };
				_dbContext.UserPasswords.Add(dbEntry);
			}
			else
			{
				dbEntry.PasswordHash = passwordHash;
			}
			await _dbContext.SaveChangesAsync();

			// 2. Cache in Redis
			try
			{
				var cacheKey = _cacheKeyPrefix + userId;
				var options = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
					SlidingExpiration = TimeSpan.FromHours(1)
				};
				await _distributedCache.SetStringAsync(cacheKey, passwordHash, options);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in StorePasswordAsync: {ex.Message}");
			}
		}

		/// <summary>
		/// Retrieve password hash: Redis first, then DB fallback.
		/// If found in DB but not Redis, re-cache it.
		/// </summary>
		public async Task<string> GetPasswordAsync(Guid userId)
		{
			if (userId == Guid.Empty)
				throw new ArgumentException("UserId cannot be empty", nameof(userId));

			// 1. Try Redis first
			var cacheKey = _cacheKeyPrefix + userId;
			string? cached = null;
			try
			{
				cached = await _distributedCache.GetStringAsync(cacheKey);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in GetPasswordAsync: {ex.Message}. Falling back to DB.");
			}

			if (!string.IsNullOrWhiteSpace(cached))
			{
				return cached;
			}

			// 2. Fallback to DB
			var dbEntry = await _dbContext.UserPasswords.FirstOrDefaultAsync(up => up.UserId == userId);
			if (dbEntry != null && !string.IsNullOrWhiteSpace(dbEntry.PasswordHash))
			{
				// Re-cache in Redis
				try
				{
					var options = new DistributedCacheEntryOptions
					{
						AbsoluteExpirationRelativeToNow = _defaultExpiration,
						SlidingExpiration = TimeSpan.FromHours(1)
					};
					await _distributedCache.SetStringAsync(cacheKey, dbEntry.PasswordHash, options);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in GetPasswordAsync while saving cache: {ex.Message}");
				}
				return dbEntry.PasswordHash;
			}

			return null;
		}

		/// <summary>
		/// Verify a plain password against the stored hash.
		/// </summary>
		public async Task<bool> VerifyPasswordAsync(Guid userId, string plainPassword, IPasswordHasher<object> passwordHasher)
		{
			if (userId == Guid.Empty)
				throw new ArgumentException("UserId cannot be empty", nameof(userId));

			if (string.IsNullOrWhiteSpace(plainPassword))
				return false;

			var hashedPassword = await GetPasswordAsync(userId);
			if (string.IsNullOrWhiteSpace(hashedPassword))
				return false;

			var result = passwordHasher.VerifyHashedPassword(null, hashedPassword, plainPassword);
			return result == PasswordVerificationResult.Success;
		}

		/// <summary>
		/// Invalidate/remove password from Redis cache.
		/// </summary>
		public async Task InvalidatePasswordAsync(Guid userId)
		{
			if (userId == Guid.Empty)
				throw new ArgumentException("UserId cannot be empty", nameof(userId));

			var cacheKey = _cacheKeyPrefix + userId;
			try
			{
				await _distributedCache.RemoveAsync(cacheKey);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DistributedCache Warning]: Redis connection failed in InvalidatePasswordAsync: {ex.Message}");
			}
		}

		/// <summary>
		/// Check if password exists in either Redis or DB.
		/// </summary>
		public async Task<bool> PasswordExistsAsync(Guid userId)
		{
			if (userId == Guid.Empty)
				throw new ArgumentException("UserId cannot be empty", nameof(userId));

			var password = await GetPasswordAsync(userId);
			return !string.IsNullOrWhiteSpace(password);
		}
	}
}
