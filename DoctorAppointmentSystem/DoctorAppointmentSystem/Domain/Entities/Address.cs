using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a physical location address record associated with a user or clinic branch.
	/// Stores street lines, area, city, state, country, and postal pincode details.
	/// </summary>
	[Table("Addresses")]
	public class Address
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the address.
		/// </summary>
		[Key]
		public Guid AddressId { get; set; }

		/// <summary>
		/// Gets or sets the associated user account.
		/// </summary>
		[Required]
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the country name.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string Country { get; set; }

		/// <summary>
		/// Gets or sets the state or province name.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string State { get; set; }

		/// <summary>
		/// Gets or sets the city or municipality name.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string City { get; set; }

		/// <summary>
		/// Gets or sets the neighborhood or area locality name.
		/// </summary>
		[Required]
		[MaxLength(150)]
		public string Area { get; set; }	

		/// <summary>
		/// Gets or sets the postal pincode or ZIP code string.
		/// </summary>
		[Required]
		[MaxLength(20)]
		public string Pincode { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets primary street address line 1.
		/// </summary>
		[Required]
		[MaxLength(250)]
		public string Addressline1 { get; set; }

		/// <summary>
		/// Gets or sets optional secondary address line 2 (building, suite, floor).
		/// </summary>
		[MaxLength(250)]
		public string Addressline2 { get; set; }
	}
}
