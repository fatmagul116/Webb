using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Hizmet Adı")]
        public string Name { get; set; }

        // ✅ BURASI DurationMinutes OLARAK KALIYOR
        [Required]
        [Range(15, 240, ErrorMessage = "Süre 15 ile 240 dakika arasında olmalıdır.")]
        [Display(Name = "Süre (Dakika)")]
        public int DurationMinutes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000, ErrorMessage = "Geçerli bir ücret giriniz.")]
        [Display(Name = "Ücret (TL)")]
        public decimal Price { get; set; }

        public int GymId { get; set; }
        public virtual Gym? Gym { get; set; }

        public virtual ICollection<Trainer>? Trainers { get; set; }
    }
}
