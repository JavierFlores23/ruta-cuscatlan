using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.ViewModels;

namespace RutaCuscatlan.Controllers;

[Authorize(Roles = "Admin")]
public sealed class DatosController : Controller
{
    private readonly AppDbContext _db;

    public DatosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DatosAdminViewModel
        {
            Departamentos = await _db.Departamentos.AsNoTracking().Include(d => d.Destinos)
                .OrderBy(d => d.Nombre).ToListAsync(),
            Categorias = await _db.Categorias.AsNoTracking().Include(c => c.Destinos)
                .OrderBy(c => c.Nombre).ToListAsync(),
            Destinos = await _db.Destinos.AsNoTracking().Include(d => d.Departamento).Include(d => d.Categoria)
                .OrderBy(d => d.Nombre).ToListAsync(),
            Guias = await _db.Guias.AsNoTracking().OrderBy(g => g.Nombre).ToListAsync(),
            Tours = await _db.Tours.AsNoTracking().Include(t => t.Destino).Include(t => t.Guia)
                .Include(t => t.Reservas).OrderBy(t => t.Titulo).ToListAsync(),
            Clientes = await _db.Clientes.AsNoTracking().Include(c => c.Reservas).OrderBy(c => c.Nombre).ToListAsync(),
            Reservas = await _db.Reservas.AsNoTracking().Include(r => r.Cliente).Include(r => r.Tour)
                .OrderByDescending(r => r.FechaReserva).ToListAsync(),
            Usuarios = await _db.Usuarios.AsNoTracking().OrderBy(u => u.Correo)
                .Select(u => new UsuarioResumenViewModel { Correo = u.Correo, Rol = u.Rol, ClienteId = u.ClienteId })
                .ToListAsync()
        };

        return View(model);
    }
}
