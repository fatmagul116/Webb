using System.ComponentModel.DataAnnotations;

namespace ProjeOdeviWeb_G231210048.Models.ViewModels
{
    public class RegisterViewModel
    {
        // --- ORTAK ALANLAR ---
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } // "Uye" veya "Antrenör"

        // --- ÜYE İÇİN (Boy/Kilo/Yaş/Cinsiyet) ---

        // Yaş ve Cinsiyet AI için zorunlu olduğu için Required kalsın.
        // Not: Antrenörler de bunları girmek zorunda olacak.
        [Required(ErrorMessage = "Yaş gereklidir.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Cinsiyet seçiniz.")]
        public string Gender { get; set; }

        // DÜZELTME: int yerine int? (nullable) yapıyoruz.
        // Böylece boş bırakılırsa 0 değil, null olarak gelir.
        public int? Height { get; set; }
        public int? Weight { get; set; }

        // --- ANTRENÖR İÇİN (Uzmanlık, Saatler, İzin Günleri) ---
        public List<string>? SelectedSpecializations { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public List<string>? SelectedDaysOff { get; set; }
    }
}
