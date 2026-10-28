using RutaCuscatlan.Models;

namespace RutaCuscatlan.ViewModels;

public sealed class DatosAdminViewModel
{
    public List<Departamento> Departamentos { get; init; } = [];
    public List<Categoria> Categorias { get; init; } = [];
    public List<Destino> Destinos { get; init; } = [];
    public List<Guia> Guias { get; init; } = [];
    public List<Tour> Tours { get; init; } = [];
    public List<Cliente> Clientes { get; init; } = [];
    public List<Reserva> Reservas { get; init; } = [];
    public List<UsuarioResumenViewModel> Usuarios { get; init; } = [];

    public int TotalRegistros => Departamentos.Count + Categorias.Count + Destinos.Count + Guias.Count
        + Tours.Count + Clientes.Count + Reservas.Count + Usuarios.Count;
}

public sealed class UsuarioResumenViewModel
{
    public string Correo { get; init; } = "";
    public string Rol { get; init; } = "";
    public int? ClienteId { get; init; }
}
