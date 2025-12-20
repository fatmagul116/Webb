using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjeOdeviWeb_G231210048.Data;
using ProjeOdeviWeb_G231210048.Models;

namespace ProjeOdeviWeb_G231210048.Controllers
{
    public class TrainersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. VİTRİN KISMI (HERKES GÖREBİLİR)
        // ============================================================

        // GET: Trainers
        public async Task<IActionResult> Index()
        {
            // Antrenörleri ve bağlı oldukları spor salonunu getir
            var trainers = _context.Trainers.Include(t => t.Gym);
            return View(await trainers.ToListAsync());
        }

        // GET: Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // BURASI GÜNCELLENDİ: Antrenörün Grup Derslerini (Sessions) de getiriyoruz.
            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.Sessions) // <-- Grup Dersleri Dahil Edildi
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            // Sadece Gelecek Tarihli ve Onaylı Dersleri View'a göndermek için filtreleme yapabiliriz
            // Ancak genellikle View tarafında filtrelemek daha esnek olur.
            // Biz burada tümünü gönderiyoruz.

            return View(trainer);
        }

        // ============================================================
        // 2. YÖNETİM KISMI (SADECE ADMIN)
        // ============================================================

        // GET: Trainers/Create
        // Not: Genelde antrenörler "Kayıt Ol" sayfasından eklenir. 
        // Burası manuel ekleme içindir.
        [Authorize(Roles = "Admin")]
    
        // GET: Trainers/Edit/5
        // Admin düzenleyebilir. (İstenirse antrenörün kendisi de düzenleyebilsin diye yetki genişletilebilir)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null) return NotFound();

            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Address", trainer.GymId);
            return View(trainer);
        }

        // POST: Trainers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Specialization,AvailableFrom,AvailableTo,GymId,DaysOff")] Trainer trainer)
        {
            if (id != trainer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trainer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(trainer.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Address", trainer.GymId);
            return View(trainer);
        }

        // GET: Trainers/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // POST: Trainers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.Id == id);
        }
    }
}
