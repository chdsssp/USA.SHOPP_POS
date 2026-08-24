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
**Fases 1–7 implementadas; sistema funcional e instalable.** La solución `Usashopp.Pos.sln`
tiene las 4 capas + tests. Módulos completos: Inventario (alta/edición/ajuste/kardex),
Categorías (CRUD), POS
(búsqueda/lector, grid, carrito, descuentos línea+global, cliente, cobro), Ventas
(historial/cancelación/devolución/notas/vista previa de ticket), Corte de caja + historial de cortes,
Clientes, Proveedores, Compras, Apartados, Login + Usuarios/roles/permisos + mi cuenta,
Reportes, Configuración y Respaldos (manual/corte/temporizador/restaurar).
Instalador self-contained x64 con Inno Setup.

**Fase 4 (ESC/POS) simulada** (stubs en Infrastructure/Hardware) — pendiente de impresora real.

Patrones clave: diálogos vía `IDialogService` (ventana + VM, evento `Cerrar(bool)`); ViewModels
usan `IServiceScopeFactory` para resolver servicios scoped; navegación filtrada por permisos en
`ShellViewModel`; ventanas de diálogo usan `SizeToContent="Height"`; tablas y campos numéricos
con estilos/behaviors globales (`Themes/Controls.xaml`, `Common/InputHelpers`).

**Pendiente:** ver el listado completo en [docs/13-estado-y-pendientes.md](docs/13-estado-y-pendientes.md)
(ESC/POS real, devolución parcial, logout sin reiniciar, mostrar notas en ticket, etc.).

Recordatorio: generar la migración inicial de EF antes de correr (ver [docs/10-desarrollo.md](docs/10-desarrollo.md)). WPF solo compila en Windows.
