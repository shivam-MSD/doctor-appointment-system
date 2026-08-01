using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a medical practitioner / doctor entity within the HealSync system.
	/// Contains qualifications, specialization references, license verification status, and consultation fees.
	/// </summary>
	[Table("Doctors")]
	public class Doctor
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the doctor.
		/// </summary>
		[Key]
		public Guid DoctorId { get; set; }

		/// <summary>
		/// Gets or sets the associated user authentication record for this doctor.
		/// </summary>
		[Required]
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the medical specialization area of the doctor.
		/// </summary>
		[Required]
		public Specialization Specialization { get; set; }

		/// <summary>
		/// Gets or sets the first name of the doctor.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		/// <summary>
		/// Gets or sets the last name of the doctor.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		/// <summary>
		/// Gets or sets the contact mobile number of the doctor.
		/// </summary>
		[Required]
		[Phone]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		/// <summary>
		/// Gets or sets the gender enumeration of the doctor.
		/// </summary>
		[Required]
		public EGender Gender { get; set; }

		/// <summary>
		/// Gets or sets the date of birth of the doctor.
		/// </summary>
		[Required]
		public DateTime DOB { get; set; }

		/// <summary>
		/// Gets or sets the academic qualifications and medical degrees held by the doctor.
		/// </summary>
		[Required]
		[MaxLength(150)]
		public string Qualification { get; set; }

		/// <summary>
		/// Gets or sets the total years of professional medical experience.
		/// </summary>
		[Required]
		public int YearsOfExperience { get; set; }

		/// <summary>
		/// Gets or sets the medical council registration or license number.
		/// </summary>
		[Required]
		[MaxLength(50)]
		public string LicenceNumber { get; set; }

		/// <summary>
		/// Gets or sets the standard consultation fee charged for appointments.
		/// </summary>
		[Required]
		public double ConsultationFee { get; set; }

		/// <summary>
		/// Gets or sets the administrative verification status of the doctor (Pending, Verified, Rejected, UpdatedPending).
		/// </summary>
		[Required]
		public EVerificationStatus VerificationStatus { get; set; } = EVerificationStatus.Pending;

		/// <summary>
		/// Gets or sets the profile image byte array for the doctor.
		/// </summary>
		public byte[]? ProfileImage { get; set; }

		/// <summary>
		/// Gets or sets the bio and professional summary text of the doctor.
		/// </summary>
		[MaxLength(2000)]
		public string? AboutDoctor { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the doctor profile was created.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Gets or sets the UTC timestamp when the doctor profile was last updated.
		/// </summary>
		public DateTime UpdatedDate { get; set; }

		/// <summary>
		/// Gets or sets the auto reschedule date setting for doctor leave / availability shifts.
		/// </summary>
		public DateTime? AutoRescheduleDate { get; set; }

		/// <summary>
		/// Gets or sets the navigation collection of clinic branches associated with this doctor.
		/// </summary>
		public ICollection<Clinic> Clinics { get; set; }
	}

	/// <summary>
	/// Enumeration representing verification status for doctors and clinics.
	/// </summary>
	public enum EVerificationStatus
	{
		Pending,
		Verified,
		Rejected,
		UpdatedPending
	}
}
