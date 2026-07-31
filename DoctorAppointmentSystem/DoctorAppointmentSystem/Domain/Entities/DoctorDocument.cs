using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents an uploaded medical credential document, certificate, or license file for a doctor.
	/// Holds document classification, file storage path, upload timestamp, and verification status.
	/// </summary>
	[Table("DoctorDocuments")]
	public class DoctorDocument
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the document record.
		/// </summary>
		[Key]
		public Guid DocumentId { get; set; }

		/// <summary>
		/// Gets or sets the doctor associated with this document.
		/// </summary>
		[Required]
		public Doctor Doctor { get; set; }

		/// <summary>
		/// Gets or sets the document type string (e.g. "MedicalLicense", "DegreeCertificate").
		/// </summary>
		[Required]
		[MaxLength(50)]
		public string DocumentType { get; set; }

		/// <summary>
		/// Gets or sets the UTC upload timestamp of the document.
		/// </summary>
		[Required]
		public DateTime UploadedDate { get; set; }

		/// <summary>
		/// Gets or sets the verification status of the document (e.g. "Pending", "Verified", "Rejected").
		/// </summary>
		[Required]
		[MaxLength(50)]
		public string Status { get; set; }

		/// <summary>
		/// Gets or sets the relative or absolute storage file path of the document.
		/// </summary>
		[Required]
		[MaxLength(500)]
		public string Path { get; set; }
	}
}
