using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // KAYIT OL SAYFASI (GET)
        public IActionResult Register()
        {
            return View();
        }

        // KAYIT OL İŞLEMİ (POST)
        [HttpPost]
        public async Task<IActionResult> Register(AppUser user)
        {
            if (ModelState.IsValid)
            {
                // E-posta kontrolü
                if (_context.AppUsers.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("", "Bu e-posta adresi zaten kayıtlı.");
                    return View(user);
                }

                // Kullanıcıyı kaydet
                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }
            return View(user);
        }

        // GİRİŞ YAP SAYFASI (GET)
        public IActionResult Login()
        {
            return View();
        }

        // GİRİŞ YAP İŞLEMİ (POST)
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // -----------------------------------------------------------
            // 1. ÖZEL DURUM: ADMİN GİRİŞİ (SABİT ŞİFRE)
            // -----------------------------------------------------------
            // Buradaki harf hatalarına dikkat et! Şifre: "sau" (küçük harf)
            if (email == "G231210048@sakarya.edu.tr" && password == "sau")
            {
                var adminClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"), // Ekranda görünecek isim
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, "Admin"), // Rolü Admin olarak atadık
                    new Claim("UserId", "0")
                };

                var adminIdentity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(adminIdentity));

                return RedirectToAction("Index", "Home");
            }

            // -----------------------------------------------------------
            // 2. NORMAL KULLANICI GİRİŞİ (VERİTABANINDAN KONTROL)
            // -----------------------------------------------------------
            var user = _context.AppUsers.FirstOrDefault(x => x.Email == email && x.Password == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            // Hatalı giriş durumunda mesaj göster
            ViewBag.Error = "E-posta veya şifre hatalı! (Admin girişi için bilgileri kontrol et)";
            return View();
        }

        // ÇIKIŞ YAP
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
