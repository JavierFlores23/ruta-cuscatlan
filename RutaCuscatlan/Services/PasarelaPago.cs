namespace RutaCuscatlan.Services;

public record SolicitudCobro(decimal Monto, string Numero, string Titular, int MesExp, int AnioExp, string Cvc, string Descripcion);

public record ResultadoCobro(bool Aprobado, string Referencia, string Marca, string Ultimos4, string Mensaje);

/// <summary>
/// Contrato de la pasarela de pago. Hoy se usa <see cref="PasarelaSimulada"/>;
/// para cobrar de verdad se crea otra clase que implemente esta interfaz
/// (PayPal, Stripe, Wompi...) y se cambia el registro en Program.cs.
/// </summary>
public interface IPasarelaPago
{
    string Nombre { get; }
    Task<ResultadoCobro> CobrarAsync(SolicitudCobro solicitud);
}

/// <summary>
/// Pasarela de demostración: no mueve dinero. Se comporta como las tarjetas de prueba de las pasarelas reales:
/// termina en 0002 = rechazada, termina en 9995 = fondos insuficientes, cualquier otra válida = aprobada.
/// </summary>
public class PasarelaSimulada : IPasarelaPago
{
    public string Nombre => "Simulado";
    public int RetrasoMs { get; init; } = 1200;

    public async Task<ResultadoCobro> CobrarAsync(SolicitudCobro solicitud)
    {
        if (RetrasoMs > 0) await Task.Delay(RetrasoMs);

        var digitos = Tarjeta.SoloDigitos(solicitud.Numero);
        var marca = Tarjeta.Marca(digitos);
        var ultimos4 = digitos.Length >= 4 ? digitos.Substring(digitos.Length - 4) : digitos;
        var referencia = "SIM-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();

        return ultimos4 switch
        {
            "0002" => new ResultadoCobro(false, referencia, marca, ultimos4, "El banco rechazó la tarjeta."),
            "9995" => new ResultadoCobro(false, referencia, marca, ultimos4, "Fondos insuficientes."),
            _ => new ResultadoCobro(true, referencia, marca, ultimos4, "Pago aprobado (simulado).")
        };
    }
}
