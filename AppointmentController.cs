using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Models;
using ProjeOdeviWeb_G231210048.Data;
using System.Security.Claims;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointments (LİSTELEME)
        public async Task<IActionResult> Index()
        {
            var appointmentsQuery = _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Service)
                .Include(a => a.Trainer)
                .AsQueryable();

            // 1. ANTRENÖR: Sadece kendi derslerini görsün
            if (User.IsInRole("Antrenör"))
            {
                var trainerName = User.Identity.Name;
                appointmentsQuery = appointmentsQuery.Where(a => a.Trainer.FullName == trainerName);
            }
            // 2. ÜYE: Sadece kendi randevularını görsün
            else if (User.IsInRole("Uye"))
            {
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value);
                    appointmentsQuery = appointmentsQuery.Where(a => a.AppUserId == userId);
                }
            }
            // 3. ADMIN: Hepsini görür

            return View(await appointmentsQuery.OrderByDescending(a => a.Date).ThenBy(a => a.Time).ToListAsync());
        }

        // --- ANTRENÖR ONAY/RED İŞLEMLERİ (ÇAKIŞMA KONTROLLÜ) ---
        [HttpPost]
        [Authorize(Roles = "Antrenör")]
        public async Task<IActionResult> Approve(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // 1. ÇAKIŞMA KONTROLÜ: Hoca bu saatte dolu mu?
                bool isConflict = _context.Appointments.Any(x =>
                    x.TrainerId == appointment.TrainerId &&
                    x.Date == appointment.Date &&
                    x.Time == appointment.Time &&
                    x.Status == "Onaylandı" && // Sadece onaylılar sorun yaratır
                    x.Id != id);

                if (isConflict)
                {
                    TempData["Error"] = "Bu saat aralığında zaten onaylanmış başka bir randevunuz var!";
                    return RedirectToAction(nameof(Index));
                }

                appointment.Status = "Onaylandı";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu onaylandı.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Antrenör")]
        public async Task<IActionResult> Reject(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Reddedildi";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevu reddedildi.";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- DETAY ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Service)
                .Include(a => a.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // --- YENİ RANDEVU (SADECE ÜYE) ---
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> Create(int? serviceId)
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", serviceId);

            IQueryable<Trainer> trainersQuery = _context.Trainers;
            if (serviceId.HasValue)
            {
                var selectedService = await _context.Services.FindAsync(serviceId);
                if (selectedService != null)
                {
                    trainersQuery = trainersQuery.Where(t => t.Specialization.Contains(selectedService.Name) || selectedService.Name.Contains(t.Specialization));
                }
            }

            ViewData["TrainerId"] = new SelectList(trainersQuery, "Id", "FullName");

            var model = new Appointment();
            if (serviceId.HasValue) model.ServiceId = serviceId.Value;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> Create([Bind("Id,Date,Time,TrainerId,ServiceId,Duration")] Appointment appointment)
        {
            ModelState.Remove("AppUser");
            ModelState.Remove("Trainer");
            ModelState.Remove("Service");
            ModelState.Remove("Status");

            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null) appointment.AppUserId = int.Parse(userIdClaim.Value);

            appointment.Status = "Onay Bekliyor";

            if (ModelState.IsValid)
            {
                // 1. FİYAT HESAPLAMA
                var service = await _context.Services.FindAsync(appointment.ServiceId);
                decimal basePrice = service.Price;
                if (appointment.Duration == 30) appointment.TotalPrice = basePrice * 0.60m;
                else if (appointment.Duration == 60) appointment.TotalPrice = basePrice;
                else if (appointment.Duration == 90) appointment.TotalPrice = basePrice * 1.50m;
                else appointment.TotalPrice = basePrice;

                // 2. ÇALIŞMA SAATİ KONTROLÜ
                var trainer = await _context.Trainers.FindAsync(appointment.TrainerId);
                TimeSpan endTime = appointment.Time.Add(TimeSpan.FromMinutes(appointment.Duration));
                if (endTime > trainer.AvailableTo)
                {
                    ModelState.AddModelError("", "Seçilen süre ile antrenörün çıkış saati aşılıyor.");
                    ReloadLists(appointment); return View(appointment);
                }

                // 3. İZİN GÜNÜ KONTROLÜ
                string dayName = appointment.Date.DayOfWeek.ToString();
                if (!string.IsNullOrEmpty(trainer.DaysOff) && trainer.DaysOff.Contains(dayName))
                {
                    ModelState.AddModelError("", "Antrenör seçilen gün izinlidir.");
                    ReloadLists(appointment); return View(appointment);
                }

                // 4. ÜYE ÇAKIŞMA KONTROLÜ (YENİ) - Üye aynı saatte başka yerde olamaz
                bool memberConflict = _context.Appointments.Any(x =>
                    x.AppUserId == appointment.AppUserId &&
                    x.Date == appointment.Date &&
                    x.Time == appointment.Time &&
                    x.Status != "Reddedildi" && x.Status != "İptal Edildi");

                if (memberConflict)
                {
                    ModelState.AddModelError("", "Bu tarih ve saatte zaten başka bir randevunuz var.");
                    ReloadLists(appointment); return View(appointment);
                }

                // 5. HOCA DOLU MU?
                bool trainerConflict = _context.Appointments.Any(x =>
                    x.TrainerId == appointment.TrainerId &&
                    x.Date == appointment.Date &&
                    x.Time == appointment.Time &&
                    x.Status != "Reddedildi" && x.Status != "İptal Edildi");

                if (trainerConflict)
                {
                    ModelState.AddModelError("", "Seçilen tarih ve saatte antrenör dolu.");
                    ReloadLists(appointment); return View(appointment);
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ReloadLists(appointment); return View(appointment);
        }

        // --- DÜZENLEME (SADECE ÜYE - GÜVENLİ VE KISITLI) ---
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && appointment.AppUserId != int.Parse(userIdClaim.Value)) return Unauthorized();

            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "FullName", appointment.TrainerId);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,Time,TrainerId,ServiceId,Duration")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            var existingAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (existingAppointment == null) return NotFound();

            // 1. GÜVENLİK
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && existingAppointment.AppUserId != int.Parse(userIdClaim.Value)) return Unauthorized();

            // 2. SABİT DEĞERLER
            appointment.AppUserId = existingAppointment.AppUserId;
            appointment.Status = "Onay Bekliyor";

            ModelState.Remove("AppUser"); ModelState.Remove("Trainer"); ModelState.Remove("Service"); ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                if (appointment.Date < DateTime.Today)
                {
                    ModelState.AddModelError("Date", "Geçmişe randevu alınamaz.");
                    ReloadLists(appointment); return View(appointment);
                }

                // 3. ÇAKIŞMA KONTROLÜ (DÜZENLEME YAPARKEN DE KONTROL ET)
                bool memberConflict = _context.Appointments.Any(x =>
                    x.AppUserId == appointment.AppUserId &&
                    x.Date == appointment.Date &&
                    x.Time == appointment.Time &&
                    x.Status != "Reddedildi" && x.Status != "İptal Edildi" &&
                    x.Id != id); // Kendisi hariç

                if (memberConflict)
                {
                    ModelState.AddModelError("", "Bu saate alırsanız başka bir randevunuzla çakışıyor.");
                    ReloadLists(appointment); return View(appointment);
                }

                // Fiyat Hesapla
                var service = await _context.Services.FindAsync(appointment.ServiceId);
                decimal basePrice = service.Price;
                if (appointment.Duration == 30) appointment.TotalPrice = basePrice * 0.60m;
                else if (appointment.Duration == 60) appointment.TotalPrice = basePrice;
                else if (appointment.Duration == 90) appointment.TotalPrice = basePrice * 1.50m;
                else appointment.TotalPrice = basePrice;

                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ReloadLists(appointment); return View(appointment);
        }

        // --- İPTAL ETME / SİLME (SADECE ÜYE) ---
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && appointment.AppUserId != int.Parse(userIdClaim.Value)) return Unauthorized();

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            var userIdClaim = User.FindFirst("UserId");

            if (appointment != null && userIdClaim != null && appointment.AppUserId == int.Parse(userIdClaim.Value))
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- API METOTLARI ---
        [HttpGet]
        public async Task<IActionResult> GetTrainerHours(int trainerId)
        {
            var trainer = await _context.Trainers.FindAsync(trainerId);
            if (trainer == null) return NotFound();
            return Json(new { start = trainer.AvailableFrom.ToString(@"hh\:mm"), end = trainer.AvailableTo.ToString(@"hh\:mm") });
        }

        [HttpGet]
        public async Task<IActionResult> GetServicePrice(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();
            return Json(new { price = service.Price });
        }

        [HttpGet]
        public async Task<IActionResult> GetTrainersByService(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();
            var trainers = await _context.Trainers
                .Where(t => t.Specialization.Contains(service.Name) || service.Name.Contains(t.Specialization))
                .Select(t => new { id = t.Id, fullName = t.FullName })
                .ToListAsync();
            return Json(trainers);
        }

        // Yardımcı Metot: Kod tekrarını önlemek için
        private void ReloadLists(Appointment appointment)
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "FullName", appointment.TrainerId);
        }
    }
}
