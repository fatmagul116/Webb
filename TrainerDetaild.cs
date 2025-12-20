namespace ProjeOdeviWeb_G231210048.Models.ViewModels
{
    public class TrainerDetailsViewModel
    {
        // Kullanıcı Tablosundan Gelecekler
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsApproved { get; set; }

        // Eğitmen Tablosundan Gelecekler
        public string Specialization { get; set; } // Uzmanlık
        public string WorkingHours { get; set; }   // 09:00 - 17:00 gibi
        public string DaysOff { get; set; }        // İzin günleri
        public int TrainerId { get; set; }         // Eğitmen tablosundaki ID'si
    }
}
