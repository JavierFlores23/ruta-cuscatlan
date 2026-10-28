using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.Controllers;

[Authorize(Roles = "Admin")]
public class ToursController : Controller
{
    private readonly AppDbContext _db;
    public ToursController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    public async Task<IActionResult> Catalogo(string? q)
    {
        var consulta = _db.Tours.Include(t => t.Destino).ThenInclude(d => d!.Categoria)
            .Include(t => t.Guia).Include(t => t.Reservas)
            .Where(t => t.Activo && t.FechaSalida > DateTime.Now);

        var termino = q?.Trim();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            var patron = $"%{termino}%";
            consulta = consulta.Where(t =>
                EF.Functions.Like(t.Titulo, patron) ||
                EF.Functions.Like(t.Descripcion ?? "", patron) ||
                EF.Functions.Like(t.Destino!.Nombre, patron));
        }

        ViewData["SearchTerm"] = termino;
        return View(await consulta.OrderBy(t => t.FechaSalida).ToListAsync());
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Tours.Include(t => t.Destino).Include(t => t.Guia).Include(t => t.Reservas)
            .OrderBy(t => t.FechaSalida).ToListAsync());

    public IActionResult Create() { Listas(); return View(new Tour()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Tour t)
    {
        if (!ModelState.IsValid) { Listas(t); return View(t); }
        _db.Add(t);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Tour creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.Tours.FindAsync(id);
        if (t == null) return NotFound();
        Listas(t);
        return View(t);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Tour t)
    {
        if (id != t.Id) return NotFound();
        if (!ModelState.IsValid) { Listas(t); return View(t); }
        _db.Update(t);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Tour actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Tours.FindAsync(id);
        if (t != null)
        {
            try { _db.Remove(t); await _db.SaveChangesAsync(); TempData["Ok"] = "Tour eliminado."; }
            catch (DbUpdateException) { TempData["Error"] = "No se puede eliminar: tiene reservas. Desactívalo en su lugar."; }
        }
        return RedirectToAction(nameof(Index));
    }

    private void Listas(Tour? t = null)
    {
        ViewBag.Destinos = new SelectList(_db.Destinos.OrderBy(x => x.Nombre), "Id", "Nombre", t?.DestinoId);
        ViewBag.Guias = new SelectList(_db.Guias.OrderBy(x => x.Nombre), "Id", "Nombre", t?.GuiaId);
    }
}
