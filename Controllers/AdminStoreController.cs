﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTKETicaretSitesi.Models;
using System.Linq;
using System.Threading.Tasks;
using BTKETicaretSitesi.Data;

[Authorize(Roles = "Admin")]
[Route("Admin/Stores")]
public class AdminStoreController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminStoreController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Tüm mağazaları listele
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var stores = await _context.Stores
            .Include(s => s.Owner)
            .Include(s => s.Products)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(stores);
    }

    // Onay bekleyen mağazalar
    [HttpGet("PendingApprovals")]
    public async Task<IActionResult> PendingApprovals()
    {
        var pendingStores = await _context.Stores
            .Where(s => !s.IsApproved)
            .Include(s => s.Owner)
            .ToListAsync();

        return View(pendingStores);
    }

    // Mağaza detayları
    

    // Mağaza onayla
    [HttpPost("Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null)
        {
            return NotFound();
        }

        store.IsApproved = true;
        store.UpdatedAt = DateTime.Now;
        _context.Update(store);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Mağaza başarıyla onaylandı";
        return RedirectToAction(nameof(PendingApprovals));
    }

    // Mağaza onayını kaldır
    [HttpPost("RevokeApproval")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeApproval(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null)
        {
            return NotFound();
        }

        store.IsApproved = false;
        store.UpdatedAt = DateTime.Now;
        _context.Update(store);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Mağaza onayı başarıyla kaldırıldı";
        return RedirectToAction(nameof(Index));
    }

    // Mağaza silme
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null)
        {
            return NotFound();
        }

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Mağaza başarıyla silindi";
        return RedirectToAction(nameof(Index));
    }
}