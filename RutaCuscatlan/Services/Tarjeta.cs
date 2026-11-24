namespace RutaCuscatlan.Services;

/// <summary>Validaciones básicas de tarjeta. Nunca se guarda el número completo ni el CVC.</summary>
public static class Tarjeta
{
    public static string SoloDigitos(string? texto) =>
        new string((texto ?? "").Where(char.IsDigit).ToArray());

    public static bool LuhnValido(string digitos)
    {
        if (digitos.Length < 13 || digitos.Length > 19) return false;
        var suma = 0;
        var alterna = false;
        for (var i = digitos.Length - 1; i >= 0; i--)
        {
            var n = digitos[i] - '0';
            if (alterna)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            suma += n;
            alterna = !alterna;
        }
        return suma % 10 == 0;
    }

    public static string Marca(string digitos)
    {
        if (digitos.StartsWith("4")) return "Visa";
        if (digitos.Length >= 2 && int.TryParse(digitos.Substring(0, 2), out var d2))
        {
            if (d2 >= 51 && d2 <= 55) return "Mastercard";
            if (d2 == 34 || d2 == 37) return "American Express";
        }
        if (digitos.Length >= 4 && int.TryParse(digitos.Substring(0, 4), out var d4) && d4 >= 2221 && d4 <= 2720)
            return "Mastercard";
        return "Tarjeta";
    }

    /// <summary>Acepta MM/AA. La tarjeta vale hasta el último día del mes indicado.</summary>
    public static bool TryParseVencimiento(string? texto, out int mes, out int anio)
    {
        mes = 0; anio = 0;
        var partes = (texto ?? "").Trim().Split('/');
        if (partes.Length != 2) return false;
        if (!int.TryParse(partes[0], out mes) || !int.TryParse(partes[1], out var aa)) return false;
        if (mes < 1 || mes > 12 || partes[1].Length != 2) return false;
        anio = 2000 + aa;
        return true;
    }

    public static bool Vencida(int mes, int anio, DateTime ahora) =>
        anio < ahora.Year || (anio == ahora.Year && mes < ahora.Month);
}
