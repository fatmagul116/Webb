using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class Session
    {
        [Key]
        public int Id { get; set; }

        // Hangi antrenör açtı?
        public int TrainerId { get; set; }
        public virtual Trainer Trainer { get; set; }

        [Required(ErrorMessage = "Ders adı zorunludur.")]
        public string ClassName { get; set; } // Örn: "Sabah Pilatesi", "Yoğun Fitness"

        [Required]
        public DateTime SessionDate { get; set; } // Tarih ve Başlangıç Saati

        [Required]
        public int Duration { get; set; } // Dakika (Örn: 60)

        [Required]
        public decimal Price { get; set; } // Ücret

        [Required]
        public int Quota { get; set; } // Kontenjan (Örn: 10 kişi)

        public int CurrentCount { get; set; } = 0; // Şu an kaç kişi kayıtlı?

        // Onay Durumu (null: Bekliyor, true: Onaylandı, false: Red)
        public bool? IsApproved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; // 2 gün kuralı için
    }
}
