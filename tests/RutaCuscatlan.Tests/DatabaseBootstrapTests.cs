using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Controllers;
using RutaCuscatlan.Data;
using RutaCuscatlan.Models;
using RutaCuscatlan.ViewModels;
using Xunit;

namespace RutaCuscatlan.Tests;

public sealed class DatabaseBootstrapTests
{
    [Fact]
    public async Task InitializeAsync_creates_schema_and_seeds_catalog_only_once()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await DatabaseBootstrap.InitializeAsync(db);
        await DatabaseBootstrap.InitializeAsync(db);

        Assert.Equal(5, await db.Departamentos.CountAsync());
        Assert.Equal(4, await db.Categorias.CountAsync());
        Assert.Equal(6, await db.Destinos.CountAsync());
        Assert.Equal(6, await db.Tours.CountAsync());
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Initial_schema_includes_photo_attribution_fields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await DatabaseBootstrap.InitializeAsync(db);

        var columns = new HashSet<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Destinos');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1));

        Assert.Contains("ImagenCredito", columns);
        Assert.Contains("ImagenFuente", columns);
    }

    [Fact]
    public async Task InitializeAsync_fills_missing_destination_photos_but_preserves_custom_paths()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);

        await DatabaseBootstrap.InitializeAsync(db);
        var suchitoto = await db.Destinos.SingleAsync(d => d.Nombre == "Suchitoto");
        suchitoto.ImagenUrl = "/uploads/suchitoto-personal.jpg";
        await db.SaveChangesAsync();

        await DatabaseBootstrap.InitializeAsync(db);

        var destinos = await db.Destinos.ToListAsync();
        Assert.All(destinos, d => Assert.False(string.IsNullOrWhiteSpace(d.ImagenUrl)));
        Assert.All(destinos, d => Assert.False(string.IsNullOrWhiteSpace(d.ImagenCredito)));
        Assert.All(destinos, d => Assert.False(string.IsNullOrWhiteSpace(d.ImagenFuente)));
        Assert.Equal("/uploads/suchitoto-personal.jpg", destinos.Single(d => d.Nombre == "Suchitoto").ImagenUrl);
    }

    [Fact]
    public async Task Datos_admin_lists_database_records_without_exposing_password_hashes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await DatabaseBootstrap.InitializeAsync(db);

        var controller = new DatosController(db);
        var result = await controller.Index();
        var view = Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(result);
        var model = Assert.IsType<DatosAdminViewModel>(view.Model);

        Assert.Equal(5, model.Departamentos.Count);
        Assert.Equal(4, model.Categorias.Count);
        Assert.Equal(6, model.Destinos.Count);
        Assert.Equal(6, model.Tours.Count);
        Assert.Single(model.Usuarios);
        Assert.Equal(24, model.TotalRegistros);
        Assert.DoesNotContain(nameof(Usuario.PasswordHash), typeof(UsuarioResumenViewModel).GetProperties().Select(p => p.Name));
        var authorization = Assert.Single(typeof(DatosController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true));
        Assert.Equal("Admin", ((Microsoft.AspNetCore.Authorization.AuthorizeAttribute)authorization).Roles);
    }

    [Fact]
    public async Task Catalogo_filters_future_tours_by_destination()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await DatabaseBootstrap.InitializeAsync(db);

        var controller = new ToursController(db);
        var result = await controller.Catalogo("Tunco");
        var view = Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(result);
        var tours = Assert.IsType<List<Tour>>(view.Model);

        var tour = Assert.Single(tours);
        Assert.Equal("El Tunco y El Zonte", tour.Destino!.Nombre);
    }
}
