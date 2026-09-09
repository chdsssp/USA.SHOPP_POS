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

## Acción inmediata — ✅ RESUELTA (commit `18dbe58`, 2026-09-07)
Las migraciones de EF ya existen en `src/Usashopp.Pos.Infrastructure/Persistence/Migrations/`:
- **`20260823091634_Inicial`** — baseline del esquema.
- **`20260907022055_AddMovimientosCaja`** — crea la tabla `MovimientosCaja` (Lote 3a). Se hizo
  **incremental** (no se regeneró la `Inicial`) porque la `Inicial` es previa al Lote 3a y el
  `pos.db` real ya la tenía aplicada con datos; regenerar habría destruido esos datos.

`MovimientosCaja` quedó con `Id`, `SesionCajaId` (FK cascade a `SesionesCaja`), `Tipo` (int),
`Monto` (decimal 18,2), `Concepto` (≤200, nullable), `UsuarioId`, `Fecha`, `CreadoEn`, `ActualizadoEn`,
más índices en `Fecha` y `SesionCajaId`. Verificado: build limpio y cadena válida sobre base nueva
(`pos-design.db`) y sobre el `pos.db` real (`AddMovimientosCaja` quedaba *Pending*; la app la aplica
al arrancar vía `DatabaseInitializer.MigrateAsync`, sin perder datos).

`Infrastructure/Persistence/AppDbContextFactory.cs` (`IDesignTimeDbContextFactory`) usa una BD
desechable `pos-design.db` para que EF trabaje sin arrancar WPF (está en `.gitignore`).

**Para el siguiente lote `[BD]`**: respaldar el `pos.db` real antes de aplicar; generar **una
migración por lote**; validar en Windows entre cada uno.

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
| **3a** | **Movimientos de caja (ingresos/retiros/gastos) + reporte X + corte con ingresos/salidas** | ✅ hecho (migración `AddMovimientosCaja`, commit `18dbe58`) |
| 3b | Devolución con reembolso (efectivo en caja o nota de crédito), conteo por denominaciones en el corte | ✅ hecho (migración `AddNotasCredito`) — **pendiente:** canjear la nota de crédito como forma de pago en el POS (ver Lote 9) |
| 4 | Auditoría (bitácora), autorización de supervisor (PIN override), bloqueo por inactividad, política de contraseñas | ✅ **hecho** (según alcance acordado): auditoría (`AddAuditoria`) + autorización de supervisor para descuento/edición de precio. Política de contraseñas: se mantuvo mínimo 4. Bloqueo por inactividad: **omitido** por decisión |
| 5 | Roles personalizables (crear roles, permisos granulares) + UI de permisos por rol | ✅ hecho (sin migración; el esquema roles↔permisos ya existía) |
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

## Lote 3b (implementado)
- **Reembolso al devolver** (`DevolucionService.EjecutarAsync`): calcula el **neto realmente
  pagado** por las líneas devueltas (respeta descuentos de línea y global) y reembolsa según
  `MetodoReembolso`:
  - **Efectivo**: crea un `MovimientoCaja` de tipo `Reembolso`; exige caja abierta (bloquea si no).
  - **Nota de crédito**: emite una `NotaCredito` (saldo a favor del cliente) sin tocar la caja;
    exige un cliente (se precarga el de la venta; se puede elegir otro en el diálogo).
- **Nota de crédito**: entidad `NotaCredito` (+ enum `EstadoNotaCredito`), migración `AddNotasCredito`,
  `NotaCreditoService` (saldo/listado por cliente). **Pendiente:** *canjearla* como forma de pago en
  el POS (toca el flujo de cobro; encaja con el Lote 9 de crédito de clientes).
- **Conteo por denominaciones** en el corte: la UI suma billetes/monedas MXN y alimenta el efectivo
  contado (solo cálculo; el desglose no se persiste).
- **Corregido de paso**: `CajaService.ListarCortesAsync` ahora incluye los movimientos de caja en el
  efectivo esperado del historial (antes solo `fondo + efectivo`, a diferencia del corte en vivo).
- Tests en `Usashopp.Pos.Application.Tests/DevolucionServiceTests.cs` (neto con descuentos, bloqueo
  sin caja, reembolso en efectivo, nota de crédito con/sin cliente, estados parcial/total).

## Lote 4 — auditoría (implementada; resto pendiente)
- **Bitácora**: entidad `RegistroAuditoria`, migración `AddAuditoria`, interfaz `IAuditoria` +
  `AuditoriaService` (escribe con el usuario de `ICurrentUser`, best-effort; y consulta con filtros).
  Nuevo permiso `auditoria.ver` + sección "Bitácora" en el shell (visor con filtros fecha/texto).
- **Compat. de permisos**: `DatabaseInitializer.SincronizarPermisosAsync` (idempotente) da de alta
  permisos nuevos del catálogo en bases ya existentes y se los asigna al rol Administrador. **Patrón
  a reutilizar** al agregar permisos en lotes futuros.
- **Acciones auditadas hoy**: inicio de sesión (éxito/fallo), cancelación y devolución de venta,
  apertura/cierre de caja y movimientos de caja, altas/ediciones/bajas de usuario y cambio de
  contraseña propia. (Login se registra directo porque `ICurrentUser` aún no existe al autenticar.)
- Tests: `AuditoriaServiceTests` (sello de usuario, best-effort, filtro de texto/orden).

### Autorización de supervisor (override) — implementada
- `AutenticacionService.AutorizarAsync(login, contraseña, permisoRequerido, accion)`: valida las
  credenciales de otro usuario y que tenga el permiso; devuelve su nombre y lo registra en bitácora
  ("Autorización de supervisor"). Usa la contraseña existente (no hay PIN aparte; sin migración).
- Diálogo `AutorizacionSupervisorWindow` + `MostrarAutorizacionSupervisor(permiso, accion)` en
  `IDialogService`.
- **POS**: si el cajero no tiene `descuentos.aplicar`, aparece "Autorizar descuento (supervisor)".
  Al autorizar, se habilitan descuentos y edición de precio **solo para la venta en curso**
  (`PosViewModel.DescuentoAutorizado`, se reinicia al cobrar). Tests: `AutenticacionServiceTests`.
- **Decisiones de alcance**: override solo para descuento/edición de precio (no cancelar/devolver);
  política de contraseñas se mantuvo en mínimo 4; bloqueo por inactividad omitido.

## Lote 5 — roles personalizables (implementado)
- **Sin migración**: el esquema roles↔permisos (many-to-many) ya existía desde `Inicial`.
- `RolService` (crear/editar/eliminar roles y asignar permisos por clave). Guardas: nombre único y
  ≥3 caracteres; el rol **Administrador** es de sistema (no se edita ni elimina — además el seed le
  re-asigna todos los permisos en cada arranque); no se elimina un rol con usuarios asignados. Audita
  altas/ediciones/bajas.
- `Permisos.Descripciones` / `Permisos.Etiqueta(clave)`: etiquetas legibles del catálogo para la UI.
- **UI**: sección "Roles" (gated por `usuarios.gestionar`) con lista (nombre, #permisos, #usuarios,
  sistema) + editor `RolEditorWindow` (nombre + casillas de permisos).
- Tests: `RolServiceTests` (validaciones, protección de Administrador, no borrar con usuarios).

## Patrones clave (recordatorio)
- Diálogos vía `IDialogService` (ventana + VM; evento `Cerrar(bool)` para modales con resultado).
- ViewModels resuelven servicios *scoped* con `IServiceScopeFactory` (crean un scope por operación).
- `MainWindow`/`ShellViewModel` son **transitorios** (el menú se arma por permisos en cada login).
- Comandos async de CommunityToolkit se auto-deshabilitan mientras corren (anti doble-clic); los
  guardados por `Click` (con `PasswordBox`) usan un flag `Ocupado`.
- Estilos y tokens en `Themes/`; el color de texto **no** se fija en un estilo global de `TextBlock`
  (rompía la herencia hacia el contenido de los botones) — se hereda del contenedor.
