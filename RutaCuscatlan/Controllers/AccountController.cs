using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;
using RutaCuscatlan.ViewModels;

namespace RutaCuscatlan.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Usuario> _hasher = new();
    public AccountController(AppDbContext db) => _db = db;

    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Correo == vm.Correo);
        if (u == null || _hasher.VerifyHashedPassword(u, u.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(vm);
        }
        await IniciarSesion(u);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Registro() => View(new RegistroViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _db.Usuarios.AnyAsync(x => x.Correo == vm.Correo) || await _db.Clientes.AnyAsync(x => x.Correo == vm.Correo))
        {
            ModelState.AddModelError("Correo", "Ese correo ya está registrado.");
            return View(vm);
        }
        var cliente = new Cliente { Nombre = vm.Nombre, Correo = vm.Correo, Telefono = vm.Telefono };
        var u = new Usuario { Correo = vm.Correo, Rol = "Cliente", Cliente = cliente };
        u.PasswordHash = _hasher.HashPassword(u, vm.Password);
        _db.Usuarios.Add(u);
        await _db.SaveChangesAsync();
        await IniciarSesion(u);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task IniciarSesion(Usuario u)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Correo), new(ClaimTypes.Role, u.Rol) };
        if (u.ClienteId != null) claims.Add(new Claim("ClienteId", u.ClienteId.Value.ToString()));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
