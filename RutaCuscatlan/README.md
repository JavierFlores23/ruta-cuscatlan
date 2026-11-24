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

## Pagos

El checkout funciona con dos proveedores, elegidos en `appsettings.json` (`Pagos:Proveedor`):

- **`Simulado`** (por defecto): pasarela de demostración, no mueve dinero.
- **`Wompi`**: cobro real con Wompi El Salvador (https://docs.wompi.sv).

En ambos casos la reserva nace **Pendiente** con el cupo apartado 15 minutos y pasa a **Confirmada** cuando el pago es aprobado.
Después se pueden ver el **recibo** (`/Pagos/Recibo/{id}`) y la **factura** (`/Pagos/Factura/{id}`).

### Simulado

Tarjetas de prueba (hay botones en pantalla que las rellenan):

| Número | Resultado |
|---|---|
| 4242 4242 4242 4242 | Aprobada |
| 4000 0000 0000 0002 | Rechazada |
| 4000 0000 0000 9995 | Fondos insuficientes |

Cualquier vencimiento futuro (ej. 12/30) y cualquier CVC de 3 dígitos. Solo se guardan la marca y los últimos 4 dígitos.

### Wompi

1. Crea tu cuenta en https://panel.wompi.sv y entra al detalle de tu negocio: **App ID** = `ClientId`, **API Secret** = `ClientSecret`.
2. Guarda las credenciales **fuera del repositorio** (con `dotnet user-secrets`, o en Visual Studio: clic derecho al proyecto → *Manage User Secrets*):

```
dotnet user-secrets set "Pagos:Proveedor" "Wompi"
dotnet user-secrets set "Wompi:ClientId" "TU_APP_ID"
dotnet user-secrets set "Wompi:ClientSecret" "TU_API_SECRET"
dotnet user-secrets set "Wompi:EmailNotificacion" "tucorreo@ejemplo.com"
```

3. **Modo de prueba:** en el panel de Wompi deja el negocio en *desarrollo*. Ahí todos los cobros se aprueban sin mover dinero; para simular un rechazo escribe `111` como CVV en la pantalla de Wompi.
4. Para cobrar de verdad, pasa el negocio a *productivo* en el panel de Wompi. No hay que cambiar código.

Cómo funciona: el sistema crea un *Enlace de Pago* (`POST /EnlacePago`), manda al cliente a la pantalla de Wompi, y al volver
confirma el pago consultando la API de Wompi desde el servidor (`GET /EnlacePago/{id}`); no se confía en los parámetros de la URL.
Los datos de tarjeta nunca pasan por este sistema. Si el cliente paga pero no vuelve al sitio, al abrir de nuevo "Pagar ahora" se verifica solo.

Wompi no ofrece reembolsos por API: al cancelar una reserva pagada con Wompi queda el aviso **Reembolso pendiente** para devolverlo desde el panel de Wompi.

Si `Wompi:UrlPublica` está vacío, Wompi devuelve al cliente a la dirección con la que abrió el sitio (sirve en `https://localhost`).

### Factura

Los datos del emisor salen de la sección `Facturacion` de `appsettings.json` (nombre, razón social, NIT, NRC, dirección...). Los precios se
asumen con IVA incluido (13 %) y la factura desglosa subtotal e IVA. **Es un documento informativo, no un DTE**: la facturación
electrónica oficial requiere estar inscrito ante el Ministerio de Hacienda y emitir los documentos por sus canales autorizados.
