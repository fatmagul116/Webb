using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Salon adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Salon Adı")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Adres bilgisi zorunludur.")]
        [Display(Name = "Adres")]
        public string Address { get; set; }

        // Çalışma saatleri
        [Required]
        [Display(Name = "Açılış Saati")]
        [DataType(DataType.Time)]
        public TimeSpan OpeningTime { get; set; }

        [Required]
        [Display(Name = "Kapanış Saati")]
        [DataType(DataType.Time)]
        public TimeSpan ClosingTime { get; set; }

        // ? işareti "Bu liste boş olabilir, hata verme" demektir.
        public virtual ICollection<Service>? Services { get; set; }
        public virtual ICollection<Trainer>? Trainers { get; set; }
    }
}
