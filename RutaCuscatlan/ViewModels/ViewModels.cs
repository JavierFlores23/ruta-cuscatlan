using System.ComponentModel.DataAnnotations;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress] public string Correo { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class RegistroViewModel
{
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, EmailAddress] public string Correo { get; set; } = "";
    [StringLength(20)] public string? Telefono { get; set; }
    [Required, MinLength(6, ErrorMessage = "Mínimo 6 caracteres"), DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class ReservaViewModel
{
    public int TourId { get; set; }
    [Range(1, 50, ErrorMessage = "Indica entre 1 y 50 personas")] public int Personas { get; set; } = 1;
    public Tour? Tour { get; set; }
}
