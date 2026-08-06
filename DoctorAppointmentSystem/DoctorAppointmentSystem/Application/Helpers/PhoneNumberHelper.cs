using System.Text.RegularExpressions;

namespace DoctorAppointmentSystem.Application.Helpers
{
	/// <summary>
	/// Central utility for phone number formatting, cleaning, and E.164 normalization.
	/// Ensures all mobile numbers stored in the database follow the exact unified E.164 format (+91XXXXXXXXXX).
	/// </summary>
	public static class PhoneNumberHelper
	{
		/// <summary>
		/// Normalizes any phone number input into standard E.164 format (+918160290198).
		/// Handles all variations:
		/// - "8160290198"         => "+918160290198"
		/// - "+91 8160290198"     => "+918160290198"
		/// - "918160290198"      => "+918160290198"
		/// - "+91-81602-90198"   => "+918160290198"
		/// - "+91 816-029-0198"  => "+918160290198"
		/// </summary>
		public static string Normalize(string? phone, string defaultCountryCode = "91")
		{
			if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

			// Extract all numeric digits
			var digitsOnly = Regex.Replace(phone.Trim(), @"[^\d]", "");

			if (string.IsNullOrEmpty(digitsOnly)) return string.Empty;

			// 10-digit national number (e.g. 8160290198) -> +918160290198
			if (digitsOnly.Length == 10)
			{
				return $"+{defaultCountryCode}{digitsOnly}";
			}

			// 12-digit number starting with 91 (e.g. 918160290198) -> +918160290198
			if (digitsOnly.Length == 12 && digitsOnly.StartsWith("91"))
			{
				return $"+{digitsOnly}";
			}

			// Any other length with digits -> prefix with '+'
			return $"+{digitsOnly}";
		}

		/// <summary>
		/// Validates if a phone number string represents a valid 10-to-15 digit mobile number.
		/// </summary>
		public static bool IsValidMobile(string? phone)
		{
			if (string.IsNullOrWhiteSpace(phone)) return false;
			var digitsOnly = Regex.Replace(phone.Trim(), @"[^\d]", "");
			return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
		}
	}
}
