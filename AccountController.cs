using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;
using ProjeOdeviWeb_G231210048.Models.ViewModels;

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

        // KAYIT OL İŞLEMİ (POST) - GÜNCELLENDİ 🛠️
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
        
            if (model.Role == "Uye")
            {
                if (model.Height == null || model.Height == 0)
                {
                    ModelState.AddModelError("Height", "Lütfen boyunuzu giriniz (Üyeler için zorunludur).");
                }
                if (model.Weight == null || model.Weight == 0)
                {
                    ModelState.AddModelError("Weight", "Lütfen kilonuzu giriniz (Üyeler için zorunludur).");
                }
            }

            if (ModelState.IsValid)
            {
                // 2. E-posta kontrolü
                if (_context.AppUsers.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Bu e-posta adresi zaten kayıtlı.");
                    return View(model);
                }

                // 3. AppUser Nesnesini Oluştur ve Verileri Eşle
                var user = new AppUser
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = model.Password, 
                    Role = model.Role,

                    // --- YENİ EKLENEN ALANLAR ---
                    Age = model.Age,        // Yaş
                    Gender = model.Gender,  // Cinsiyet
                    Height = model.Height,  // Boy (Nullable)
                    Weight = model.Weight   // Kilo (Nullable)
                };

                // 4. ROL VE ONAY KONTROLLERİ
                if (model.Role == "Uye")
                {
                    user.IsApproved = true; // Üyeler direkt onaylı
                }
                else if (model.Role == "Antrenör")
                {
                    user.IsApproved = false; // Antrenörler onay bekler
                }

                // 5. KULLANICIYI KAYDET (AppUsers Tablosu)
                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync(); // ID oluşması için kayıt şart

                // 6. EĞER ANTRENÖRSE, TRAINER TABLOSUNA DA EKLE
                if (model.Role == "Antrenör")
                {
                    var trainer = new Trainer
                    {
                        FullName = model.FullName,

                        // Uzmanlık alanlarını birleştir (Örn: "Fitness, Pilates")
                        Specialization = model.SelectedSpecializations != null
                                         ? string.Join(", ", model.SelectedSpecializations)
                                         : "Genel",

                        AvailableFrom = model.StartTime ?? new TimeSpan(9, 0, 0),
                        AvailableTo = model.EndTime ?? new TimeSpan(18, 0, 0),
                        GymId = 1, // Varsayılan Gym
                        DaysOff = model.SelectedDaysOff != null ? string.Join(",", model.SelectedDaysOff) : ""
                    };

                    _context.Trainers.Add(trainer);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Login");
            }
            return View(model);
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
            // 1. ADMİN GİRİŞİ (SABİT)
            if (email == "G231210048@sakarya.edu.tr" && password == "sau")
            {
                var adminClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("UserId", "0")
                };

                var adminIdentity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(adminIdentity));

                return RedirectToAction("Index", "Admin");
            }

            // 2. NORMAL KULLANICI GİRİŞİ
            var user = _context.AppUsers.FirstOrDefault(x => x.Email == email && x.Password == password);

            if (user != null)
            {
                // ONAY KONTROLÜ
                if (user.Role == "Antrenör" && !user.IsApproved)
                {
                    return RedirectToAction("WaitForApproval");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // ROL YÖNLENDİRMESİ
                if (user.Role == "Admin") return RedirectToAction("Index", "Admin");
                else if (user.Role == "Antrenör") return RedirectToAction("TrainerDashboard", "Home");
                else return RedirectToAction("MemberDashboard", "Home");
            }

            ViewBag.Error = "E-posta veya şifre hatalı!";
            return View();
        }

        // ONAY BEKLEME EKRANI
        public IActionResult WaitForApproval()
        {
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
