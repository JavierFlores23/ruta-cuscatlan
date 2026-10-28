using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.Services;

public interface IReservaService
{
    Task<(bool Ok, string Error)> CrearAsync(int tourId, int clienteId, int personas);
    Task<bool> CancelarAsync(int reservaId, int? clienteId, bool esAdmin);
}

public class ReservaService : IReservaService
{
    private readonly AppDbContext _db;
    public ReservaService(AppDbContext db) => _db = db;

    public async Task<(bool Ok, string Error)> CrearAsync(int tourId, int clienteId, int personas)
    {
        var tour = await _db.Tours.Include(t => t.Reservas).FirstOrDefaultAsync(t => t.Id == tourId);
        if (tour == null) return (false, "El tour no existe.");
        if (!tour.Activo) return (false, "El tour no está disponible.");
        if (tour.FechaSalida <= DateTime.Now) return (false, "Ese tour ya salió.");
        if (personas <= 0) return (false, "Indica al menos una persona.");
        if (personas > tour.Disponibles) return (false, $"Solo quedan {tour.Disponibles} cupos disponibles.");

        _db.Reservas.Add(new Reserva
        {
            TourId = tourId,
            ClienteId = clienteId,
            CantidadPersonas = personas,
            Total = tour.Precio * personas,
            Estado = "Confirmada"
        });
        await _db.SaveChangesAsync();
        return (true, "");
    }

    public async Task<bool> CancelarAsync(int reservaId, int? clienteId, bool esAdmin)
    {
        var r = await _db.Reservas.FindAsync(reservaId);
        if (r == null || (!esAdmin && r.ClienteId != clienteId)) return false;
        r.Estado = "Cancelada";
        await _db.SaveChangesAsync();
        return true;
    }
}
