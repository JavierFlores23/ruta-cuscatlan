using System.Text;
using Microsoft.Extensions.Options;
using RutaCuscatlan.Services;
using Xunit;

namespace RutaCuscatlan.Tests;

public sealed class WompiTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<string, string> _respuesta;
        public List<(string Method, string Url, string? Auth, string Body)> Llamadas { get; } = new();

        public FakeHandler(Func<string, string> respuesta) => _respuesta = respuesta;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            var url = request.RequestUri!.ToString();
            Llamadas.Add((request.Method.Method, url, request.Headers.Authorization?.ToString(), body));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_respuesta(url), Encoding.UTF8, "application/json")
            };
        }
    }

    private static WompiOptions Opciones() => new()
    {
        ClientId = "APP-ID",
        ClientSecret = "SECRETO",
        EmailNotificacion = "ventas@ejemplo.com"
    };

    private static WompiClient Cliente(FakeHandler handler) =>
        new(new HttpClient(handler), Options.Create(Opciones()));

    private static string Token(string url) =>
        url.Contains("connect/token") ? "{\"access_token\":\"tok123\",\"expires_in\":3600}" : "";

    [Fact]
    public async Task CrearEnlace_autentica_y_envia_el_enlace_de_pago()
    {
        var handler = new FakeHandler(url => url.Contains("connect/token")
            ? Token(url)
            : "{\"idEnlace\":15,\"urlEnlace\":\"https://lk.wompi.sv/abcd\",\"estaProductivo\":false}");

        var enlace = await Cliente(handler).CrearEnlaceAsync("RC-1-abc", 25.5m, "Tour de prueba", "Reserva 1",
            "https://sitio/Pagos/Retorno/1", "https://sitio/Pagos/Checkout/1", 15);

        Assert.Equal("15", enlace.IdEnlace);
        Assert.Equal("https://lk.wompi.sv/abcd", enlace.UrlEnlace);
        Assert.False(enlace.EstaProductivo);

        Assert.Equal(2, handler.Llamadas.Count);
        var token = handler.Llamadas[0];
        Assert.Equal("https://id.wompi.sv/connect/token", token.Url);
        Assert.Contains("grant_type=client_credentials", token.Body);
        Assert.Contains("audience=wompi_api", token.Body);
        Assert.Contains("client_id=APP-ID", token.Body);

        var crear = handler.Llamadas[1];
        Assert.Equal("POST", crear.Method);
        Assert.Equal("https://api.wompi.sv/EnlacePago", crear.Url);
        Assert.Equal("Bearer tok123", crear.Auth);
        Assert.Contains("\"identificadorEnlaceComercio\":\"RC-1-abc\"", crear.Body);
        Assert.Contains("\"monto\":25.5", crear.Body);
        Assert.Contains("\"permitirTarjetaCreditoDebido\":true", crear.Body);
        Assert.Contains("\"emailsNotificacion\":\"ventas@ejemplo.com\"", crear.Body);
    }

    [Fact]
    public async Task ConsultarEnlace_devuelve_la_transaccion_aprobada()
    {
        var handler = new FakeHandler(url => url.Contains("connect/token")
            ? Token(url)
            : "{\"cantidadIntentoPagoFallidos\":1,\"transaccionCompra\":{\"resultadoTransaccion\":0,\"idTransaccion\":\"T-1\",\"esReal\":false,\"esAprobada\":true,\"codigoAutorizacion\":\"A1\",\"mensaje\":\"\",\"formaPago\":0,\"monto\":25.5},\"transacciones\":[]}");

        var consulta = await Cliente(handler).ConsultarEnlaceAsync("15");

        Assert.NotNull(consulta.Aprobada);
        Assert.Equal("T-1", consulta.Aprobada!.IdTransaccion);
        Assert.False(consulta.Aprobada.EsReal);
        Assert.Equal(25.5m, consulta.Aprobada.Monto);
        Assert.Equal(1, consulta.IntentosFallidos);
        Assert.Equal("https://api.wompi.sv/EnlacePago/15", handler.Llamadas[1].Url);
    }

    [Fact]
    public async Task ConsultarEnlace_sin_pago_aprobado_devuelve_el_ultimo_mensaje()
    {
        var handler = new FakeHandler(url => url.Contains("connect/token")
            ? Token(url)
            : "{\"transaccionCompra\":null,\"transacciones\":[{\"esAprobada\":false,\"mensaje\":\"CVV invalido\",\"monto\":25.5}]}");

        var consulta = await Cliente(handler).ConsultarEnlaceAsync("15");

        Assert.Null(consulta.Aprobada);
        Assert.Equal("CVV invalido", consulta.UltimoMensaje);
    }

    [Fact]
    public void Configurado_exige_credenciales_y_correo()
    {
        Assert.True(Opciones().Configurado);
        Assert.False(new WompiOptions().Configurado);
        Assert.False(new WompiOptions { ClientId = "a", ClientSecret = "b" }.Configurado);
    }
}
