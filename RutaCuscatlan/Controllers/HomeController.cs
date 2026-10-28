using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;

namespace RutaCuscatlan.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Destinos.Include(d => d.Departamento).Include(d => d.Categoria)
            .Where(d => d.Activo).OrderBy(d => d.Nombre).ToListAsync());
}
