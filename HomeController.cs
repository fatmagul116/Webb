using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;
using System.Diagnostics;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    [Authorize] // Giriş yapmayan giremesin (Varsayılan kural)
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ==========================================================
        // 1. ANA SAYFA & YÖNLENDİRME
        // ==========================================================
        [AllowAnonymous] // Herkese açık
        public IActionResult Index()
        {
            // --- TRAFİK KONTROLÜ ---
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");
                else if (User.IsInRole("Antrenör"))
                    return RedirectToAction("TrainerDashboard");
                else if (User.IsInRole("Uye"))
                    return RedirectToAction("MemberDashboard");
            }

            // Misafirler için Reklam/Landing Page
            return View();
        }

        // ==========================================================
        // 2. HİZMETLER / DERS PROGRAMI (YENİ EKLENDİ ✅)
        // ==========================================================
        [AllowAnonymous] // Üye olmayanlar da dersleri inceleyebilsin
        public async Task<IActionResult> Services()
        {
            // Sadece Onaylı (IsApproved == true) ve Gelecek Tarihli dersleri getir
            var activeSessions = await _context.Sessions
                .Include(s => s.Trainer) // Hocanın adını görmek için
                .Where(s => s.IsApproved == true && s.SessionDate > DateTime.Now)
                .OrderBy(s => s.SessionDate) // Tarihe göre sırala
                .ToListAsync();

            return View(activeSessions);
        }

        // ==========================================================
        // 3. ÜYE PANELİ (Hibrit: Randevular + Ders Önerileri)
        // ==========================================================
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> MemberDashboard()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.AppUsers.FindAsync(userId);

            // A) BİREYSEL RANDEVULAR
            var myAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.AppUserId == userId && a.Date >= DateTime.Today)
                .OrderBy(a => a.Date).ThenBy(a => a.Time)
                .ToListAsync();

            // B) GRUP SEANSLARI (Öneri olarak gösterilenler)
            ViewBag.ActiveSessions = await _context.Sessions
                .Include(s => s.Trainer)
                .Where(s => s.IsApproved == true && s.SessionDate >= DateTime.Now)
                .OrderBy(s => s.SessionDate)
                .Take(3) // Sadece en yakın 3 dersi göster
                .ToListAsync();

            // C) AI ANALİZİ (VKI Hesaplama)
            double vki = 0;
            string aiRecommendation = "Henüz boy/kilo bilgisi girilmemiş.";

            if (user != null && user.Weight.HasValue && user.Height.HasValue && user.Height > 0)
            {
                double boyMetre = user.Height.Value / 100.0;
                vki = user.Weight.Value / (boyMetre * boyMetre);

                if (vki < 18.5) aiRecommendation = "AI Analizi: Kilonuz idealin altında. Kas kütlesi artışı için 'Fitness' programı öneriyorum.";
                else if (vki < 25) aiRecommendation = "AI Analizi: Formunuz harika! Korumak için 'Pilates' veya 'Yoga' öneriyorum.";
                else if (vki < 30) aiRecommendation = "AI Analizi: İdeal kilonun üzerindesiniz. 'Spinning' veya 'Cardio' öneriyorum.";
                else aiRecommendation = "AI Analizi: Sağlığınız için 'Özel Fitness' programına başlamanızı öneriyorum.";
            }

            ViewBag.VKI = Math.Round(vki, 2);
            ViewBag.AiMessage = aiRecommendation;
            ViewBag.User = user;

            return View(myAppointments);
        }

        // ==========================================================
        // 4. ANTRENÖR PANELİ (Hibrit: Bireysel + Grup)
        // ==========================================================
        [Authorize(Roles = "Antrenör")]
        public async Task<IActionResult> TrainerDashboard()
        {
            var trainerName = User.Identity.Name;

            // Önce Trainer tablosundaki ID'yi bulmamız lazım
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.FullName == trainerName);

            if (trainer == null)
            {
                ViewBag.MySessions = new List<Session>();
                return View(new List<Appointment>());
            }

            // A) BUGÜNKÜ BİREYSEL RANDEVULAR
            var todaysAppointments = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Service)
                .Where(a => a.TrainerId == trainer.Id && a.Date == DateTime.Today && a.Status == "Onaylandı")
                .OrderBy(a => a.Time)
                .ToListAsync();

            // B) ANTRENÖRÜN OLUŞTURDUĞU GRUP SEANSLARI (Gelecek Tarihli)
            var mySessions = await _context.Sessions
                .Where(s => s.TrainerId == trainer.Id && s.SessionDate >= DateTime.Today)
                .OrderBy(s => s.SessionDate)
                .ToListAsync();

            ViewBag.MySessions = mySessions;

            return View(todaysAppointments);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
