using Microsoft.EntityFrameworkCore;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Destino> Destinos => Set<Destino>();
    public DbSet<Guia> Guias => Set<Guia>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Cliente>().HasIndex(c => c.Correo).IsUnique();
        b.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();
        b.Entity<Departamento>().HasIndex(d => d.Nombre).IsUnique();
        b.Entity<Categoria>().HasIndex(c => c.Nombre).IsUnique();
        b.Entity<Tour>().HasOne(t => t.Destino).WithMany(d => d.Tours).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reserva>().HasOne(r => r.Tour).WithMany(t => t.Reservas).OnDelete(DeleteBehavior.Restrict);
    }
}
