using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RelayHR.Web.Models
{
    // Entidad de Usuarios existente (referencia base para la relación)
    public class User
    {
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // No se expone en DTOs
    }

    public class FreelancerCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<FreelancerProfile> FreelancerProfiles { get; set; } = new List<FreelancerProfile>();
    }

    public class FreelancerProfile
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int CategoryId { get; set; }
        public FreelancerCategory Category { get; set; } = null!;

        [Required, MaxLength(150)]
        public string ProfessionalTitle { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Experience { get; set; } = string.Empty;

        [Required]
        [Range(0, 999999.99, ErrorMessage = "La tarifa debe ser mayor o igual a cero.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "DOP";

        public bool IsActive { get; set; } = true;

        public ICollection<FreelancerPortfolio> Portfolios { get; set; } = new List<FreelancerPortfolio>();
        public ICollection<FreelancerAvailability> Availabilities { get; set; } = new List<FreelancerAvailability>();
    }

    public class FreelancerPortfolio
    {
        public int Id { get; set; }

        [Required]
        public int FreelancerProfileId { get; set; }
        public FreelancerProfile FreelancerProfile { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, Url, MaxLength(500)]
        public string ProjectUrl { get; set; } = string.Empty;
    }

    public class FreelancerAvailability
    {
        public int Id { get; set; }

        [Required]
        public int FreelancerProfileId { get; set; }
        public FreelancerProfile FreelancerProfile { get; set; } = null!;

        [Required]
        public DateOnly AvailableFrom { get; set; }

        [Required]
        public DateOnly AvailableUntil { get; set; }
    }
}