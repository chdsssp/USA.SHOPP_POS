# 02 — Arquitectura (Clean Architecture)

## Objetivo

Separar el **negocio** de la **tecnología**. Las reglas de la tienda (cómo se calcula una venta, qué es un apartado, cuándo baja el stock) no deben depender de WPF, de SQLite ni de la impresora. Esto hace el sistema **testeable, mantenible y a prueba de cambios** (por ejemplo, cambiar SQLite por SQL Server, o WPF por otra UI, sin tocar el negocio).

## Las 4 capas

```
┌──────────────────────────────────────────────────────────────┐
│                     PRESENTATION  (WPF)                        │
│   Views (XAML) · ViewModels (MVVM) · Converters · DI host     │
│   Depende de → Application (y Domain para tipos simples)       │
└──────────────────────────────────────────────────────────────┘
                              ▼ usa
┌──────────────────────────────────────────────────────────────┐
│                        APPLICATION                            │
│   Casos de uso / servicios · DTOs · Interfaces (puertos):     │
│   IRepository, IUnitOfWork, ITicketPrinter, IBackupService…   │
│   Validación (FluentValidation) · Mapeo                       │
│   Depende de → Domain                                          │
└──────────────────────────────────────────────────────────────┘
                              ▼ usa
┌──────────────────────────────────────────────────────────────┐
│                          DOMAIN                               │
│   Entidades · Value Objects (Dinero, Sku, CodigoBarras)       │
│   Enums · Reglas y servicios de dominio · Excepciones         │
│   NO depende de nada (corazón del sistema)                     │
└──────────────────────────────────────────────────────────────┘
                              ▲ implementa las interfaces
┌──────────────────────────────────────────────────────────────┐
│                       INFRASTRUCTURE                          │
│   EF Core + SQLite · Repositorios · UnitOfWork                │
│   Impresión ESC/POS · Cajón de dinero · Respaldos · Logging   │
│   Depende de → Application + Domain                            │
└──────────────────────────────────────────────────────────────┘
```

## Regla de dependencias (la más importante)

**Las dependencias apuntan siempre hacia adentro.**

- `Domain` no conoce a nadie.
- `Application` conoce solo a `Domain`.
- `Infrastructure` y `Presentation` conocen a `Application` y `Domain`, pero **no al revés**.
- `Infrastructure` **implementa** las interfaces declaradas en `Application` (inversión de dependencias). La UI y la infraestructura se "enchufan" en tiempo de ejecución mediante **inyección de dependencias**.

Esto significa: `Domain` y `Application` **no referencian** EF Core, WPF ni ninguna librería de infraestructura.

## Responsabilidad de cada capa

### Domain (`Usashopp.Pos.Domain`)
El corazón. Sin dependencias externas.
- **Entidades**: `Producto`, `VarianteProducto`, `Venta`, `DetalleVenta`, `Apartado`, `Cliente`, etc.
- **Value Objects**: `Dinero` (monto + moneda, evita errores de decimales), `Sku`, `CodigoBarras`.
- **Enums**: `MetodoPago`, `EstadoVenta`, `TipoMovimientoInventario`, etc.
- **Reglas de dominio**: p. ej., una `Venta` recalcula su total al agregar líneas; un `Apartado` no se liquida si el saldo > 0.
- **Excepciones de dominio**: `StockInsuficienteException`, `CajaNoAbiertaException`.

### Application (`Usashopp.Pos.Application`)
Orquesta los casos de uso. Define **qué** hace el sistema, no **cómo** se persiste o se imprime.
- **Casos de uso / servicios**: `RegistrarVentaService`, `BuscarProductosQuery`, `AbrirCajaService`, `RegistrarAbonoApartadoService`…
- **Puertos (interfaces)** que la infraestructura implementará:
  - `IProductoRepository`, `IVentaRepository`, `IUnitOfWork`
  - `ITicketPrinter`, `ICashDrawer`, `IBackupService`, `IDateTime`, `ICurrentUser`
- **DTOs** para comunicarse con la UI (la UI no recibe entidades de dominio directamente).
- **Validación** con FluentValidation.

> **Decisión pragmática:** usamos **servicios de casos de uso** en lugar de CQRS con MediatR completo, para mantener el sistema ligero y sencillo en el equipo objetivo. Si el proyecto crece, migrar a MediatR es un cambio localizado. (Ver [Stack](03-stack-tecnologico.md).)

### Infrastructure (`Usashopp.Pos.Infrastructure`)
El **cómo**. Implementa los puertos de Application.
- **Persistencia**: `AppDbContext` (EF Core + SQLite), repositorios, `UnitOfWork`, migraciones.
- **Impresión**: `EscPosTicketPrinter : ITicketPrinter`, `EscPosCashDrawer : ICashDrawer`.
- **Respaldos**: `SqliteBackupService : IBackupService`.
- **Servicios del sistema**: reloj (`SystemDateTime`), usuario actual, logging (Serilog).

### Presentation (`Usashopp.Pos.Wpf`)
La cara visible. Patrón **MVVM**.
- **Views** (XAML): pantallas y controles. Cero lógica de negocio en el code-behind.
- **ViewModels**: estado y comandos de cada pantalla; llaman a los servicios de Application. Usan **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`).
- **Composición**: un **Generic Host** (`Microsoft.Extensions.Hosting`) registra en el contenedor de DI todas las capas y arranca la ventana principal.

## MVVM en detalle

```
Vista (XAML)  ── binding ──▶  ViewModel  ── llama ──▶  Application Service
      ▲                            │                          │
      └──── notifica cambios ──────┘                          ▼
                                                    Domain + Infrastructure
```

- **Binding bidireccional** entre controles y propiedades del ViewModel.
- **Commands** (`RelayCommand`) para las acciones (Agregar, Cobrar, Guardar…).
- **Navegación** mediante un `INavigationService` (implementado en Presentation) que intercambia ViewModels en la región principal.
- **Sin lógica en code-behind**: solo, excepcionalmente, cosas puramente visuales (foco, animaciones).

## Flujo de ejemplo — "Registrar una venta"

1. El cajero agrega productos → el `PosViewModel` mantiene la lista de líneas (DTOs) y totales.
2. Presiona **Cobrar** → `PosViewModel.CobrarCommand` abre el diálogo de pago.
3. Confirmado el pago → llama a `RegistrarVentaService.EjecutarAsync(ventaDto)`.
4. El servicio (Application):
   - Valida (caja abierta, stock suficiente, montos correctos) con reglas de **Domain**.
   - Construye la entidad `Venta`, descuenta stock (`MovimientoInventario`).
   - Persiste vía `IUnitOfWork` (**Infrastructure**).
   - Solicita imprimir con `ITicketPrinter` y abrir cajón con `ICashDrawer`.
5. Devuelve el resultado a la UI (folio, cambio) y el ViewModel limpia el carrito.

Ni el ViewModel ni el servicio saben que por debajo hay SQLite o una impresora Epson: solo hablan con **interfaces**.

## Manejo de errores y transacciones

- Operaciones que tocan varias tablas (venta + inventario + pago) van dentro de una **transacción** vía `IUnitOfWork`.
- Excepciones de dominio se traducen a mensajes claros en la UI (nunca stack traces al usuario).
- **Serilog** registra a archivo rotativo para diagnóstico.

## Pruebas

- **Domain** y **Application**: pruebas unitarias puras (sin base de datos, con dobles/mocks de los puertos). Es donde vive el valor del negocio.
- **Infrastructure**: pruebas de integración con SQLite en memoria/archivo temporal.
- **Presentation**: los ViewModels son testeables porque no dependen de WPF.

Ver herramientas de test en [Stack tecnológico](03-stack-tecnologico.md) y organización en [Estructura de la solución](07-estructura-solucion.md).
