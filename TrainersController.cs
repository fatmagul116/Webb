using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;
using System.Security.Claims;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    [Authorize(Roles = "Antrenör")] // Sadece Antrenörler girebilir
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DASHBOARD (ÖZET EKRANI) - İSTEĞE BAĞLI
        // ==========================================
        // Antrenör panelinin ana sayfası burası olabilir
        public async Task<IActionResult> Index()
        {
            var trainerName = User.Identity.Name;
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);
            if (trainer == null) return RedirectToAction("Login", "Account");

            // Hem Grup Derslerini hem Bireysel Randevuları görüntüle
            var model = new
            {
                UpcomingSessions = await _context.Sessions
                    .Where(s => s.TrainerId == trainer.Id && s.SessionDate > DateTime.Now)
                    .OrderBy(s => s.SessionDate)
                    .ToListAsync(),

                PendingAppointments = await _context.Appointments
                    .Include(a => a.AppUser) // Öğrenci bilgisi
                    .Include(a => a.Service) // Hizmet bilgisi
                    .Where(a => a.TrainerId == trainer.Id && a.Status == "Onay Bekliyor")
                    .OrderBy(a => a.Date).ThenBy(a => a.Time)
                    .ToListAsync()
            };

            return View(model);
        }

        // ==========================================
        // 2. GRUP DERSİ (SEANS) OLUŞTURMA
        // ==========================================
        public async Task<IActionResult> CreateSession()
        {
            var trainerName = User.Identity.Name;
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);

            if (trainer == null) return RedirectToAction("Index", "Home");

            // --- UZMANLIK FİLTRESİ ---
            var allServices = await _context.Services.ToListAsync();
            var allowedServices = allServices
                .Where(s => trainer.Specialization.Contains(s.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!allowedServices.Any())
            {
                TempData["Error"] = "Uzmanlık alanınıza uygun hizmet bulunamadı.";
                return RedirectToAction("Index");
            }

            ViewBag.AllowedServices = new SelectList(allowedServices, "Name", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSession(Session model)
        {
            ModelState.Remove("Trainer"); // Trainer formdan gelmediği için validasyonu kaldır

            var trainerName = User.Identity.Name;
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);

            if (trainer == null) return RedirectToAction("Login", "Account");

            async Task RefillDropdown()
            {
                var allServices = await _context.Services.ToListAsync();
                var allowed = allServices.Where(s => trainer.Specialization.Contains(s.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                ViewBag.AllowedServices = new SelectList(allowed, "Name", "Name");
            }

            // GÜVENLİK VE ÇAKIŞMA KONTROLLERİ
            if (!trainer.Specialization.Contains(model.ClassName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Sadece uzmanlık alanınızda ders açabilirsiniz!");
                await RefillDropdown(); return View(model);
            }

            TimeSpan start = model.SessionDate.TimeOfDay;
            TimeSpan end = start.Add(TimeSpan.FromMinutes(model.Duration));

            if (start < new TimeSpan(9, 0, 0) || end > new TimeSpan(22, 0, 0))
            {
                ModelState.AddModelError("", "Seanslar 09:00 - 22:00 arasında olmalıdır.");
                await RefillDropdown(); return View(model);
            }

            bool conflict = await _context.Sessions.AnyAsync(s =>
                s.TrainerId == trainer.Id &&
                s.SessionDate < model.SessionDate.AddMinutes(model.Duration) &&
                s.SessionDate.AddMinutes(s.Duration) > model.SessionDate);

            if (conflict)
            {
                ModelState.AddModelError("", "Bu saatte başka bir grup dersiniz var.");
                await RefillDropdown(); return View(model);
            }

            if (ModelState.IsValid)
            {
                model.TrainerId = trainer.Id;
                model.IsApproved = null;
                model.CreatedAt = DateTime.Now;
                model.CurrentCount = 0;

                _context.Sessions.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Grup dersi talebi oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            await RefillDropdown();
            return View(model);
        }

        // ==========================================
        // 3. BİREYSEL RANDEVULARIM (YENİ EKLENDİ)
        // ==========================================
        public async Task<IActionResult> MyAppointments()
        {
            var trainerName = User.Identity.Name;
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);
            if (trainer == null) return RedirectToAction("Login", "Account");

            // Hocaya ait tüm bireysel randevuları getir
            var appointments = await _context.Appointments
                .Include(a => a.AppUser) // Öğrenci Adı
                .Include(a => a.Service) // Ders Türü
                .Where(a => a.TrainerId == trainer.Id)
                .OrderByDescending(a => a.Date).ThenBy(a => a.Time)
                .ToListAsync();

            return View(appointments);
        }

        // Bireysel Randevu Onayla
        [HttpPost]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt != null)
            {
                appt.Status = "Onaylandı";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bireysel randevu onaylandı.";
            }
            return RedirectToAction(nameof(MyAppointments));
        }

        // Bireysel Randevu Reddet
        [HttpPost]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt != null)
            {
                appt.Status = "Reddedildi";
                await _context.SaveChangesAsync();
                TempData["Warning"] = "Bireysel randevu reddedildi.";
            }
            return RedirectToAction(nameof(MyAppointments));
        }

        // ==========================================
        // 4. PROFİL DÜZENLEME
        // ==========================================
        public async Task<IActionResult> EditProfile()
        {
            var trainerName = User.Identity.Name;
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);
            if (trainer == null) return RedirectToAction("Index", "Home");

            ViewBag.GymList = new SelectList(_context.Gyms, "Id", "Address", trainer.GymId);
            return View(trainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Trainer model)
        {
            var trainerName = User.Identity.Name;
            var existingTrainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);
            if (existingTrainer == null) return RedirectToAction("Index", "Home");

            existingTrainer.Specialization = model.Specialization;
            existingTrainer.AvailableFrom = model.AvailableFrom;
            existingTrainer.AvailableTo = model.AvailableTo;
            existingTrainer.DaysOff = model.DaysOff;
            existingTrainer.GymId = model.GymId;

            _context.Update(existingTrainer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profil güncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
