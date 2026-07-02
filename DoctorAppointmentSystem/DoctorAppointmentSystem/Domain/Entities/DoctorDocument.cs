using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("DoctorDocuments")]
	public class DoctorDocument
	{
		[Key]
		public Guid DocumentId { get; set; }

		[Required]
		public Doctor Doctor { get; set; }

		[Required]
		[MaxLength(50)]
		public string DocumentType { get; set; }

		[Required]
		public DateTime UploadedDate { get; set; }

		[Required]
		[MaxLength(50)]
		public string Status { get; set; }

		[Required]
		[MaxLength(500)]
		public string Path { get; set; }
	}
}
