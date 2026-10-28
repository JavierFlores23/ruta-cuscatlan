using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Services;
using RutaCuscatlan.ViewModels;

namespace RutaCuscatlan.Controllers;

[Authorize]
public class ReservasController : Controller
{
    private readonly AppDbContext _db;
    private readonly IReservaService _svc;
    public ReservasController(AppDbContext db, IReservaService svc) { _db = db; _svc = svc; }

    private int? ClienteId => int.TryParse(User.FindFirst("ClienteId")?.Value, out var id) ? id : null;

    public async Task<IActionResult> Create(int tourId)
    {
        var t = await _db.Tours.Include(x => x.Destino).Include(x => x.Reservas).FirstOrDefaultAsync(x => x.Id == tourId);
        if (t == null) return NotFound();
        return View(new ReservaViewModel { TourId = t.Id, Tour = t });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservaViewModel vm)
    {
        if (ClienteId is not int cid)
        {
            TempData["Error"] = "Solo las cuentas de cliente pueden reservar.";
            return RedirectToAction("Catalogo", "Tours");
        }
        var (ok, error) = await _svc.CrearAsync(vm.TourId, cid, vm.Personas);
        if (!ok)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Create), new { tourId = vm.TourId });
        }
        TempData["Ok"] = "¡Reserva confirmada!";
        return RedirectToAction(nameof(Mis));
    }

    public async Task<IActionResult> Mis()
    {
        var cid = ClienteId;
        return View(await _db.Reservas.Include(r => r.Tour).Include(r => r.Cliente)
            .Where(r => r.ClienteId == cid).OrderByDescending(r => r.FechaReserva).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index() =>
        View(await _db.Reservas.Include(r => r.Tour).Include(r => r.Cliente)
            .OrderByDescending(r => r.FechaReserva).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var ok = await _svc.CancelarAsync(id, ClienteId, User.IsInRole("Admin"));
        TempData[ok ? "Ok" : "Error"] = ok ? "Reserva cancelada." : "No se pudo cancelar la reserva.";
        return RedirectToAction(User.IsInRole("Admin") ? nameof(Index) : nameof(Mis));
    }
}
