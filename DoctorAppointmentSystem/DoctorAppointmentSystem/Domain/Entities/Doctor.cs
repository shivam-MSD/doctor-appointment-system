using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Doctors")]
	public class Doctor
	{
		[Key]
		public Guid DoctorId { get; set; }

		[Required]
		public User User { get; set; }

		[Required]
		public Specialization Specialization { get; set; }

		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		[Required]
		[Phone]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		[Required]
		public EGender Gender { get; set; }

		[Required]
		public DateTime DOB { get; set; }

		[Required]
		[MaxLength(150)]
		public string Qualification { get; set; }

		[Required]
		public int YearsOfExperience { get; set; }

		[Required]
		[MaxLength(50)]
		public string LicenceNumber { get; set; }

		[Required]
		public double ConsultationFee { get; set; }

		[Required]
		[MaxLength(150)]
		public string HospitalName { get; set; }

		[Required]
		public EVerificationStatus VerificationStatus { get; set; }

		public byte[]? ProfileImage { get; set; }

		[MaxLength(2000)]
		public string? AboutDoctor { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		public DateTime UpdatedDate { get; set; }
	}

	public enum EVerificationStatus
	{
		Pending,
		Verified,
		Rejected
	}
}
