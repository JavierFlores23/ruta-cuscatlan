using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.Services;

public interface IReservaService
{
    Task<(bool Ok, string Error, int ReservaId)> CrearAsync(int tourId, int clienteId, int personas);
    Task<bool> CancelarAsync(int reservaId, int? clienteId, bool esAdmin);
}

public class ReservaService : IReservaService
{
    private readonly AppDbContext _db;
    public ReservaService(AppDbContext db) => _db = db;

    public async Task<(bool Ok, string Error, int ReservaId)> CrearAsync(int tourId, int clienteId, int personas)
    {
        var tour = await _db.Tours.Include(t => t.Reservas).FirstOrDefaultAsync(t => t.Id == tourId);
        if (tour == null) return (false, "El tour no existe.", 0);
        if (!tour.Activo) return (false, "El tour no está disponible.", 0);
        if (tour.FechaSalida <= DateTime.Now) return (false, "Ese tour ya salió.", 0);
        if (personas <= 0) return (false, "Indica al menos una persona.", 0);
        if (personas > tour.Disponibles) return (false, $"Solo quedan {tour.Disponibles} cupos disponibles.", 0);

        // Queda "Pendiente" (con el cupo apartado) hasta que el pago sea aprobado.
        var reserva = new Reserva
        {
            TourId = tourId,
            ClienteId = clienteId,
            CantidadPersonas = personas,
            Total = tour.Precio * personas,
            Estado = "Pendiente"
        };
        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync();
        return (true, "", reserva.Id);
    }

    public async Task<bool> CancelarAsync(int reservaId, int? clienteId, bool esAdmin)
    {
        var r = await _db.Reservas.Include(x => x.Pagos).FirstOrDefaultAsync(x => x.Id == reservaId);
        if (r == null || (!esAdmin && r.ClienteId != clienteId)) return false;
        r.Estado = "Cancelada";
        foreach (var pago in r.Pagos.Where(p => p.Estado == "Aprobado"))
        {
            if (pago.Proveedor.StartsWith("Wompi"))
            {
                // Wompi no ofrece reembolsos por API: queda pendiente para devolverlo desde el panel de Wompi.
                pago.Estado = "Reembolso pendiente";
                pago.Mensaje = "Devolver manualmente desde panel.wompi.sv.";
            }
            else
            {
                pago.Estado = "Reembolsado";
                pago.Mensaje = "Reembolso simulado por cancelación.";
            }
        }
        await _db.SaveChangesAsync();
        return true;
    }
}
