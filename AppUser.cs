using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "E-Posta zorunludur")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Rol")]
        public string Role { get; set; }

        public double? Height { get; set; }
        public double? Weight { get; set; }

        
         public int? Age { get; set; }      // Yaş
         public string? Gender { get; set; } // Cinsiyet (Kadın/Erkek)
       

        [Display(Name = "Hesap Onaylı mı?")]
        public bool IsApproved { get; set; } = false;

        // --- BU KISMI EKLEMEN GEREKİYOR ---
        // Bir üyenin birden fazla randevusu olabilir, o yüzden Liste (ICollection) yapıyoruz.
        public virtual ICollection<Appointment>? Appointments { get; set; }
        // -----------------------------------
    }
}
