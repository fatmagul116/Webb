using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models.ViewModels; // ViewModel için bu satır şart!
using System.Linq;
using System.Threading.Tasks;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- DASHBOARD (ANA SAYFA) ---
        public async Task<IActionResult> Index()
        {
            // 1. Onay bekleyen Antrenör Kullanıcıları
            ViewBag.PendingTrainers = await _context.AppUsers
                .Where(u => u.Role == "Antrenör" && !u.IsApproved)
                .ToListAsync();

            // 2. Onay bekleyen Randevular (İlişkili tablolarla beraber)
            ViewBag.PendingAppointments = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.Status == "Onay Bekliyor")
                .ToListAsync();

            return View();
        }

        // --- DETAY SAYFASI (ViewModel Kullanacak Şekilde Düzeltildi) ---
        public async Task<IActionResult> Details(int id)
        {
            // 1. Kullanıcıyı Bul
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            // 2. Eğitmen Detaylarını Bul (İsim eşleşmesiyle)
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == user.FullName);

            // 3. Verileri ViewModel Paketine Koy
            var model = new TrainerDetailsViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsApproved = user.IsApproved,

                // Trainer bilgileri (Eğer trainer null ise hata vermesin diye önlemler)
                Specialization = trainer?.Specialization ?? "Belirtilmemiş",
                WorkingHours = trainer != null ? $"{trainer.AvailableFrom:hh\\:mm} - {trainer.AvailableTo:hh\\:mm}" : "-",
                DaysOff = trainer?.DaysOff ?? "",
                TrainerId = trainer?.Id ?? 0
            };

            // 4. Paketi Sayfaya Gönder
            return View(model);
        }

        // --- KULLANICI İŞLEMLERİ (ONAYLA / SİL) ---

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                user.IsApproved = true; // Onay ver
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                // 1. Önce Trainer tablosundaki kaydını da bulup silelim (Temizlik)
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == user.FullName);
                if (trainer != null)
                {
                    _context.Trainers.Remove(trainer);
                }

                // 2. Kullanıcıyı sil
                _context.AppUsers.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // --- RANDEVU İŞLEMLERİ (ONAYLA / REDDET) ---

        [HttpPost]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // ÇAKIŞMA KONTROLÜ
                bool isConflict = _context.Appointments.Any(x =>
                    x.TrainerId == appointment.TrainerId &&
                    x.Date == appointment.Date &&
                    x.Time == appointment.Time &&
                    x.Status == "Onaylandı" &&
                    x.Id != id);

                if (isConflict)
                {
                    TempData["Error"] = "Hata: Bu antrenörün o saatte zaten onaylı bir dersi var.";
                    return RedirectToAction("Index");
                }

                appointment.Status = "Onaylandı";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu onaylandı.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Reddedildi"; // Admin reddettiği için
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
