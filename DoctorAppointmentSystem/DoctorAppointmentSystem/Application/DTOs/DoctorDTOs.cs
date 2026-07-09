using System.ComponentModel.DataAnnotations;
using DoctorAppointmentSystem.Domain.Entities;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class DoctorDto
	{
		public Guid DoctorId { get; set; }
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public Guid SpecializationId { get; set; }
		public string SpecializationName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MobileNo { get; set; }
		public string Qualification { get; set; }
		public int YearsOfExperience { get; set; }
		public string LicenceNumber { get; set; }
		public double ConsultationFee { get; set; }
		public string HospitalName { get; set; }
		public string VerificationStatus { get; set; }
		public string AboutDoctor { get; set; }
		public byte[] ProfileImage { get; set; }
		public string State { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }
	}

	public class DoctorRegisterDto
	{
		[Required]
		public Guid UserId { get; set; }

		[Required]
		public Guid SpecializationId { get; set; }

		[Required]
		public string Qualification { get; set; }

		[Required]
		[Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100.")]
		public int YearsOfExperience { get; set; }

		[Required]
		public string LicenceNumber { get; set; }

		[Required]
		[Range(0, 100000, ErrorMessage = "Consultation fee must be non-negative.")]
		public double ConsultationFee { get; set; }

		[Required]
		public string HospitalName { get; set; }

		public string AboutDoctor { get; set; }
	}

	public class DoctorUpdateDto
	{
		[Required]
		public Guid SpecializationId { get; set; }

		[Required]
		public string Qualification { get; set; }

		[Required]
		[Range(0, 100, ErrorMessage = "Years of experience must be between 0 and 100.")]
		public int YearsOfExperience { get; set; }

		[Required]
		public string LicenceNumber { get; set; }

		[Required]
		[Range(0, 100000, ErrorMessage = "Consultation fee must be non-negative.")]
		public double ConsultationFee { get; set; }

		[Required]
		public string HospitalName { get; set; }

		public string AboutDoctor { get; set; }

		public byte[] ProfileImage { get; set; }
	}
}