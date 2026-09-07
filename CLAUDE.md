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
- **Desarrollo en Windows 11 con .NET 8 SDK** (VS 2022 o VS Code + C# Dev Kit). Aquí **sí** se
  compila, ejecuta y prueba. Úsalo: `dotnet build`, `dotnet run --project src/Usashopp.Pos.Wpf`,
  `dotnet test`, `dotnet ef …`. **Valida compilando** — buena parte de los lotes recientes se
  escribió desde macOS **a ciegas** (sin compilador), así que al integrar corre build/tests y
  corrige lo que aparezca antes de dar por bueno un lote.
- Git **inicializado**; remoto en GitHub (`origin`), rama de trabajo `main`. Sube cada lote con commit.
- **Pendiente inmediato: generar la migración inicial de EF** (no existe carpeta `Migrations/` aún).
  Ver "Acción inmediata" abajo y [docs/14-roadmap-lotes.md](docs/14-roadmap-lotes.md).

## Estado
**Fases 1–7 implementadas; sistema funcional e instalable.** La solución `Usashopp.Pos.sln`
tiene las 4 capas + tests. Módulos completos: Inventario (alta/edición/ajuste/kardex con
filtros+CSV), Categorías (CRUD), POS (búsqueda/lector, grid, carrito con **cantidad y precio
editables**, descuentos línea+global, cliente al vuelo, **pago mixto**, **venta en espera**,
notas, atajos F2–F9, pantalla completa F11), Ventas (historial/cancelación/**devolución parcial**/
notas/vista previa de ticket con descuentos visibles), **Caja** (apertura, **movimientos:
ingresos/retiros/gastos** [requiere migración], corte con ingresos/salidas + historial de cortes),
Clientes, Proveedores, Compras, Apartados, Login + Usuarios/roles/permisos + mi cuenta + **cerrar
sesión sin reiniciar**, **Reportes** (utilidad/margen, inventario valorizado, por forma de pago/
usuario/categoría/hora, comparativo, sin movimiento, export CSV), Configuración y Respaldos
(manual/corte/temporizador/**restaurar**). Instalador self-contained x64 con Inno Setup.

**Plan de pulido por lotes** (progreso y qué sigue): [docs/14-roadmap-lotes.md](docs/14-roadmap-lotes.md).
Lotes sin migración hechos: 1, 2, 7, 12. En curso: **3a (movimientos de caja)** — falta la migración.

**Fase 4 (ESC/POS) simulada** (stubs en Infrastructure/Hardware) — pendiente de impresora real.

Patrones clave: diálogos vía `IDialogService` (ventana + VM, evento `Cerrar(bool)`); ViewModels
usan `IServiceScopeFactory` para resolver servicios scoped; navegación filtrada por permisos en
`ShellViewModel`; ventanas de diálogo usan `SizeToContent="Height"`; tablas y campos numéricos
con estilos/behaviors globales (`Themes/Controls.xaml`, `Common/InputHelpers`).

**Pendiente:** ver el listado completo en [docs/13-estado-y-pendientes.md](docs/13-estado-y-pendientes.md)
(ESC/POS real, devolución con reembolso/totales, apartados con fecha límite, gráficas y
export a Excel, etiquetas de código de barras, etc.) y el plan por lotes en
[docs/14-roadmap-lotes.md](docs/14-roadmap-lotes.md).

## Acción inmediata (al retomar en Windows)
1. `git pull` y **compila**: `dotnet build`. Corrige cualquier error de los lotes escritos a ciegas.
2. **Genera la migración inicial** (no existe ninguna aún; la primera es el baseline completo,
   incluye la tabla `MovimientosCaja` del Lote 3a):
   ```
   dotnet ef migrations add Inicial --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf
   ```
   (Hay `AppDbContextFactory` para que EF cree el contexto sin arrancar WPF. Si existe un `pos.db`
   viejo sin migraciones en `C:\ProgramData\USASHOPP POS\`, bórralo antes.)
3. `dotnet run --project src/Usashopp.Pos.Wpf` — la migración se aplica sola al iniciar.
4. Prueba el **Lote 3a**: abre caja → "Movimiento de caja" → registra ingreso/retiro → verifica el corte.
5. Continúa con el resto del **Lote 3b** y los demás lotes `[BD]` según [docs/14](docs/14-roadmap-lotes.md).
