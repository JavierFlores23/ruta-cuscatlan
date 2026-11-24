using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;
using RutaCuscatlan.Services;
using Xunit;

namespace RutaCuscatlan.Tests;

public sealed class PagosTests
{
    [Theory]
    [InlineData("4242424242424242", true)]
    [InlineData("4000000000000002", true)]
    [InlineData("4242424242424241", false)]
    [InlineData("1234", false)]
    public void Luhn_valida_numeros_de_tarjeta(string numero, bool esperado) =>
        Assert.Equal(esperado, Tarjeta.LuhnValido(numero));

    [Fact]
    public void Vencimiento_se_interpreta_y_detecta_tarjetas_vencidas()
    {
        Assert.True(Tarjeta.TryParseVencimiento("12/30", out var mes, out var anio));
        Assert.Equal(12, mes);
        Assert.Equal(2030, anio);
        Assert.False(Tarjeta.TryParseVencimiento("13/30", out _, out _));
        Assert.True(Tarjeta.Vencida(1, 2020, new DateTime(2026, 11, 11)));
        Assert.False(Tarjeta.Vencida(11, 2026, new DateTime(2026, 11, 11)));
    }

    [Theory]
    [InlineData("4242 4242 4242 4242", true, "Visa")]
    [InlineData("4000 0000 0000 0002", false, "Visa")]
    [InlineData("4000 0000 0000 9995", false, "Visa")]
    public async Task Pasarela_simulada_responde_segun_la_tarjeta_de_prueba(string numero, bool aprobado, string marca)
    {
        var pasarela = new PasarelaSimulada { RetrasoMs = 0 };
        var resultado = await pasarela.CobrarAsync(new SolicitudCobro(25m, numero, "Cliente Demo", 12, 2030, "123", "Prueba"));

        Assert.Equal(aprobado, resultado.Aprobado);
        Assert.Equal(marca, resultado.Marca);
        Assert.StartsWith("SIM-", resultado.Referencia);
        Assert.Equal(4, resultado.Ultimos4.Length);
    }

    [Fact]
    public async Task Reserva_nace_pendiente_y_cancelar_reembolsa_el_pago_aprobado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await DatabaseBootstrap.InitializeAsync(db);

        var cliente = new Cliente { Nombre = "Cliente Demo", Correo = "demo@test.com" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        var tour = await db.Tours.FirstAsync(t => t.FechaSalida > DateTime.Now);

        var svc = new ReservaService(db);
        var (ok, _, reservaId) = await svc.CrearAsync(tour.Id, cliente.Id, 1);
        Assert.True(ok);
        Assert.Equal("Pendiente", (await db.Reservas.FindAsync(reservaId))!.Estado);

        db.Pagos.Add(new Pago { ReservaId = reservaId, Monto = tour.Precio, Estado = "Aprobado", Referencia = "SIM-TEST", Ultimos4 = "4242" });
        await db.SaveChangesAsync();

        Assert.True(await svc.CancelarAsync(reservaId, cliente.Id, false));
        Assert.Equal("Reembolsado", (await db.Pagos.SingleAsync()).Estado);
    }

    [Fact]
    public async Task Esquema_incluye_la_tabla_Pagos_sin_columnas_de_tarjeta_completa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await DatabaseBootstrap.InitializeAsync(db);

        var columnas = new HashSet<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Pagos');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) columnas.Add(reader.GetString(1));

        Assert.Contains("Ultimos4", columnas);
        Assert.DoesNotContain("Numero", columnas);
        Assert.DoesNotContain("Cvc", columnas);
    }
}
