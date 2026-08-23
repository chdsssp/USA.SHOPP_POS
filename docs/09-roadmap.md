# 09 — Roadmap

Plan por fases, orientado a tener una **caja funcional cuanto antes** y luego crecer. Cada fase deja algo utilizable y probado.

## Fase 0 — Planeación y documentación ✅ (actual)
- Decisiones de arquitectura, stack, alcance.
- Documentación inicial (este conjunto de documentos).
- **Entregable:** repositorio con `/docs` (hecho).

## Fase 1 — Cimientos técnicos
- Crear la solución y los 4 proyectos + tests (ver [Estructura](07-estructura-solucion.md)).
- Configurar Generic Host, DI, Serilog, `appsettings.json`.
- EF Core + SQLite: `AppDbContext`, primeras entidades, primera migración, `Database.Migrate()` al arrancar.
- Sistema de temas WPF (`Themes/`): tokens de color, tipografía Inter, estilos base de botones/inputs/tarjetas.
- Shell de la app: `MainWindow` con barra superior + navegación lateral + `INavigationService`.
- **Entregable:** la app abre, aplica migraciones y navega entre vistas vacías con el look definido.

## Fase 2 — Catálogo e inventario 🚧 (en progreso)
- Entidades y CRUD de Categorías, Productos y **Variantes** (talla/color, SKU, código de barras, precio, costo, stock mín.).
- Movimientos de inventario y ajustes de stock; alertas de bajo stock.
- Pantalla de Inventario (lista + búsqueda + edición) con estilos del sistema.
- Seed inicial (permisos, roles, config de tienda) y carga de catálogo.
- **Entregable:** se administran productos y existencias.

**Avance:** ya implementados en Application `CategoriaService`, `ProductoService` (alta con
variantes) e `InventarioService` (listar, ajustar stock). En WPF, la **pantalla de
Inventario** (búsqueda, filtro de bajo stock, badges de stock), el **alta de producto con
variantes** y el **ajuste de stock** funcionan contra la base real. Seed añade una categoría
"General". Pendiente: edición de producto existente y gestión de categorías desde la UI.

## Fase 3 — POS (corazón del sistema) 🚧 (en progreso)
- Búsqueda de productos (nombre/código/SKU/atributos) rápida e indexada.
- **Modo búsqueda** (teclado/lector) + **modo grid** (táctil) con conmutador.
- Carrito/ticket: cantidades (stepper táctil), descuentos de línea y global, notas.
- Sesión de caja: apertura con fondo (bloquea ventas sin caja abierta).
- Cobro (efectivo/tarjeta/mixto) con teclado numérico y cálculo de cambio.
- Registro de venta transaccional + descuento de stock.
- **Entregable:** se puede vender de principio a fin en pantalla (sin hardware aún).

**Avance:** implementado el flujo completo de venta. En Application, `CajaService`
(abrir/cerrar/estado) y se usa `RegistrarVentaService`/`BuscarProductosService`. En WPF, la
pantalla POS real: **búsqueda en vivo** (Enter/lector) y **grid táctil** con filtro por
categoría, **carrito** con stepper, **apertura de caja** (con fondo), **cobro** (efectivo/
tarjeta, teclado numérico, montos rápidos y cambio) y registro de la venta que **descuenta
stock**. Login temporal como `admin` hasta la Fase 6. Pendiente: descuentos de línea/global,
notas, pago mixto en el diálogo y selección de cliente.

## Fase 4 — Hardware
- Impresión de **ticket ESC/POS** (`ITicketPrinter`) + configuración de impresora y ticket de prueba.
- **Cajón de dinero** (`ICashDrawer`) por drawer-kick.
- Afinar captura del **lector** de código de barras en el POS.
- **Entregable:** venta real con ticket impreso y cajón que abre.

## Fase 5 — Operación de tienda 🚧 (en progreso)
- **Historial de ventas**: consulta, detalle, reimpresión, cancelación/devolución (con permisos).
- **Corte de caja**: conteo, efectivo esperado vs. contado, diferencia, reporte de cierre.
- **Clientes** y **Proveedores** (ABC + búsqueda).
- **Compras** a proveedor que reingresan stock y actualizan costo.
- **Apartados**: crear, abonar, liquidar (→ venta).
- **Entregable:** flujo completo de tienda cubierto.

**Avance:** implementados **Historial de ventas** (pantalla Ventas: filtro por fechas,
listado, detalle con líneas/pagos y reimpresión de ticket) y **Corte de caja** (resumen de
fondo, ventas y efectivo esperado vs. contado con diferencia, y cierre de sesión desde la
barra superior). Estado de caja sincronizado entre POS y shell vía mensajería.
**Clientes** y **Proveedores** ya implementados (pantallas ABC con búsqueda, alta/edición
en diálogo y baja lógica). Pendiente: compras, apartados y cancelación/devolución.

## Fase 6 — Usuarios, reportes y respaldos
- **Usuarios/roles/permisos**: login (PIN/contraseña), control de acceso en Application.
- **Reportes**: ventas por periodo, top productos, existencias, ticket promedio; KPIs en tablero.
- **Respaldos** automáticos (al cierre de caja + temporizador) y restauración.
- Configuración general (tienda, impuestos, folios, hardware).
- **Entregable:** sistema administrable y seguro, con datos protegidos.

## Fase 7 — Endurecimiento y despliegue
- Pruebas unitarias/integración clave; manejo global de errores; pulido táctil y de rendimiento.
- Instalador (Inno Setup/Velopack) + guía de instalación en sitio.
- Puesta en marcha en el All-in-One: periféricos, catálogo real, capacitación básica.
- **Entregable:** v1.0 instalada y operando en la tienda.

## Mejoras futuras (post-v1.0)
- Facturación electrónica (CFDI/SAT), si el negocio lo requiere.
- Multi-terminal / multi-sucursal con base de datos central.
- Integración con terminal bancaria (SDK) e e-commerce/Shopify.
- Programa de lealtad / promociones avanzadas.
- Tema oscuro (los tokens ya lo permiten).

---

### Sugerencia de orden mínimo para "vender lo antes posible"
Fases **1 → 2 → 3 → 4** entregan una caja que ya imprime tickets y abre el cajón. Las fases 5–6 convierten la caja en un sistema de gestión completo.
