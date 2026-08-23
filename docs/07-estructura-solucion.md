# 07 — Estructura de la solución

## Solución .NET (proyectos)

Un proyecto por capa de Clean Architecture, más pruebas:

```
Usashopp.Pos.sln
│
├── src/
│   ├── Usashopp.Pos.Domain/            ← Núcleo. Sin dependencias externas.
│   ├── Usashopp.Pos.Application/        ← Casos de uso, interfaces (puertos), DTOs.
│   ├── Usashopp.Pos.Infrastructure/     ← EF Core/SQLite, impresión, respaldos.
│   └── Usashopp.Pos.Wpf/                ← UI WPF (MVVM). Proyecto de arranque.
│
└── tests/
    ├── Usashopp.Pos.Domain.Tests/
    ├── Usashopp.Pos.Application.Tests/
    └── Usashopp.Pos.Infrastructure.Tests/
```

### Referencias entre proyectos (dirección de dependencias)

```
Domain          → (nada)
Application      → Domain
Infrastructure   → Application, Domain
Wpf              → Application, Domain, Infrastructure (solo para registrar DI en el arranque)
```

> `Wpf` referencia `Infrastructure` **únicamente** en el punto de composición (arranque/DI). El resto de la UI depende solo de `Application`/`Domain`.

## Estructura interna por proyecto

### `Usashopp.Pos.Domain`
```
Domain/
├── Common/            # EntidadBase, IEntidadAuditable, Enumeration
├── ValueObjects/      # Dinero, Sku, CodigoBarras, Descuento
├── Entities/          # Producto, VarianteProducto, Venta, DetalleVenta, Apartado…
├── Enums/             # MetodoPago, EstadoVenta, TipoMovimientoInventario…
├── Services/          # CalculadoraTotalesVenta, GeneradorFolios
└── Exceptions/        # StockInsuficienteException, CajaNoAbiertaException…
```

### `Usashopp.Pos.Application`
```
Application/
├── Common/
│   ├── Interfaces/    # IUnitOfWork, I*Repository, IDateTime, ICurrentUser
│   ├── Interfaces/Hardware/  # ITicketPrinter, ICashDrawer, IBarcodeScanner
│   ├── Interfaces/System/    # IBackupService
│   ├── Models/        # Result<T>, PagedList<T>
│   └── Mapping/       # perfiles/mapeo entidad↔DTO
├── Ventas/
│   ├── Dtos/          # VentaDto, LineaVentaDto, CobroDto
│   ├── Services/      # RegistrarVentaService, DevolucionService
│   └── Validators/    # FluentValidation
├── Productos/         # BuscarProductosQuery, dtos, validators
├── Inventario/
├── Apartados/
├── Compras/
├── Clientes/  Proveedores/
├── Caja/              # AbrirCajaService, CerrarCajaService (corte)
├── Reportes/
├── Usuarios/          # autenticación, permisos
└── DependencyInjection.cs   # AddApplication()
```

### `Usashopp.Pos.Infrastructure`
```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/    # IEntityTypeConfiguration<T> por entidad
│   ├── Repositories/      # ProductoRepository, VentaRepository…
│   ├── UnitOfWork.cs
│   ├── Migrations/        # generadas por EF Core
│   └── Seed/              # datos iniciales (roles, permisos, config)
├── Hardware/
│   ├── EscPosTicketPrinter.cs
│   ├── EscPosCashDrawer.cs
│   └── BarcodeScanner (si se requiere manejo especial)
├── System/
│   ├── SqliteBackupService.cs
│   ├── SystemDateTime.cs
│   └── CurrentUserService.cs
├── Logging/              # configuración Serilog
└── DependencyInjection.cs   # AddInfrastructure(config)
```

### `Usashopp.Pos.Wpf`
```
Wpf/
├── App.xaml / App.xaml.cs        # arranque: Generic Host, DI, migraciones, MainWindow
├── appsettings.json              # conexión, impresora, tienda
├── Fonts/                        # Inter
├── Assets/                       # íconos, imágenes, placeholder
├── Themes/                       # ResourceDictionaries: Colors, Typography, Buttons, Inputs, Cards…
│   ├── Tokens.xaml               # colores, tamaños, radios (de doc 06)
│   ├── Controls.xaml             # estilos de Button, TextBox, DataGrid…
│   └── Theme.xaml                # merge de todo
├── Common/
│   ├── ViewModelBase.cs
│   ├── Navigation/               # INavigationService + implementación
│   └── Converters/               # BoolToVisibility, MoneyFormat…
├── Controls/                     # UserControls reutilizables (NumericKeypad, QuantityStepper, StatusBadge…)
├── Features/                     # una carpeta por módulo, View + ViewModel juntos
│   ├── Shell/                    # MainWindow, ShellViewModel (nav + barra superior)
│   ├── Pos/                      # PosView.xaml + PosViewModel + CobroDialog
│   ├── Inventario/
│   ├── Ventas/
│   ├── Apartados/
│   ├── Compras/
│   ├── Clientes/  Proveedores/
│   ├── Reportes/
│   ├── Caja/                     # apertura y corte
│   ├── Configuracion/
│   └── Login/
└── DependencyInjection.cs        # AddPresentation() registra ViewModels y servicios de UI
```

## Convenciones de código

- **Idioma del código:** nombres de dominio en **español** (Producto, Venta, Apartado) para alinear con el negocio; términos técnicos/patrones en inglés cuando es estándar (Repository, Service, Dto, ViewModel).
- **Nullable reference types** habilitado; **warnings as errors** en `Domain` y `Application`.
- **Async/await** en toda operación de datos/IO (`...Async`, `CancellationToken` donde aplique).
- **`Result<T>`** en Application para operaciones que pueden fallar por reglas de negocio (evita excepciones para flujo esperado); excepciones solo para lo excepcional.
- **DTOs** cruzan hacia la UI; las **entidades de dominio no salen** de Application/Infrastructure.
- **Un archivo por clase**; carpetas por feature en la UI (View + ViewModel juntos).
- **Estilos centralizados** en `Themes/`; nada de colores/tamaños "hardcodeados" en las vistas — siempre tokens (`{StaticResource ...}`).
- **Formato/analizadores:** `.editorconfig` compartido; `dotnet format` en el flujo.

## Arranque de la aplicación (composición)

`App.xaml.cs` (esqueleto conceptual):

```
1. Construir Generic Host:
   - AddApplication()
   - AddInfrastructure(configuration)   // DbContext SQLite, repos, hardware, backups
   - AddPresentation()                  // ViewModels, navegación, servicios de UI
   - Serilog
2. Aplicar migraciones + seed (Database.Migrate()).
3. Resolver y mostrar Login (o MainWindow si hay sesión).
4. Manejo global de excepciones no controladas → log + diálogo amigable.
```

## Configuración (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "AppDb": "Data Source=%ProgramData%/USASHOPP POS/data/pos.db"
  },
  "Impresora": {
    "Nombre": "EPSON TM-T20",
    "AnchoColumnas": 42,
    "AbrirCajon": true
  },
  "Respaldos": {
    "Carpeta": "%ProgramData%/USASHOPP POS/backups",
    "CarpetaNube": "",
    "RetenerUltimos": 30,
    "CadaHoras": 4
  }
}
```

## Control de versiones

- Inicializar **Git** en la raíz del proyecto (aún no está inicializado).
- `.gitignore` de .NET (bin/obj, `*.user`, artefactos de publicación, `*.db` locales).
- Ramas: `main` estable + ramas por feature.
