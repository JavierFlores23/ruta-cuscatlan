using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;
using RutaCuscatlan.Services;
using RutaCuscatlan.ViewModels;

namespace RutaCuscatlan.Controllers;

[Authorize]
public class PagosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasarelaPago _pasarela;
    private readonly WompiClient _wompi;
    private readonly WompiOptions _wompiOpt;
    private readonly PagosOptions _opciones;

    public PagosController(AppDbContext db, IPasarelaPago pasarela, WompiClient wompi,
        IOptions<WompiOptions> wompiOpt, IOptions<PagosOptions> opciones)
    {
        _db = db;
        _pasarela = pasarela;
        _wompi = wompi;
        _wompiOpt = wompiOpt.Value;
        _opciones = opciones.Value;
    }

    private int? ClienteId => int.TryParse(User.FindFirst("ClienteId")?.Value, out var id) ? id : null;

    private async Task<Reserva?> CargarAsync(int id)
    {
        var cid = ClienteId;
        if (cid is null) return null;
        return await _db.Reservas
            .Include(r => r.Tour).ThenInclude(t => t.Destino)
            .Include(r => r.Pagos)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClienteId == cid);
    }

    // Devuelve una redirección si la reserva ya no está en estado de pago.
    private async Task<IActionResult?> RevisarEstadoAsync(Reserva r)
    {
        if (r.Estado == "Confirmada") return RedirectToAction(nameof(Recibo), new { id = r.Id });
        if (r.Estado == "Cancelada")
        {
            TempData["Error"] = "Esa reserva fue cancelada.";
            return RedirectToAction("Mis", "Reservas");
        }
        if (r.PendienteVencida)
        {
            r.Estado = "Cancelada";
            await _db.SaveChangesAsync();
            TempData["Error"] = "El cupo apartado venció. Haz la reserva de nuevo.";
            return RedirectToAction("Catalogo", "Tours");
        }
        return null;
    }

    private static void Preparar(Reserva r, PagoViewModel vm)
    {
        vm.Reserva = r;
        vm.Segundos = Math.Max(0, (int)(r.FechaReserva.AddMinutes(Reserva.MinutosCupo) - DateTime.Now).TotalSeconds);
    }

    private IActionResult Vista(Reserva r, PagoViewModel vm)
    {
        Preparar(r, vm);
        return View(vm);
    }

    private IActionResult VistaWompi(Reserva r)
    {
        var vm = new PagoViewModel();
        Preparar(r, vm);
        ViewBag.WompiConfigurado = _wompi.Configurado;
        return View("CheckoutWompi", vm);
    }

    private static string NombreFormaPago(int forma) => forma switch
    {
        0 => "Tarjeta",
        1 => "Puntos",
        2 => "Cuotas",
        3 => "Bitcoin",
        4 => "QuickPay",
        _ => "Wompi"
    };

    private static bool EsErrorDeRed(Exception ex) =>
        ex is WompiException || ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException;

    /// <summary>
    /// Si hay un pago de Wompi pendiente, le pregunta a Wompi si ya fue aprobado y, de ser así, confirma la reserva.
    /// La confirmación sale de la API de Wompi (servidor a servidor), nunca de los parámetros de la URL de retorno.
    /// Devuelve un aviso para mostrar al cliente, o null.
    /// </summary>
    private async Task<string?> VerificarWompiPendienteAsync(Reserva r)
    {
        if (r.Estado != "Pendiente") return null;
        var pendiente = r.Pagos
            .Where(p => p.Proveedor.StartsWith("Wompi") && p.Estado == "Pendiente")
            .OrderByDescending(p => p.Fecha)
            .FirstOrDefault();
        if (pendiente == null) return null;

        ConsultaWompi consulta;
        try
        {
            consulta = await _wompi.ConsultarEnlaceAsync(pendiente.Referencia);
        }
        catch (Exception ex) when (EsErrorDeRed(ex))
        {
            return "No pudimos consultar a Wompi en este momento. Intenta de nuevo en unos segundos.";
        }

        var t = consulta.Aprobada;
        if (t == null)
            return string.IsNullOrWhiteSpace(consulta.UltimoMensaje) ? null : "Wompi reportó: " + consulta.UltimoMensaje;

        if (Math.Abs(t.Monto - r.Total) > 0.01m)
        {
            pendiente.Estado = "Rechazado";
            pendiente.Mensaje = "El monto pagado no coincide con el de la reserva.";
            await _db.SaveChangesAsync();
            return "El monto pagado no coincide con el de la reserva. Escríbenos para revisarlo.";
        }

        var idEnlace = pendiente.Referencia;
        pendiente.Estado = "Aprobado";
        pendiente.Proveedor = t.EsReal ? "Wompi" : "Wompi (pruebas)";
        pendiente.Referencia = t.IdTransaccion.Length > 40 ? t.IdTransaccion.Substring(0, 40) : t.IdTransaccion;
        pendiente.Marca = NombreFormaPago(t.FormaPago);
        pendiente.Mensaje = $"Enlace {idEnlace}. Autorización {t.CodigoAutorizacion}".Trim();
        pendiente.Fecha = DateTime.Now;
        r.Estado = "Confirmada";
        await _db.SaveChangesAsync();
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int id)
    {
        var r = await CargarAsync(id);
        if (r == null) return NotFound();

        string? aviso = null;
        if (_opciones.UsaWompi) aviso = await VerificarWompiPendienteAsync(r);

        var redireccion = await RevisarEstadoAsync(r);
        if (redireccion != null) return redireccion;

        if (aviso != null) ViewBag.Rechazo = aviso;
        return _opciones.UsaWompi ? VistaWompi(r) : Vista(r, new PagoViewModel());
    }

    /// <summary>Crea el enlace de pago en Wompi y manda al cliente a pagar a la pantalla de Wompi.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarWompi(int id)
    {
        if (!_opciones.UsaWompi) return RedirectToAction(nameof(Checkout), new { id });
        var r = await CargarAsync(id);
        if (r == null) return NotFound();

        await VerificarWompiPendienteAsync(r); // por si ya había pagado
        var redireccion = await RevisarEstadoAsync(r);
        if (redireccion != null) return redireccion;

        if (!_wompi.Configurado)
        {
            TempData["Error"] = "Wompi no está configurado (faltan ClientId, ClientSecret o EmailNotificacion).";
            return RedirectToAction(nameof(Checkout), new { id });
        }

        var baseUrl = string.IsNullOrWhiteSpace(_wompiOpt.UrlPublica)
            ? $"{Request.Scheme}://{Request.Host}"
            : _wompiOpt.UrlPublica.TrimEnd('/');
        var personas = r.CantidadPersonas == 1 ? "persona" : "personas";

        try
        {
            var enlace = await _wompi.CrearEnlaceAsync(
                identificador: $"RC-{r.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                monto: r.Total,
                nombreProducto: $"{r.Tour?.Titulo} ({r.CantidadPersonas} {personas})",
                descripcion: $"Reserva #{r.Id} - {r.Tour?.Destino?.Nombre} - salida {r.Tour?.FechaSalida:dd/MM/yyyy HH:mm}",
                urlRedirect: $"{baseUrl}/Pagos/Retorno/{r.Id}",
                urlRetorno: $"{baseUrl}/Pagos/Checkout/{r.Id}",
                duracionMinutos: Reserva.MinutosCupo);

            if (!Uri.TryCreate(enlace.UrlEnlace, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !uri.Host.EndsWith("wompi.sv", StringComparison.OrdinalIgnoreCase))
                throw new WompiException("Wompi devolvió una dirección de pago inesperada.");

            foreach (var anterior in r.Pagos.Where(p => p.Proveedor.StartsWith("Wompi") && p.Estado == "Pendiente"))
                anterior.Estado = "Reemplazado";

            _db.Pagos.Add(new Pago
            {
                ReservaId = r.Id,
                Monto = r.Total,
                Estado = "Pendiente",
                Proveedor = "Wompi",
                Referencia = enlace.IdEnlace,
                Mensaje = "Esperando el pago en Wompi."
            });
            await _db.SaveChangesAsync();
            return Redirect(enlace.UrlEnlace);
        }
        catch (Exception ex) when (EsErrorDeRed(ex))
        {
            TempData["Error"] = ex is WompiException
                ? ex.Message
                : "No pudimos comunicarnos con Wompi. Intenta de nuevo.";
            return RedirectToAction(nameof(Checkout), new { id });
        }
    }

    /// <summary>A donde Wompi devuelve al cliente después de pagar. Se confirma consultando la API, no la URL.</summary>
    [HttpGet]
    public async Task<IActionResult> Retorno(int id)
    {
        var r = await CargarAsync(id);
        if (r == null) return NotFound();

        string? aviso = null;
        if (_opciones.UsaWompi) aviso = await VerificarWompiPendienteAsync(r);

        if (r.Estado == "Confirmada")
        {
            TempData["Ok"] = "¡Pago aprobado! Tu reserva está confirmada.";
            return RedirectToAction(nameof(Recibo), new { id });
        }

        TempData["Error"] = aviso ?? "Todavía no vemos un pago aprobado en Wompi. Si ya pagaste, espera unos segundos y vuelve a intentar.";
        return RedirectToAction(nameof(Checkout), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int id, PagoViewModel vm)
    {
        if (_opciones.UsaWompi) return RedirectToAction(nameof(Checkout), new { id });
        var r = await CargarAsync(id);
        if (r == null) return NotFound();
        var redireccion = await RevisarEstadoAsync(r);
        if (redireccion != null) return redireccion;

        var digitos = Tarjeta.SoloDigitos(vm.Numero);
        if (!string.IsNullOrWhiteSpace(vm.Numero) && !Tarjeta.LuhnValido(digitos))
            ModelState.AddModelError(nameof(vm.Numero), "El número de tarjeta no es válido.");

        var mes = 0; var anio = 0;
        if (!string.IsNullOrWhiteSpace(vm.Vencimiento))
        {
            if (!Tarjeta.TryParseVencimiento(vm.Vencimiento, out mes, out anio))
                ModelState.AddModelError(nameof(vm.Vencimiento), "Usa el formato MM/AA.");
            else if (Tarjeta.Vencida(mes, anio, DateTime.Now))
                ModelState.AddModelError(nameof(vm.Vencimiento), "La tarjeta está vencida.");
        }

        if (!ModelState.IsValid)
        {
            vm.Cvc = "";
            return Vista(r, vm);
        }

        var resultado = await _pasarela.CobrarAsync(new SolicitudCobro(
            r.Total, digitos, vm.Titular, mes, anio, vm.Cvc, $"Reserva #{r.Id} - {r.Tour?.Titulo}"));

        // Solo se guarda marca y últimos 4 dígitos. Nunca el número completo ni el CVC.
        _db.Pagos.Add(new Pago
        {
            ReservaId = r.Id,
            Monto = r.Total,
            Estado = resultado.Aprobado ? "Aprobado" : "Rechazado",
            Proveedor = _pasarela.Nombre,
            Referencia = resultado.Referencia,
            Marca = resultado.Marca,
            Ultimos4 = resultado.Ultimos4,
            Mensaje = resultado.Mensaje
        });
        if (resultado.Aprobado) r.Estado = "Confirmada";
        await _db.SaveChangesAsync();

        if (resultado.Aprobado)
        {
            TempData["Ok"] = "¡Pago aprobado! Tu reserva está confirmada.";
            return RedirectToAction(nameof(Recibo), new { id = r.Id });
        }

        ViewBag.Rechazo = "Pago rechazado: " + resultado.Mensaje + " Prueba con otra tarjeta.";
        vm.Numero = "";
        vm.Cvc = "";
        return Vista(r, vm);
    }

    [HttpGet]
    public async Task<IActionResult> Recibo(int id)
    {
        var r = await CargarAsync(id);
        if (r == null) return NotFound();
        var pago = r.Pagos.Where(p => p.Estado == "Aprobado").OrderByDescending(p => p.Fecha).FirstOrDefault();
        if (pago == null)
        {
            TempData["Error"] = "Esa reserva no tiene un pago aprobado.";
            return RedirectToAction("Mis", "Reservas");
        }
        return View(new ReciboViewModel { Reserva = r, Pago = pago });
    }

    [HttpGet]
    public async Task<IActionResult> Factura(int id)
    {
        var r = await CargarAsync(id);
        if (r == null) return NotFound();
        var pago = r.Pagos.Where(p => p.Estado == "Aprobado").OrderByDescending(p => p.Fecha).FirstOrDefault();
        if (pago == null)
        {
            TempData["Error"] = "Esa reserva no tiene un pago aprobado, así que no hay factura.";
            return RedirectToAction("Mis", "Reservas");
        }
        r.Cliente = await _db.Clientes.FindAsync(r.ClienteId);
        return View(new ReciboViewModel { Reserva = r, Pago = pago });
    }
}
