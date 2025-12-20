using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class Trainer
    {
        public int Id { get; set; }
      

        [Required(ErrorMessage = "Antrenör adı zorunludur.")]
        [Display(Name = "Adı Soyadı")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanı belirtilmelidir.")]
        [Display(Name = "Uzmanlık Alanı")]
        public string Specialization { get; set; } // Örn: Kas geliştirme, Kilo verme

        // Antrenörün müsaitlik saatleri
        [Required]
        [Display(Name = "Başlangıç Saati")]
        [DataType(DataType.Time)]
        public TimeSpan AvailableFrom { get; set; }

        [Required]
        [Display(Name = "Bitiş Saati")]
        [DataType(DataType.Time)]
        public TimeSpan AvailableTo { get; set; }
        public string? DaysOff { get; set; }

        // Eski Hali:
        // public int GymId { get; set; }
        // public virtual Gym Gym { get; set; }  // HATA BURADA! (Sistem bunu "dolu gelmeli" sanıyor)

        // YENİ HALİ (Soru işareti ekle):
        public int GymId { get; set; }      // Bu int olarak kalmalı (Zorunlu)
        public virtual Gym? Gym { get; set; } // Bu ? almalı (Formda boş gelebilir)

        // Antrenörün verebildiği hizmetler
        // Eski hali: public virtual ICollection<Service> Services { get; set; }
        public virtual ICollection<Service>? Services { get; set; }
        public virtual ICollection<Session>? Sessions { get; set; }
    }
}
