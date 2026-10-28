using Microsoft.AspNetCore.Identity;
using RutaCuscatlan.Models;

namespace RutaCuscatlan.Data;

public static class DbSeeder
{
    private sealed record FotoDestino(string Url, string Credito, string Fuente);

    private static readonly Dictionary<string, FotoDestino> Fotos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ruta de las Flores"] = new("/images/destinos/ruta-flores-ataco.jpg", "David Stanley · CC BY 2.0", "https://commons.wikimedia.org/wiki/File:Ataco_(7185455734).jpg"),
        ["El Tunco y El Zonte"] = new("/images/destinos/el-tunco-surf.jpg", "Casa Presidencial El Salvador · CC0", "https://commons.wikimedia.org/wiki/File:Surfistas_en_El_Tunco.jpg"),
        ["Volcán de Santa Ana"] = new("/images/destinos/volcan-santa-ana.jpg", "Emberlifi · CC BY-SA 4.0", "https://commons.wikimedia.org/wiki/File:Crat%C3%A8re_du_Volcan_de_Santa_Ana,_Salvador.jpg"),
        ["Lago de Coatepeque"] = new("/images/destinos/lago-coatepeque.jpg", "JMRAFFi · CC BY 4.0", "https://commons.wikimedia.org/wiki/File:Coatepeque_Vista.jpg"),
        ["Suchitoto"] = new("/images/destinos/suchitoto.jpg", "Hilda Gómez · CC BY-SA 4.0", "https://commons.wikimedia.org/wiki/File:Calle_empedrada_de_Suchitoto.jpg"),
        ["Joya de Cerén"] = new("/images/destinos/joya-ceren.jpg", "BelenM · CC BY-SA 3.0", "https://commons.wikimedia.org/wiki/File:Joya_de_Ceren,_La_Libertad.JPG")
    };

    public static void Seed(AppDbContext db)
    {
        var destinosExistentes = db.Destinos.ToList();
        if (destinosExistentes.Count > 0)
        {
            var actualizados = false;
            foreach (var destino in destinosExistentes)
            {
                if (!string.IsNullOrWhiteSpace(destino.ImagenUrl) || !Fotos.TryGetValue(destino.Nombre, out var foto))
                    continue;

                AplicarFoto(destino, foto);
                actualizados = true;
            }

            if (actualizados) db.SaveChanges();
            return;
        }

        var dep = new[] { "Sonsonate", "Ahuachapán", "La Libertad", "Santa Ana", "Cuscatlán" }
            .Select(n => new Departamento { Nombre = n }).ToList();
        var cat = new[] { "Cultural", "Playa", "Volcán", "Naturaleza" }
            .Select(n => new Categoria { Nombre = n }).ToList();
        var guias = new List<Guia>
        {
            new() { Nombre = "Carlos Méndez", Telefono = "7000-0001", Idiomas = "Español, Inglés" },
            new() { Nombre = "Ana Rivas", Telefono = "7000-0002", Idiomas = "Español, Inglés, Francés" }
        };
        db.AddRange(dep); db.AddRange(cat); db.AddRange(guias);
        db.SaveChanges();

        Departamento D(string n) => dep.First(x => x.Nombre == n);
        Categoria C(string n) => cat.First(x => x.Nombre == n);
        Destino CrearDestino(string nombre, string descripcion, string departamento, string categoria)
        {
            var destino = new Destino
            {
                Nombre = nombre,
                Descripcion = descripcion,
                Departamento = D(departamento),
                Categoria = C(categoria)
            };
            if (Fotos.TryGetValue(nombre, out var foto)) AplicarFoto(destino, foto);
            return destino;
        }

        var destinos = new List<Destino>
        {
            CrearDestino("Ruta de las Flores", "Juayúa, Apaneca y Ataco: murales, feria gastronómica y fincas de café.", "Sonsonate", "Naturaleza"),
            CrearDestino("El Tunco y El Zonte", "Surf, atardeceres y playas con ambiente para todos los gustos.", "La Libertad", "Playa"),
            CrearDestino("Volcán de Santa Ana", "Caminata guiada hasta el cráter con laguna turquesa.", "Santa Ana", "Volcán"),
            CrearDestino("Lago de Coatepeque", "Aguas azules dentro de una caldera volcánica. Kayak y miradores.", "Santa Ana", "Naturaleza"),
            CrearDestino("Suchitoto", "Calles empedradas, arte local y vista al lago Suchitlán.", "Cuscatlán", "Cultural"),
            CrearDestino("Joya de Cerén", "Aldea maya preservada bajo ceniza, Patrimonio de la Humanidad.", "La Libertad", "Cultural")
        };
        db.AddRange(destinos);
        db.SaveChanges();

        var precios = new[] { 35m, 25m, 30m, 28m, 32m, 20m };
        for (var i = 0; i < destinos.Count; i++)
        {
            db.Tours.Add(new Tour
            {
                Titulo = "Tour " + destinos[i].Nombre,
                Descripcion = "Salida guiada con transporte incluido.",
                Precio = precios[i],
                DuracionHoras = 5 + i,
                CupoMaximo = 10,
                FechaSalida = DateTime.Today.AddDays(7 + i * 3).AddHours(8),
                DestinoId = destinos[i].Id,
                GuiaId = guias[i % 2].Id
            });
        }

        var hasher = new PasswordHasher<Usuario>();
        var admin = new Usuario { Correo = "admin@ruta.com", Rol = "Admin" };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
        db.Usuarios.Add(admin);
        db.SaveChanges();
    }

    private static void AplicarFoto(Destino destino, FotoDestino foto)
    {
        destino.ImagenUrl = foto.Url;
        destino.ImagenCredito = foto.Credito;
        destino.ImagenFuente = foto.Fuente;
    }
}
