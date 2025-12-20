using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models.ViewModels;
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

        // ============================================================
        // 1. DASHBOARD (ANA SAYFA - ONAY BEKLEYENLER)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // A) Onay bekleyen Antrenör Kullanıcıları
            ViewBag.PendingTrainers = await _context.AppUsers
                .Where(u => u.Role == "Antrenör" && !u.IsApproved)
                .ToListAsync();

            // B) Onay bekleyen Bireysel Randevular
            ViewBag.PendingAppointments = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.Status == "Onay Bekliyor")
                .ToListAsync();

            // C) Onay bekleyen Grup Seansları (Sessions)
            ViewBag.PendingSessions = await _context.Sessions
                .Include(s => s.Trainer)
                .Where(s => s.IsApproved == null) // Henüz karar verilmemişler
                .ToListAsync();

            return View();
        }

        // ============================================================
        // 2. TÜM KAYITLARI GÖRME (YENİ EKLENENLER)
        // ============================================================

        // Tüm Grup Seansları (Geçmiş ve Gelecek)
        public async Task<IActionResult> AllSessions()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Trainer)
                .OrderByDescending(s => s.SessionDate) // En yeniden eskiye
                .ToListAsync();
            return View(sessions);
        }

        // Tüm Bireysel Randevular (Geçmiş ve Gelecek)
        public async Task<IActionResult> AllAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .OrderByDescending(a => a.Date).ThenBy(a => a.Time)
                .ToListAsync();
            return View(appointments);
        }

        // ============================================================
        // 3. DETAY VE ONAYLAMA İŞLEMLERİ
        // ============================================================

        // Antrenör Detay Sayfası
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == user.FullName);

            var model = new TrainerDetailsViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsApproved = user.IsApproved,
                Specialization = trainer?.Specialization ?? "Belirtilmemiş",
                WorkingHours = trainer != null ? $"{trainer.AvailableFrom:hh\\:mm} - {trainer.AvailableTo:hh\\:mm}" : "-",
                DaysOff = trainer?.DaysOff ?? "",
                TrainerId = trainer?.Id ?? 0
            };

            return View(model);
        }

        // Antrenör Onayla
        [HttpPost]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // Antrenör Sil / Reddet
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == user.FullName);
                if (trainer != null)
                {
                    _context.Trainers.Remove(trainer);
                }
                _context.AppUsers.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // Bireysel Randevu Onayla
        [HttpPost]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // Çakışma Kontrolü
                bool isConflict = await _context.Appointments.AnyAsync(x =>
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

        // Bireysel Randevu Reddet
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Reddedildi";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // Grup Seansı Onayla / Reddet (Dashboard'dan gelen)
        [HttpPost]
        public async Task<IActionResult> ApproveSession(int id, bool decision)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session != null)
            {
                session.IsApproved = decision; // true=Onay, false=Red
                await _context.SaveChangesAsync();

                if (decision) TempData["Success"] = "Grup seansı onaylandı.";
                else TempData["Warning"] = "Grup seansı reddedildi.";
            }
            return RedirectToAction("Index");
        }

        // Grup Seansı SİL (AllSessions sayfasından gelen)
        [HttpPost]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Seans kalıcı olarak silindi.";
            }
            // İşlemi nerede yaptıysa oraya dönsün
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
