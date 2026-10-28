using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RutaCuscatlan.Models;

public class Departamento
{
    public int Id { get; set; }
    [Required, StringLength(60)] public string Nombre { get; set; } = "";
    public List<Destino> Destinos { get; set; } = new();
}

public class Categoria
{
    public int Id { get; set; }
    [Required, StringLength(60)] public string Nombre { get; set; } = "";
    public List<Destino> Destinos { get; set; } = new();
}

public class Destino
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, StringLength(500)] public string Descripcion { get; set; } = "";
    [StringLength(200)] public string? ImagenUrl { get; set; }
    [StringLength(150)] public string? ImagenCredito { get; set; }
    [Url(ErrorMessage = "La fuente debe ser un enlace válido."), StringLength(300)] public string? ImagenFuente { get; set; }
    public bool Activo { get; set; } = true;
    public int DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public List<Tour> Tours { get; set; } = new();
}

public class Guia
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [StringLength(20)] public string? Telefono { get; set; }
    [StringLength(100)] public string? Correo { get; set; }
    [StringLength(100)] public string? Idiomas { get; set; }
    public List<Tour> Tours { get; set; } = new();
}

public class Tour
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Titulo { get; set; } = "";
    [StringLength(500)] public string? Descripcion { get; set; }
    [Column(TypeName = "decimal(10,2)"), Range(0, 100000)] public decimal Precio { get; set; }
    [Range(1, 240, ErrorMessage = "La duración debe ser entre 1 y 240 horas")] public int DuracionHoras { get; set; } = 6;
    [Range(1, 500, ErrorMessage = "El cupo debe ser entre 1 y 500")] public int CupoMaximo { get; set; } = 10;
    public DateTime FechaSalida { get; set; } = DateTime.Today.AddDays(7).AddHours(8);
    public bool Activo { get; set; } = true;
    public int DestinoId { get; set; }
    public Destino? Destino { get; set; }
    public int? GuiaId { get; set; }
    public Guia? Guia { get; set; }
    public List<Reserva> Reservas { get; set; } = new();

    [NotMapped]
    public int Disponibles => CupoMaximo - Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.CantidadPersonas);
}

public class Cliente
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Correo { get; set; } = "";
    [StringLength(20)] public string? Telefono { get; set; }
    public List<Reserva> Reservas { get; set; } = new();
}

public class Reserva
{
    public int Id { get; set; }
    [Range(1, 50)] public int CantidadPersonas { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Total { get; set; }
    [StringLength(20)] public string Estado { get; set; } = "Pendiente"; // Pendiente, Confirmada, Cancelada
    public DateTime FechaReserva { get; set; } = DateTime.Now;
    public int TourId { get; set; }
    public Tour? Tour { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
}

public class Usuario
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Correo { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [StringLength(20)] public string Rol { get; set; } = "Cliente"; // Admin o Cliente
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
}
