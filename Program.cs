using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı Bağlantısı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- KİMLİK DOĞRULAMA (GİRİŞ) SİSTEMİ EKLENDİ ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriş yapmamış kullanıcıyı buraya at
        options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisi yetmeyeni buraya at
    });
// ------------------------------------------------

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- SIRALAMA ÇOK ÖNEMLİ: ÖNCE Authentication, SONRA Authorization ---
app.UseAuthentication(); // Kimsin? (Giriş)
app.UseAuthorization();  // Yetkin var mı? (İzin)
// ---------------------------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
