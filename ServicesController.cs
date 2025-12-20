using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. VİTRİN KISMI (HERKES GÖREBİLİR)
        // ============================================================

        // GET: Services (Kategorileri Listeler)
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.Include(s => s.Gym).ToListAsync();
            return View(services);
        }

        // GET: Services/Details/5 (SEÇİLEN HİZMETİN SEANSLARINI VE EĞİTMENLERİNİ GETİRİR)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // 1. Hizmeti Bul
            var service = await _context.Services.FirstOrDefaultAsync(m => m.Id == id);
            if (service == null) return NotFound();

            ViewBag.ServiceName = service.Name;
            ViewBag.ServiceId = service.Id;

            // 2. GRUP DERSLERİ: Bu hizmet ismini içeren, ONAYLI ve GELECEK seansları bul
            var relatedSessions = await _context.Sessions
                .Include(s => s.Trainer)
                .Where(s => s.IsApproved == true &&
                            s.SessionDate > DateTime.Now &&
                            s.ClassName.Contains(service.Name))
                .OrderBy(s => s.SessionDate)
                .ToListAsync();

            // 3. (YENİ) BİREYSEL EĞİTMENLER: Uzmanlık alanı bu hizmeti içeren hocalar
            // Örn: Hizmet "Pilates" ise, uzmanlığında "Pilates" yazan hocaları getir.
            var availableTrainers = await _context.Trainers
                .Where(t => t.Specialization.Contains(service.Name))
                .ToListAsync();

            // Eğitmen listesini ViewBag ile sayfaya taşıyoruz
            ViewBag.AvailableTrainers = availableTrainers;

            // View, öncelikli olarak Seans listesini (Model) olarak bekliyor
            return View(relatedSessions);
        }

        // ============================================================
        // 2. ÜYE İŞLEMLERİ (DERSE KATILMA)
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Uye")] // Sadece üyeler katılabilir
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinSession(int sessionId)
        {
            // Kullanıcıyı Bul
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdClaim.Value);

            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            // KONTROLLER

            // A) Kontenjan Dolu mu?
            if (session.CurrentCount >= session.Quota)
            {
                TempData["Error"] = "Üzgünüz, bu dersin kontenjanı dolmuş.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // B) Zaten katılmış mı?
            bool alreadyJoined = await _context.SessionRegistrations
                .AnyAsync(r => r.SessionId == sessionId && r.AppUserId == userId);

            if (alreadyJoined)
            {
                TempData["Warning"] = "Zaten bu derse kaydınız mevcut.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            // İŞLEM: KAYIT VE ARTTIRMA

            // 1. Kayıt Tablosuna Ekle
            var registration = new SessionRegistration
            {
                SessionId = sessionId,
                AppUserId = userId,
                RegistrationDate = DateTime.Now
            };
            _context.SessionRegistrations.Add(registration);

            // 2. Kontenjanı Arttır
            session.CurrentCount += 1;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Derse başarıyla kayıt oldunuz!";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // ============================================================
        // 3. ADMIN İŞLEMLERİ (EKLE / SİL / DÜZENLE)
        // ============================================================

        // GET: Services/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Address");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,DurationMinutes,Price,GymId")] Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Address", service.GymId);
            return View(service);
        }

        // GET: Services/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Address", service.GymId);
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DurationMinutes,Price,GymId")] Service service)
        {
            if (id != service.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Address", service.GymId);
            return View(service);
        }

        // GET: Services/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services
                .Include(s => s.Gym)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null) _context.Services.Remove(service);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}
