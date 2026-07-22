using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointmentSystem.Domain.Entities
{
    /// <summary>
    /// Junction table linking Admin → Clinic.
    /// One admin can manage many clinics, but each clinic can have at most one admin
    /// (enforced by the unique index on ClinicId).
    /// </summary>
    [Table("AdminClinics")]
    [Index(nameof(ClinicId), IsUnique = true)]
    public class AdminClinic
    {
        [Key]
        public Guid AdminClinicId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AdminId { get; set; }
        public Admin Admin { get; set; }

        [Required]
        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
