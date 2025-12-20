using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjeOdeviWeb_G231210048.Models
{
    public class SessionRegistration
    {
        [Key]
        public int Id { get; set; }

        // Hangi Ders?
        public int SessionId { get; set; }
        public virtual Session Session { get; set; }

        // Hangi Üye?
        public int AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
