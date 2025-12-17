using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tarih seçimi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Randevu Tarihi")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Saat seçimi zorunludur.")]
        [DataType(DataType.Time)]
        [Display(Name = "Randevu Saati")]
        public TimeSpan Time { get; set; }

        [Display(Name = "Durum")]
        public string Status { get; set; } = "Onay Bekliyor";

        // --- İlişkiler ---
        [Display(Name = "Üye")]
        public int AppUserId { get; set; }
        public virtual AppUser? AppUser { get; set; }

        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }
        public virtual Trainer? Trainer { get; set; }

        [Display(Name = "Hizmet")]
        public int ServiceId { get; set; }
        public virtual Service? Service { get; set; }
        // Mevcut özelliklerin altına ekle:

        [Display(Name = "Süre (Dakika)")]
        public int Duration { get; set; } // 30, 60 veya 90



// ...

    [Display(Name = "Hesaplanan Ücret")]
    [Column(TypeName = "decimal(18,2)")] // <-- BU SATIRI EKLE
    public decimal TotalPrice { get; set; }
}
}
