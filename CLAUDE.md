# CLAUDE.md — USASHOPP POS

Guía para trabajar en este proyecto. Lee también `/docs` (índice en [README.md](README.md)).

## Qué es
Punto de venta + gestión (ERP-lite) para una tienda de ropa y artículos varios. App **nativa de Windows 10**, **WPF + .NET 8**, **Clean Architecture**, **MVVM**. Datos **SQLite local** con respaldos. Español (MX), MXN. Diseño minimalista tema claro estilo Shopify.

## Decisiones ya tomadas (no re-litigar sin pedir)
- Stack: **WPF + .NET 8** (nativo). Alternativa solo-si-no-hay-Windows: Avalonia (reutilizaría Domain/Application/Infrastructure).
- Alcance MVP: **ERP-lite** (POS, inventario con variantes, clientes, proveedores, compras, apartados, usuarios/roles, descuentos, reportes, corte de caja).
- Datos: **SQLite** local + respaldos automáticos.
- Periféricos: lector de código de barras (keyboard-wedge), impresora **ESC/POS**, **cajón** (drawer-kick vía impresora). Terminal bancaria: fuera del MVP (solo se registra el pago).

## Reglas de arquitectura (respetar siempre)
- Dependencias hacia adentro: `Domain` ← `Application` ← (`Infrastructure`, `Wpf`).
- `Domain` y `Application` **no** referencian EF Core, WPF ni infraestructura.
- La UI habla con **servicios de Application** e **interfaces**; nunca con EF/SQLite/impresora directamente.
- Entidades de dominio **no salen** de Application; hacia la UI van **DTOs**.
- El **stock solo cambia** por `MovimientoInventario`. Una **venta requiere caja abierta**. Importes en `decimal`, VO `Dinero`.
- Estilos WPF centralizados en `Themes/`; **nada de colores/tamaños hardcodeados** — usar tokens (doc 06).
- Permisos validados en Application, no solo ocultando botones.

## Convenciones
- Dominio en **español** (Producto, Venta, Apartado); patrones técnicos en inglés (Repository, Service, Dto, ViewModel).
- Async en todo IO; `Result<T>` para fallos de negocio esperados; excepciones solo para lo excepcional.
- Nullable habilitado; MVVM con **CommunityToolkit.Mvvm** (`[ObservableProperty]`, `[RelayCommand]`).
- Una carpeta por feature en la UI (View + ViewModel juntos).

## Entorno
- **WPF solo compila/ejecuta en Windows** (VS 2022 o VS Code + C# Dev Kit + .NET 8 SDK). Esta sesión de docs corre en macOS: **no** intentar `dotnet build` de la UI aquí.
- Git aún **no** inicializado.

## Estado
Fase 0 (docs) y Fase 1 (esqueleto) completas. La solución `Usashopp.Pos.sln` tiene las 4 capas + tests, dominio completo, puertos y 2 casos de uso en Application (`BuscarProductosService`, `RegistrarVentaService`), infraestructura (SQLite + repos + UoW + seed + respaldos + hashing) y WPF (host+DI, temas, shell + pantalla POS de andamiaje).

Pendiente antes de correr en Windows: generar la migración inicial de EF (ver [docs/10-desarrollo.md](docs/10-desarrollo.md)). WPF no compila en esta Mac.

Fases 2 y 3 en progreso.
- **Fase 2 (Inventario):** `CategoriaService`/`ProductoService`/`InventarioService` + pantalla Inventario (búsqueda, bajo stock, alta con variantes, ajuste de stock).
- **Fase 3 (POS):** `CajaService` + pantalla POS real: búsqueda en vivo/lector, grid táctil por categoría, carrito con stepper, apertura de caja, cobro (efectivo/tarjeta, teclado numérico, cambio) y registro de venta que descuenta stock.

Patrones: diálogos vía `IDialogService`; ViewModels usan `IServiceScopeFactory` para servicios scoped; los diálogos cierran con un evento `Cerrar(bool)`. **Auto-login temporal como `admin`** en `SesionBootstrap` (App.OnStartup) hasta la pantalla de login de la Fase 6 — recordar reemplazarlo.

Pendiente: descuentos/notas/cliente/pago mixto en POS, editar producto y CRUD categorías, y las fases 5–7. WPF no compila en esta Mac. Ver [docs/09-roadmap.md](docs/09-roadmap.md).
