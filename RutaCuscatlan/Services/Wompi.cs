using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace RutaCuscatlan.Services;

/// <summary>Qué pasarela usa el checkout: "Simulado" (por defecto) o "Wompi".</summary>
public class PagosOptions
{
    public string Proveedor { get; set; } = "Simulado";
    public bool UsaWompi => string.Equals(Proveedor, "Wompi", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Credenciales de Wompi El Salvador (panel.wompi.sv → detalle del negocio: App ID = ClientId, API Secret = ClientSecret).
/// El secreto NO va en appsettings.json: usa "dotnet user-secrets" o variables de entorno.
/// </summary>
public class WompiOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string UrlToken { get; set; } = "https://id.wompi.sv/connect/token";
    public string UrlApi { get; set; } = "https://api.wompi.sv";
    /// <summary>Wompi exige al menos un correo (o webhook) para crear el enlace de pago.</summary>
    public string EmailNotificacion { get; set; } = "";
    /// <summary>Opcional: URL pública del sitio (por ejemplo la de ngrok). Si está vacía se usa la del navegador.</summary>
    public string UrlPublica { get; set; } = "";

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(EmailNotificacion);
}

public record EnlaceWompi(string IdEnlace, string UrlEnlace, bool EstaProductivo);

public record TransaccionWompi(bool EsAprobada, bool EsReal, string IdTransaccion, string CodigoAutorizacion, string Mensaje, decimal Monto, int FormaPago);

public record ConsultaWompi(TransaccionWompi? Aprobada, string? UltimoMensaje, int IntentosFallidos);

public class WompiException : Exception
{
    public WompiException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Cliente mínimo de la API de Wompi El Salvador (https://docs.wompi.sv):
/// OAuth2 client credentials → crear Enlace de Pago → consultar el enlace para confirmar el pago desde el servidor.
/// El cliente paga en la pantalla de Wompi, así que aquí nunca pasan datos de tarjeta.
/// </summary>
public class WompiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;
    private readonly WompiOptions _opt;
    private string? _token;
    private DateTime _tokenExpira = DateTime.MinValue;

    public WompiClient(HttpClient http, IOptions<WompiOptions> opt)
    {
        _http = http;
        _opt = opt.Value;
    }

    public bool Configurado => _opt.Configurado;

    private async Task<string> TokenAsync()
    {
        if (_token != null && DateTime.UtcNow < _tokenExpira) return _token;

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["audience"] = "wompi_api",
            ["client_id"] = _opt.ClientId,
            ["client_secret"] = _opt.ClientSecret
        });
        using var resp = await _http.PostAsync(_opt.UrlToken, form);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new WompiException($"Wompi rechazó las credenciales ({(int)resp.StatusCode}). Revisa ClientId y ClientSecret.");

        using var doc = JsonDocument.Parse(body);
        _token = doc.RootElement.GetProperty("access_token").GetString()
                 ?? throw new WompiException("Wompi no devolvió un token de acceso.");
        var segundos = doc.RootElement.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 3600;
        _tokenExpira = DateTime.UtcNow.AddSeconds(Math.Max(segundos - 60, 30));
        return _token;
    }

    private async Task<HttpRequestMessage> NuevaPeticionAsync(HttpMethod metodo, string ruta)
    {
        var req = new HttpRequestMessage(metodo, _opt.UrlApi.TrimEnd('/') + ruta);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync());
        return req;
    }

    public async Task<EnlaceWompi> CrearEnlaceAsync(string identificador, decimal monto, string nombreProducto,
        string? descripcion, string urlRedirect, string urlRetorno, int duracionMinutos)
    {
        var payload = new
        {
            identificadorEnlaceComercio = identificador,
            monto,
            nombreProducto = Recortar(nombreProducto, 500),
            formaPago = new
            {
                permitirTarjetaCreditoDebido = true,
                permitirPagoConPuntoAgricola = false,
                permitirPagoEnCuotasAgricola = false,
                permitirPagoEnBitcoin = false,
                permitePagoQuickPay = false
            },
            infoProducto = new { descripcionProducto = descripcion },
            configuracion = new
            {
                urlRedirect,
                urlRetorno,
                emailsNotificacion = _opt.EmailNotificacion,
                notificarTransaccionCliente = true,
                duracionInterfazIntentoMinutos = duracionMinutos
            }
        };

        using var req = await NuevaPeticionAsync(HttpMethod.Post, "/EnlacePago");
        req.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new WompiException($"Wompi no pudo crear el enlace de pago ({(int)resp.StatusCode}).");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var id = root.GetProperty("idEnlace").ToString();
        var url = root.GetProperty("urlEnlace").GetString() ?? throw new WompiException("Wompi no devolvió la URL de pago.");
        var productivo = root.TryGetProperty("estaProductivo", out var p) && p.ValueKind == JsonValueKind.True;
        return new EnlaceWompi(id, url, productivo);
    }

    /// <summary>Consulta el enlace en Wompi y devuelve la transacción aprobada, si existe. Es la fuente de verdad del pago.</summary>
    public async Task<ConsultaWompi> ConsultarEnlaceAsync(string idEnlace)
    {
        using var req = await NuevaPeticionAsync(HttpMethod.Get, "/EnlacePago/" + Uri.EscapeDataString(idEnlace));
        using var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new WompiException($"Wompi no pudo consultar el enlace ({(int)resp.StatusCode}).");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var intentos = root.TryGetProperty("cantidadIntentoPagoFallidos", out var f) && f.TryGetInt32(out var n) ? n : 0;

        var candidatas = new List<TransaccionWompi>();
        if (root.TryGetProperty("transaccionCompra", out var tc) && tc.ValueKind == JsonValueKind.Object)
            candidatas.Add(LeerTransaccion(tc));
        if (root.TryGetProperty("transacciones", out var lista) && lista.ValueKind == JsonValueKind.Array)
            foreach (var t in lista.EnumerateArray()) candidatas.Add(LeerTransaccion(t));

        var aprobada = candidatas.FirstOrDefault(t => t.EsAprobada);
        var mensaje = candidatas.Select(t => t.Mensaje).LastOrDefault(m => !string.IsNullOrWhiteSpace(m));
        return new ConsultaWompi(aprobada, mensaje, intentos);
    }

    private static TransaccionWompi LeerTransaccion(JsonElement e)
    {
        bool Bool(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;
        string Str(string k) => e.TryGetProperty(k, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : "";
        decimal Dec(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d) ? d : 0m;
        int Int(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;

        return new TransaccionWompi(Bool("esAprobada"), Bool("esReal"), Str("idTransaccion"), Str("codigoAutorizacion"),
            Str("mensaje"), Dec("monto"), Int("formaPago"));
    }

    private static string Recortar(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
}

/// <summary>Datos que salen en la factura. Se llenan en appsettings.json (sección "Facturacion").</summary>
public class FacturacionOptions
{
    public string NombreComercial { get; set; } = "Ruta Cuscatlán";
    public string RazonSocial { get; set; } = "";
    public string Nit { get; set; } = "";
    public string Nrc { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Correo { get; set; } = "";
}
