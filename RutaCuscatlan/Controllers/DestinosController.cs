using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.Controllers;

[Authorize(Roles = "Admin")]
public class DestinosController : Controller
{
    private readonly AppDbContext _db;
    public DestinosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Destinos.Include(d => d.Departamento).Include(d => d.Categoria).OrderBy(d => d.Nombre).ToListAsync());

    public IActionResult Create() { Listas(); return View(new Destino()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Destino d)
    {
        if (!ModelState.IsValid) { Listas(d); return View(d); }
        _db.Add(d);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Destino creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var d = await _db.Destinos.FindAsync(id);
        if (d == null) return NotFound();
        Listas(d);
        return View(d);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Destino d)
    {
        if (id != d.Id) return NotFound();
        if (!ModelState.IsValid) { Listas(d); return View(d); }
        _db.Update(d);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Destino actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Destinos.FindAsync(id);
        if (d != null)
        {
            try { _db.Remove(d); await _db.SaveChangesAsync(); TempData["Ok"] = "Destino eliminado."; }
            catch (DbUpdateException) { TempData["Error"] = "No se puede eliminar: tiene tours asociados."; }
        }
        return RedirectToAction(nameof(Index));
    }

    private void Listas(Destino? d = null)
    {
        ViewBag.Departamentos = new SelectList(_db.Departamentos.OrderBy(x => x.Nombre), "Id", "Nombre", d?.DepartamentoId);
        ViewBag.Categorias = new SelectList(_db.Categorias.OrderBy(x => x.Nombre), "Id", "Nombre", d?.CategoriaId);
    }
}
