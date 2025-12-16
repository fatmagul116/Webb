using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;
using System.Diagnostics;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    [Authorize] // Giriş yapmayan giremesin
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [AllowAnonymous] // Anasayfa herkese açık
        public IActionResult Index()
        {
            // --- TRAFİK KONTROLÜ ---

            // 1. Eğer kullanıcı zaten GİRİŞ YAPMIŞSA, onu bekleme yapmadan paneline atalım.
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin"); // Patron ofise
                }
                else if (User.IsInRole("Antrenör"))
                {
                    return RedirectToAction("TrainerDashboard"); // Hoca sahaya
                }
                else if (User.IsInRole("Uye"))
                {
                    return RedirectToAction("MemberDashboard"); // Üye programa
                }
            }

            // 2. Eğer kullanıcı MİSAFİR ise (Giriş yapmamışsa), Reklam Sayfasını göster.
            return View();
        }

        // --- 1. ÜYE PANELİ ---
        [Authorize(Roles = "Uye")]
        public async Task<IActionResult> MemberDashboard()
        {
            // Giriş yapan kullanıcının ID'sini al
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.AppUsers.FindAsync(userId);

            // Randevuları Getir
            var myAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.AppUserId == userId && a.Date >= DateTime.Today)
                .OrderBy(a => a.Date).ThenBy(a => a.Time)
                .ToListAsync();

            // Antrenörleri ve Hizmetleri Getir
            ViewBag.Trainers = await _context.Trainers.ToListAsync();
            ViewBag.Services = await _context.Services.ToListAsync();

            // AI Hesaplaması
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

        // --- 2. ANTRENÖR PANELİ ---
        [Authorize(Roles = "Antrenör")]
        public async Task<IActionResult> TrainerDashboard()
        {
            var trainerName = User.Identity.Name;

            var mySchedule = await _context.Appointments
                .Include(a => a.AppUser)
                .Include(a => a.Service)
                .Include(a => a.Trainer)
                // Antrenörün kendi adıyla eşleşen ve bugünün randevularını getir
                .Where(a => a.Trainer.FullName == trainerName && a.Date == DateTime.Today)
                .OrderBy(a => a.Time)
                .ToListAsync();

            return View(mySchedule);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
