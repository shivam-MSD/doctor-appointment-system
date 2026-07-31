using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DoctorAppointmentSystem.Domain.Entities;

namespace DoctorAppointmentSystem.Application.DTOs
{
	/// <summary>
	/// Data transfer object carrying complete doctor profile details, qualifications, fees, and associated clinic branches.
	/// </summary>
	public class DoctorDto
	{
		/// <summary>Gets or sets unique doctor ID.</summary>
		public Guid DoctorId { get; set; }
		/// <summary>Gets or sets associated user ID.</summary>
		public Guid UserId { get; set; }
		/// <summary>Gets or sets email address.</summary>
		public string Email { get; set; }
		/// <summary>Gets or sets specialization ID.</summary>
		public Guid SpecializationId { get; set; }
		/// <summary>Gets or sets specialization name string.</summary>
		public string SpecializationName { get; set; }
		/// <summary>Gets or sets first name.</summary>
		public string FirstName { get; set; }
		/// <summary>Gets or sets last name.</summary>
		public string LastName { get; set; }
		/// <summary>Gets or sets gender string.</summary>
		public string Gender { get; set; }
		/// <summary>Gets or sets mobile contact number.</summary>
		public string MobileNo { get; set; }
		/// <summary>Gets or sets qualification degrees string.</summary>
		public string Qualification { get; set; }
		/// <summary>Gets or sets years of experience.</summary>
		public int YearsOfExperience { get; set; }
		/// <summary>Gets or sets medical license number.</summary>
		public string LicenceNumber { get; set; }
		/// <summary>Gets or sets consultation fee.</summary>
		public double ConsultationFee { get; set; }
		/// <summary>Gets or sets verification status string.</summary>
		public string VerificationStatus { get; set; }
		/// <summary>Gets or sets bio summary text.</summary>
		public string AboutDoctor { get; set; }
		/// <summary>Gets or sets profile image byte array.</summary>
		public byte[] ProfileImage { get; set; }
		/// <summary>Gets or sets state location string.</summary>
		public string State { get; set; } = string.Empty;
		/// <summary>Gets or sets city location string.</summary>
		public string City { get; set; } = string.Empty;
		/// <summary>Gets or sets UTC creation date.</summary>
		public DateTime CreatedDate { get; set; }
		/// <summary>Gets or sets UTC update date.</summary>
		public DateTime UpdatedDate { get; set; }
		/// <summary>Gets or sets calculated age in years.</summary>
		public int Age { get; set; }
		/// <summary>Gets or sets list of associated clinic branches.</summary>
		public List<ClinicBasicDto> Clinics { get; set; } = new List<ClinicBasicDto>();
	}

	/// <summary>
	/// Lightweight data transfer object carrying basic clinic branch information.
	/// </summary>
	public class ClinicBasicDto
	{
		/// <summary>Gets or sets clinic ID.</summary>
		public Guid ClinicId { get; set; }
		/// <summary>Gets or sets clinic branch name.</summary>
		public string ClinicName { get; set; }
		/// <summary>Gets or sets clinic facility type.</summary>
		public string ClinicType { get; set; }
		/// <summary>Gets or sets state location.</summary>
		public string State { get; set; }
		/// <summary>Gets or sets city location.</summary>
		public string City { get; set; }
		/// <summary>Gets or sets area locality.</summary>
		public string Area { get; set; }
		/// <summary>Gets or sets clinic contact phone number.</summary>
		public string? ContactNumber { get; set; }
		/// <summary>Gets or sets availability status boolean.</summary>
		public bool IsAvailable { get; set; } = true;
		/// <summary>Gets or sets unavailability reason text.</summary>
		public string? UnavailabilityReason { get; set; }
		/// <summary>Gets or sets doctor availability flag at this branch.</summary>
		public bool IsDoctorAvailable { get; set; } = true;
		/// <summary>Gets or sets doctor unavailability reason text.</summary>
		public string? DoctorUnavailabilityReason { get; set; }
		/// <summary>Gets or sets Google Maps location link.</summary>
		public string? LocationLink { get; set; }
		/// <summary>Gets or sets open days string.</summary>
		public string? OpenDays { get; set; }
		/// <summary>Gets or sets operating start time.</summary>
		public string? StartTime { get; set; }
		/// <summary>Gets or sets operating end time.</summary>
		public string? EndTime { get; set; }
		/// <summary>Gets or sets booking window start date.</summary>
		public DateTime? BookingWindowStartDate { get; set; }
		/// <summary>Gets or sets booking window end date.</summary>
		public DateTime? BookingWindowEndDate { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying professional qualification details during doctor registration.
	/// </summary>
	public class DoctorRegisterDto
	{
		/// <summary>Gets or sets user account ID.</summary>
		[Required]
		public Guid UserId { get; set; }

		/// <summary>Gets or sets medical specialization ID.</summary>
		[Required]
		public Guid SpecializationId { get; set; }

		/// <summary>Gets or sets qualification degrees.</summary>
		[Required]
		public string Qualification { get; set; }

		/// <summary>Gets or sets years of experience.</summary>
		[Required]
		[Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100.")]
		public int YearsOfExperience { get; set; }

		/// <summary>Gets or sets medical license number.</summary>
		[Required]
		public string LicenceNumber { get; set; }

		/// <summary>Gets or sets consultation fee.</summary>
		[Required]
		[Range(0, 100000, ErrorMessage = "Consultation fee must be non-negative.")]
		public double ConsultationFee { get; set; }

		/// <summary>Gets or sets bio text.</summary>
		public string AboutDoctor { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying updated profile fields for a doctor.
	/// </summary>
	public class DoctorUpdateDto
	{
		/// <summary>Gets or sets specialization ID.</summary>
		[Required]
		public Guid SpecializationId { get; set; }

		/// <summary>Gets or sets qualification degrees.</summary>
		[Required]
		public string Qualification { get; set; }

		/// <summary>Gets or sets years of experience.</summary>
		[Required]
		[Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100.")]
		public int YearsOfExperience { get; set; }

		/// <summary>Gets or sets medical license number.</summary>
		[Required]
		public string LicenceNumber { get; set; }

		/// <summary>Gets or sets consultation fee.</summary>
		[Required]
		[Range(0, 100000, ErrorMessage = "Consultation fee must be non-negative.")]
		public double ConsultationFee { get; set; }

		/// <summary>Gets or sets bio summary text.</summary>
		public string AboutDoctor { get; set; }

		/// <summary>Gets or sets profile image byte array.</summary>
		public byte[] ProfileImage { get; set; }
	}
}