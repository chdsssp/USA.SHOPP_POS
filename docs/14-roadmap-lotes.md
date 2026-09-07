# 14 — Roadmap de pulido por lotes (traspaso entre sesiones)

Este documento es el **hilo de continuidad** del trabajo de pulido posterior a las Fases 1–7.
Resume la estrategia acordada, qué lotes ya se hicieron y qué sigue, para que cualquier sesión
(humana o de Claude) retome sin perder contexto.

## Contexto de cómo se ha trabajado
- Las Fases 1–7 y varios lotes de pulido se escribieron **desde macOS, sin compilador** ("a ciegas"),
  validando después en Windows. **A partir de ahora el desarrollo es en Windows 11 con .NET 8**, así
  que **se compila y prueba de una vez** (`dotnet build` / `dotnet run` / `dotnet test`).
- **Al integrar un lote escrito a ciegas, compila y corrige** lo que aparezca antes de darlo por bueno.
  Ya pasó una vez: `ReportesService` usaba nombres del DTO sobre la entidad `VarianteProducto`
  (se corrigió en commit `edad89c`).

## Acción inmediata pendiente (bloqueante)
**No existe la migración inicial de EF** (no hay carpeta `src/Usashopp.Pos.Infrastructure/Migrations/`).
La primera migración es el **baseline completo** e incluye la tabla `MovimientosCaja` del Lote 3a.

```bash
# 1) (una vez) herramientas EF
dotnet tool install --global dotnet-ef

# 2) si hay un pos.db viejo sin migraciones, bórralo:  C:\ProgramData\USASHOPP POS\pos.db

# 3) generar la migración inicial (baseline)
dotnet ef migrations add Inicial --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf

# 4) correr; la migración se aplica sola al iniciar (DatabaseInitializer.MigrateAsync)
dotnet run --project src/Usashopp.Pos.Wpf
```

Existe `Infrastructure/Persistence/AppDbContextFactory.cs` (`IDesignTimeDbContextFactory`) para que
EF cree el contexto sin arrancar WPF. El archivo `pos-design.db` que genere la herramienta es desechable.

**Qué revisar en la migración**: que cree todas las tablas y que `MovimientosCaja` tenga `Id`,
`SesionCajaId` (FK), `Tipo` (int), `Monto` (decimal 18,2), `Concepto` (≤200), `UsuarioId`, `Fecha`,
`CreadoEn`, `ActualizadoEn`. Los `Dinero` → `decimal(18,2)`; `Sku`/`CodigoBarras`/`Descuento` → texto.

## Estrategia de los lotes
- **Prioridad-primero cruzando categorías** (lo más alto de todas, luego se baja), respetando
  cadenas de dependencia y **agrupando el cambio de esquema: una migración por lote `[BD]`**.
- Los lotes `[BD]` van **de uno en uno, validando en Windows entre cada uno** (una migración mala
  sobre datos reales es cara de revertir; respaldar antes de aplicar sobre datos reales).
- **Antes de tocar entidades**, seguir los patrones EF existentes: converters globales de VOs en
  `AppDbContext.ConfigureConventions` (`Dinero`, `Sku`, `CodigoBarras`, `Descuento`); configs en
  `Persistence/Configurations/ModelConfigurations.cs`; enums se guardan como `int` por defecto.
- `[HW]` (ESC/POS, báscula, cajón, terminal) y **fiscal/CFDI** al final (requieren dispositivo o PAC).

## Estado de los lotes

| Lote | Contenido | Estado |
|---|---|---|
| 1 | Pago mixto, venta en espera, notas en POS, descuentos visibles en ticket/detalle, anti doble-clic | ✅ hecho (sin migración) |
| 2 | Reportería ampliada (utilidad/margen, inventario valorizado, descuentos, devoluciones, por categoría/hora, comparativo, sin movimiento) + kardex con filtros/CSV | ✅ hecho (sin migración) |
| 7 | POS avanzado: cantidad tecleable, edición de precio con permiso, cliente al vuelo, atajos F2–F9 | ✅ hecho (sin migración) |
| 12 | UX: pantalla completa/kiosco F11 (ordenar columnas y virtualización ya vienen por defecto en WPF) | ✅ hecho (sin migración) |
| **3a** | **Movimientos de caja (ingresos/retiros/gastos) + reporte X + corte con ingresos/salidas** | 🟠 **código listo; falta generar/aplicar la migración inicial** |
| 3b | Devolución con reembolso (afecta caja/totales; usar `TipoMovimientoCaja.Reembolso` ya previsto), nota de crédito, conteo por denominaciones en el corte | ⬜ pendiente `[BD]` |
| 4 | Auditoría (bitácora), autorización de supervisor (PIN override), bloqueo por inactividad, política de contraseñas | ⬜ pendiente `[BD]` |
| 5 | Roles personalizables (crear roles, permisos granulares) + UI de permisos por rol | ⬜ pendiente `[BD]` |
| 6 | Catálogo: import/export CSV, autogeneración de SKU/código, toma de inventario físico, historial de precios, imágenes de producto | ⬜ pendiente `[BD]` |
| 8 | Compras: órdenes de compra con estado, recepción parcial, devolución a proveedor, cuentas por pagar | ⬜ pendiente `[BD]` |
| 9 | Clientes: crédito/fiado (CxC), historial de compras, datos fiscales, lealtad/puntos | ⬜ pendiente `[BD]` |
| 10 | Apartados: fecha límite y avisos de vencidos; ligar liquidación a venta/caja | ⬜ pendiente `[BD]` |
| 11 | Configuración: logo en ticket, impuestos múltiples/exentos, asistente de primera configuración | ⬜ pendiente `[BD]` |
| 13 | Calidad/entrega: gráficas, export a Excel/PDF, actualizador automático, respaldo a la nube, más pruebas | ⬜ pendiente (mixto) |
| 14 | Hardware: ESC/POS real + cajón, config de impresora, etiquetas de código de barras, báscula, pantalla de cliente | ⬜ pendiente `[HW]` |
| 15 | Fiscal (México): CFDI 4.0 con PAC, ticket fiscal, export contable | ⬜ pendiente `[BD]`/externo |

## Detalle del Lote 3a (ya implementado en código)
- Entidad `Domain/Entities/MovimientoCaja.cs` + enum `Domain/Enums/TipoMovimientoCaja.cs`
  (Ingreso/Retiro/Gasto/Reembolso). `Efecto`/`EsEntrada` calculados (Ignorados en EF).
- Config `MovimientoCajaConfig` en `ModelConfigurations.cs`; `DbSet<MovimientoCaja> MovimientosCaja` en `AppDbContext`.
- `CajaService`: `RegistrarMovimientoAsync`, `ListarMovimientosAsync`; `ObtenerCorteAsync` ahora
  calcula `esperado = fondo + efectivo ventas + ingresos − (retiros+gastos+reembolsos)`.
  `CorteCajaDto` ganó `Ingresos` y `Salidas`; nuevo `MovimientoCajaDto`.
- UI: `Features/Pos/MovimientoCajaWindow` (registrar + estado parcial "reporte X" + lista); botón
  "Movimiento de caja" en la barra superior (`ShellViewModel.MovimientoCajaCommand`, visible con caja
  abierta); el corte muestra ingresos/retiros.

## Nota para el Lote 3b
`TipoMovimientoCaja.Reembolso` ya existe. La devolución con reembolso debería, al registrar la
devolución (`DevolucionService`), crear un `MovimientoCaja` de tipo `Reembolso` por el importe
devuelto (si hay caja abierta), para que el efectivo esperado del corte baje. Hoy `DevolucionService`
solo reintegra stock y marca el estado de la venta; **no** toca dinero.

## Patrones clave (recordatorio)
- Diálogos vía `IDialogService` (ventana + VM; evento `Cerrar(bool)` para modales con resultado).
- ViewModels resuelven servicios *scoped* con `IServiceScopeFactory` (crean un scope por operación).
- `MainWindow`/`ShellViewModel` son **transitorios** (el menú se arma por permisos en cada login).
- Comandos async de CommunityToolkit se auto-deshabilitan mientras corren (anti doble-clic); los
  guardados por `Click` (con `PasswordBox`) usan un flag `Ocupado`.
- Estilos y tokens en `Themes/`; el color de texto **no** se fija en un estilo global de `TextBlock`
  (rompía la herencia hacia el contenido de los botones) — se hereda del contenedor.
