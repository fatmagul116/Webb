using System.ComponentModel.DataAnnotations;

namespace ProjeOdeviWeb_G231210048.Models.ViewModels
{
    public class AiRequestViewModel
    {
        [Required(ErrorMessage = "Yaş gereklidir.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Kilo gereklidir.")]
        public int Weight { get; set; }

        [Required(ErrorMessage = "Boy gereklidir.")]
        public int Height { get; set; }

        [Required(ErrorMessage = "Cinsiyet seçiniz.")]
        public string Gender { get; set; } // Kadın / Erkek

        [Required(ErrorMessage = "Hedef seçiniz.")]
        public string Goal { get; set; } // Kilo Vermek, Kas Yapmak, Formu Korumak

        [Required(ErrorMessage = "Aktivite düzeyi seçiniz.")]
        public string ActivityLevel { get; set; } // Hareketsiz, Orta, Çok Hareketli

        // AI'dan gelen cevabı ekranda göstermek için
        public string? AiResponse { get; set; }
    }
}
