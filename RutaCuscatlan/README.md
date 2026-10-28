# Ruta Cuscatlán
Aplicación web de turismo en ASP.NET Core MVC (.NET 8), Entity Framework Core y SQLite.

## Ejecutar desde Visual Studio
1. Abre `RutaCuscatlan.csproj` en Visual Studio 2022.
2. Espera a que Visual Studio restaure los paquetes de NuGet.
3. Selecciona el perfil `Ruta Cuscatlán` y presiona **F5**.

También puedes abrir una terminal en la carpeta `RutaCuscatlan` y ejecutar:

```bash
dotnet run
```

En el primer inicio se crea `RutaCuscatlan.db` junto al proyecto, se aplica la migración inicial y se cargan los destinos y tours de ejemplo. No hace falta instalar SQL Server ni LocalDB.

## Pruebas
Desde la carpeta raíz del proyecto:

```bash
dotnet test tests/RutaCuscatlan.Tests/RutaCuscatlan.Tests.csproj
```

## Acceso de demostración
Correo: `admin@ruta.com`  
Contraseña: `Admin123!`

Estas credenciales son solo para desarrollo local. Cámbialas antes de publicar la aplicación en Internet.
