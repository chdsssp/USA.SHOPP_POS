# 13 — Estado del proyecto y pendientes

Fecha de corte: **agosto 2026**. Resumen de lo implementado y de lo que falta.

## Resumen

El sistema está **funcional de punta a punta** y es **instalable** (ver
[docs/12](12-instalador-y-despliegue.md)). Cubre venta, inventario, caja, terceros,
compras, apartados, usuarios/roles, reportes y respaldos. Lo principal pendiente es la
**impresión ESC/POS real** (hoy simulada) y varios refinamientos.

---

## ✅ Implementado

### Fundamentos (Fase 1)
- Solución **Clean Architecture** (Domain, Application, Infrastructure, WPF) + proyectos de test.
- **EF Core + SQLite**, migraciones al iniciar, datos semilla (permisos, roles, admin, config, categoría "General").
- **WPF + MVVM** con host genérico, inyección de dependencias, logging (Serilog).
- **Sistema de diseño** estilo Shopify: fuente **Inter**, tokens de color, botones, tarjetas, **tablas estilizadas**, shell con navegación.

### Catálogo e inventario (Fase 2)
- Productos con **variantes** (talla/color, SKU, código de barras, precio, costo, stock, mínimo).
- **Alta y edición** de productos (cambiar precio/datos, agregar variantes).
- **Gestión de categorías** (crear/renombrar/eliminar; bloquea eliminar si tiene productos).
- **Ajuste de stock** con movimiento de inventario.
- **Kardex** por variante (movimientos entrada/salida con saldo acumulado).
- Listado con **búsqueda** y filtro de **bajo stock**; estados vacíos.

### Punto de venta (Fase 3)
- **Búsqueda** por nombre/código/SKU (lector) y **grid táctil** por categoría.
- **Carrito** con stepper, **descuentos por línea y global** (con permiso).
- **Selección de cliente**, apertura de caja, **cobro** (efectivo/tarjeta, teclado numérico, cambio).
- Registro de venta transaccional que **descuenta stock**; **toast** de confirmación.

### Operación de tienda (Fase 5)
- **Historial de ventas** (filtro por fecha, detalle, reimpresión, **vista previa del ticket** en pantalla), **cancelación** (reintegra stock) y **devolución parcial/total** de mercancía por línea.
- **Notas en la venta** (campo opcional en el cobro; se muestran en la vista previa y el detalle).
- **Corte de caja** (esperado vs. contado, diferencia) con respaldo automático al cerrar.
- **Historial de cortes de caja** (sesiones cerradas con esperado/contado/diferencia).
- **Clientes** y **Proveedores** (ABC con búsqueda).
- **Compras** a proveedor (reingresa stock, actualiza costo).
- **Apartados** (anticipo que reserva stock, abonos, liquidar, cancelar).

### Seguridad, reportes y respaldos (Fase 6)
- **Login** con contraseña hasheada (PBKDF2); reemplaza el auto-login.
- **Usuarios/roles** (Administrador/Encargado/Cajero) y **permisos** que filtran la navegación y acciones.
- **Mi cuenta**: el usuario cambia su propia contraseña (verifica la actual).
- **Cerrar sesión sin reiniciar**: vuelve al login manteniendo el proceso vivo.
- **Reportes**: KPIs (ventas, número, ticket promedio, bajo stock), top de productos,
  **ventas por forma de pago** y **por usuario** por rango; **exportar a CSV**.
- **Configuración** de la tienda (datos, impuestos, operación).
- **Respaldos**: manual, al cerrar caja y por temporizador; **restaurar** desde la UI (se aplica al reiniciar).

### Empaquetado y calidad (Fase 7)
- **Instalador** self-contained x64 (script de publicación + Inno Setup).
- **Pruebas de dominio** (Dinero, Venta, Descuento, Impuestos, Apartado).
- Pulido: **formularios que se ajustan a su contenido** (botón de confirmar siempre visible),
  **validación numérica**, **doble clic para editar**, **confirmaciones** en acciones destructivas,
  navegación por permisos, tablas con estilo minimalista.

---

## 🚧 Pendiente

### Prioridad alta
| Pendiente | Nota |
|---|---|
| **Impresión ESC/POS real** + apertura de cajón | Hoy son *stubs* que registran en log. Requiere la impresora física para probar (Fase 4). |
| **Migración inicial de EF** | Debe generarse una vez con `dotnet ef migrations add Inicial` (ver [docs/10](10-desarrollo.md)). |

### Prioridad media
| Pendiente | Nota |
|---|---|
| **Devolución: reembolso / afectar totales** | Hoy la devolución reintegra stock y marca el estado, pero no ajusta el importe de la venta ni el corte. |

### Prioridad baja / futuro
| Pendiente | Nota |
|---|---|
| **Apartados**: fecha límite y avisos de vencidos; ligarlos a caja/reportes | |
| **Reportes**: gráficas, exportar a Excel | CSV, forma de pago y por usuario ya están. |
| **Etiquetas / código de barras** imprimibles para productos | |
| **Facturación electrónica CFDI (SAT)** | Evaluar si el negocio lo requiere. |
| **Programa de lealtad / puntos** | |
| **Multi-terminal / multi-sucursal** (servidor central) | Fuera del alcance actual. |
| **Más pruebas** (Application/Infrastructure) y pulido de rendimiento | |

---

## Notas de operación
- **Usuario inicial:** `admin` / `admin` (cambiar en producción).
- **Datos** en `C:\ProgramData\USASHOPP POS\` (base, respaldos, logs).
- **Desarrollo:** WPF solo compila en Windows; desde Mac se usa una VM (ver
  [docs/11](11-ejecutar-en-windows-vm.md)).
