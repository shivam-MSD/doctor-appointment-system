using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Addresses")]
	public class Address
	{
		[Key]
		public Guid AddressId { get; set; }

		[Required]
		public User User { get; set; }

		[Required]
		[MaxLength(100)]
		public string Country { get; set; }

		[Required]
		[MaxLength(100)]
		public string State { get; set; }

		[Required]
		[MaxLength(100)]
		public string City { get; set; }

		[Required]
		[MaxLength(150)]
		public string Area { get; set; }	

		[Required]
		[MaxLength(250)]
		public string Addressline1 { get; set; }

		[MaxLength(250)]
		public string Addressline2 { get; set; }
	}
}
