# 15 — Plan de mejoras: táctil, flujo de ropa, datos y entrega (Lotes 16–24)

Fecha: **noviembre 2026**. Sale de una revisión completa del código (build limpio, 100/100 pruebas
en verde). Continúa la numeración de [docs/14](14-roadmap-lotes.md) y sigue sus mismas reglas.

**Decisión de fondo: no reescribir.** La arquitectura (Domain/Application/Infrastructure/WPF) es
sólida; los problemas están en la capa de UI y en algunas consultas. Todo se corrige de forma
incremental y **la base de datos se conserva** (solo migraciones incrementales, una por lote `[BD]`).

Equipo objetivo: All-in-One táctil, i7-6700T, 12 GB RAM, SSD, **Windows 10**, sin internet.

---

## Reglas de trabajo (igual que docs/14)

1. Un lote a la vez. Al terminar: `dotnet build` + `dotnet test` limpios, commit y push.
2. Lotes `[BD]`: **respaldar el `pos.db` real antes de aplicar**, **una sola migración por lote**,
   probar la migración sobre una copia del `pos.db` real (ver memoria/smoke-test con BD temporal).
3. Respetar CLAUDE.md: la UI solo habla con servicios de Application, nada de colores/tamaños fijos
   (todo por tokens), permisos validados en Application, stock solo por `MovimientoInventario`.
4. Cada lote trae **pruebas** de lo que toca en Application/Domain y una **lista de verificación
   manual en pantalla táctil** (el equipo real), porque el tacto no se puede probar en la VM.
5. Al cerrar un lote: marcarlo ✅ en la tabla de abajo y en docs/14, y actualizar CLAUDE.md.

## Resumen y orden

| Lote | Tema | BD | Depende de | Prioridad |
|---|---|---|---|---|
| 16 | Correcciones críticas (reportes truncados, errores de hardware, pantalla de resultado, totales unificados, lector global) | — | — | 🔴 Alta |
| 17 | Datos confiables: ventas en espera persistentes, costo por línea, PIN, puntos configurables | `[BD]` `AddEsperaCostoPinPuntos` | 16 | 🔴 Alta |
| 18 | Base táctil: desplazamiento con el dedo, objetivos grandes, estado presionado, tokens de tipografía, escala de UI | — | — | 🔴 Alta |
| 19 | Teclados en pantalla y selectores táctiles (numérico, alfabético, buscador de clientes) | — | 18 | 🟠 Media-alta |
| 20 | POS para ropa: tarjeta por producto + matriz talla×color, fotos, búsqueda rápida, modo cajero | — | 17, 18 | 🟠 Media-alta |
| 21 | Cambios de talla y devoluciones desde el POS (escaneo del ticket) | `[BD]` `AddCambios` | 16, 20 | 🟠 Media |
| 22 | Promociones (2×1, N×$, rebaja por temporada/categoría) | `[BD]` `AddPromociones` | 16, 17 | 🟡 Media |
| 23 | Respaldos robustos sin internet (API de respaldo SQLite, verificación, USB, pre-migración) | — | — | 🟠 Media-alta |
| 24 | Entrega y calidad: actualizador offline, CI en GitHub, pruebas de ViewModels | — | — | 🟡 Media |
| 25 | Pantallas de gestión táctiles (diálogos → paneles, tablas, fechas) | — | 18, 19 | 🟢 Baja-media |

Orden sugerido de ejecución: **16 → 18 → 23 → 17 → 19 → 20 → 21 → 22 → 24 → 25**.
(16, 18 y 23 no tocan la BD y dan el mayor beneficio inmediato; 23 va antes de los lotes `[BD]`
para que el respaldo pre-migración ya exista cuando se apliquen.)

Los Lotes 14 (hardware ESC/POS real) y 15 (CFDI) de docs/14 siguen vigentes y son independientes;
el 16 deja lista la parte de "avisar si falla la impresora" para cuando llegue el 14.

---

## Lote 16 — Correcciones críticas (sin migración)

**Objetivo:** que los números sean siempre correctos y que el cajero nunca pierda información clave.

### 16.1 Reportes e historial truncados a 1000 ventas
- Problema: `VentaRepository.ListarPorFechaAsync` hace `.Take(1000)`
  ([Repositories.cs:29](../src/Usashopp.Pos.Infrastructure/Persistence/Repositories/Repositories.cs)).
  `ReportesService` (periodo y comparativo) y `ConsultarVentasService` lo usan → totales falsos
  en periodos con más de 1000 ventas, sin aviso.
- Tareas:
  - Nuevo método de repositorio para reportes que **proyecte solo las columnas necesarias**
    (fecha, estado, total, usuario, líneas: variante/cantidad/importe/descuento, pagos: método/monto)
    con `AsNoTracking()` y **sin tope**. Nota técnica: EF Core + SQLite no traduce `Sum` sobre
    `decimal` (se guarda como TEXT), así que la agregación se hace en memoria **sobre la proyección
    ligera**, no sobre el grafo de entidades. Es suficiente para años de datos en este equipo.
  - Historial de ventas (`ConsultarVentasService`/`VentasView`): **paginación** (p. ej. 100 por
    página, "Cargar más") en lugar del tope silencioso, y mostrar "N ventas en el periodo".
  - Revisar los demás `Take(500)` (compras, apartados, ventas por cliente) y decidir paginación
    o un aviso visible "mostrando las 500 más recientes".
- Pruebas: `ReportesServiceTests` con >1000 ventas sembradas que verifique el total exacto.

### 16.2 Errores de impresora/cajón silenciados
- Problema: `RegistrarVentaService.IntentarAsync` tiene `catch { }` vacío.
- Tareas:
  - Registrar la excepción con `ILogger` (Application solo conoce la abstracción).
  - `ResultadoVentaDto` gana `IReadOnlyList<string> Advertencias` (p. ej. "No se pudo imprimir el
    ticket", "No se abrió el cajón"). La venta sigue registrada.
  - La pantalla de resultado (16.3) muestra la advertencia con botón **Reintentar impresión**.
  - Aplicar lo mismo en los demás lugares que imprimen (reimpresión, corte, apartados).
- Pruebas: impresora falsa que lanza excepción → venta OK + advertencia presente.

### 16.3 Pantalla de resultado de la venta (cambio visible)
- Problema: el cambio solo aparece en un toast de 3 s.
- Tareas:
  - Panel/overlay a pantalla completa tras cobrar: **Cambio $X** en tamaño enorme (token
    `Font.Hero`, ~64), total, forma(s) de pago, folio.
  - Botones grandes: **Nueva venta** (primario, también Enter/Espacio), **Reimprimir ticket**,
    **Ver ticket**. No se cierra solo; también se cierra al escanear el siguiente producto
    (el código escaneado se agrega a la nueva venta).
  - Advertencias de hardware (16.2) visibles aquí.
- Verificación táctil: el cajero ve el cambio sin buscarlo; un toque inicia la siguiente venta.

### 16.4 Una sola fuente de verdad para los totales
- Problema: `PosViewModel.RecalcularTotales`/`CalcularDescuentoGlobal` duplican la lógica del
  dominio (`Venta`, `Descuento`, `DetalleVenta.Importe`).
- Tareas:
  - Servicio puro en Application, p. ej. `CalculadoraTotales.Calcular(lineas, descuentoGlobal)` que
    use los mismos tipos/reglas del dominio (redondeo, tope del descuento) y devuelva subtotal,
    descuentos por línea, descuento global, impuestos (desglose informativo) y total.
  - `PosViewModel` y `RegistrarVentaService` usan esa misma calculadora.
  - Base para las promociones del Lote 22.
- Pruebas: casos de redondeo, descuento % y monto, descuento global mayor que el subtotal; el
  total del VM == total de la `Venta` registrada.

### 16.5 Lector de código de barras en toda la ventana
- Problema: el escaneo solo funciona si el foco está en `BusquedaBox`.
- Tareas:
  - Detector de ráfaga de teclas (caracteres con <30–50 ms entre sí + Enter) a nivel de
    `PosView`/`MainWindow` (`PreviewTextInput`), configurable. Si se detecta una ráfaga y el foco
    no está en un campo de texto, se envía a `BuscarCommand` como código.
  - Sonido/vibración visual de confirmación (borde verde breve) y de error (rojo + mensaje
    "Código no encontrado: …") sin ventana modal.
- Verificación: escanear con el foco en el carrito, en notas vacías y tras la pantalla de resultado.

### 16.6 Stock al agregar al carrito
- Hoy `LineaCarrito.StockDisponible` se guarda pero no se usa hasta el cobro.
- Tareas: avisar en la línea (badge ámbar "Solo quedan N") al superar el stock, respetando
  `PermitirVentaStockNegativo`; el bloqueo definitivo sigue en `RegistrarVentaService`.

**Criterios de aceptación del Lote 16:** reportes exactos con >1000 ventas; fallo de impresora
visible y reintentable; cambio visible hasta que el cajero continúe; mismos totales en pantalla y
en la venta guardada; escaneo funciona sin importar el foco.

---

## Lote 17 — Datos confiables `[BD]` (migración `AddEsperaCostoPinPuntos`)

**Objetivo:** que nada importante viva solo en memoria y que la utilidad histórica no cambie.

### 17.1 Ventas en espera persistentes
- Problema: `VentasEnEsperaStore` es memoria → se pierden al cerrar/caer la app o al ir la luz.
- Tareas:
  - Entidades `VentaEnEspera` + `LineaVentaEnEspera` (Domain), tablas nuevas, servicio
    `VentasEnEsperaService` (suspender, listar, recuperar=eliminar, descartar).
  - Guardan usuario y sesión de caja; al cerrar caja, avisar si quedan ventas en espera.
  - Al recuperar, **revalidar precio y stock actuales** y avisar si cambiaron.
  - Opcional: auto-guardado del carrito activo (borrador) para recuperarlo tras un cierre inesperado.
- Pruebas: suspender → reiniciar scope/contexto → sigue ahí; recuperar con precio cambiado avisa.

### 17.2 Costo congelado en cada línea de venta
- Problema: la utilidad se calcula con el costo **actual** de la variante.
- Tareas:
  - `DetalleVenta.CostoUnitario` (`Dinero`). Se llena al registrar la venta.
  - **Backfill** en la migración: ventas existentes toman el costo actual de la variante (es la
    mejor aproximación disponible; documentarlo).
  - `ReportesService` usa `CostoUnitario` de la línea. Igual en apartados liquidados.
- Pruebas: cambiar el costo después de vender no altera la utilidad del periodo.

### 17.3 PIN de usuario (para supervisor y cambio rápido de cajero)
- `Usuario.PinHash` (nullable, hasheado con el mismo `IPasswordHasher`), 4–6 dígitos, único por
  usuario activo. Se captura en el editor de usuario y en "Mi cuenta".
- `AutenticacionService.AutorizarAsync` acepta PIN además de contraseña (sigue auditando).
- Base para el "modo cajero" del Lote 20.

### 17.4 Regla de puntos de lealtad configurable
- `ConfiguracionTienda.PesosPorPunto` (default 10) y `LealtadActiva` (bool).
- `RegistrarVentaService` deja de tener el "/10" fijo. Campo en Configuración.

**Criterios:** migración aplicada sobre copia del `pos.db` real sin pérdida; ventas en espera
sobreviven a un reinicio; utilidad estable; supervisor autoriza con PIN.

---

## Lote 18 — Base táctil (sin migración)

**Objetivo:** que toda la app se use cómodamente con el dedo. Es el lote con más impacto en el
uso diario.

### 18.1 Desplazamiento con el dedo
- Problema: ningún `ScrollViewer` tiene `PanningMode`; arrastrar con el dedo no desplaza listas.
- Tareas:
  - Estilo implícito global de `ScrollViewer` en `Themes/Controls.xaml` con
    `PanningMode="VerticalFirst"`, `PanningDeceleration` y `PanningRatio` ajustados; los horizontales
    (chips de categoría) con `HorizontalOnly`.
  - Manejar `ManipulationBoundaryFeedback` (evita que la ventana completa "rebote" al llegar al
    final de una lista).
  - `DataGrid`: habilitar panning en su `ScrollViewer` interno (estilo/plantilla).
  - Barras de desplazamiento más anchas (≥ 12–16 px) para quien prefiera arrastrarlas.

### 18.2 Objetivos táctiles
- Tokens nuevos: `TouchTarget.Min = 48`, `TouchTarget.Comodo = 56`, `TouchTarget.Grande = 64`,
  `Espacio.Tactil = 8` (separación mínima entre botones).
- Corregir en el carrito del POS
  ([PosView.xaml](../src/Usashopp.Pos.Wpf/Features/Pos/PosView.xaml)): ✕ quitar (hoy sin
  tamaño), –/+ (hoy 34×32), % (hoy 28×26), "Abrir caja" (34) → todos ≥ 48.
- Revisar la ✕ de pagos en `CobroWindow`, botones dentro de filas de tablas, chips, segmentado
  Búsqueda/Grid, botones "sutiles" de texto (Descuento a la venta, Autorizar).
- Alternativa en el carrito: **deslizar la línea a la izquierda para quitar** y tocar la línea
  para abrir un panel de edición (cantidad, precio, descuento) con botones grandes, en vez de
  meter 4 controles pequeños en cada línea.

### 18.3 Estados visuales para tacto
- Agregar trigger `IsPressed` (fondo más oscuro / escala 0.97) a `Button.Primary`,
  `Button.Secondary`, `Button.Subtle`, `Tecla`, tarjetas y filas.
- Quitar/neutralizar efectos dependientes de hover en táctil: animación de elevación y
  `DropShadowEffect` en `ProductCard`, sombra del segmentado y del toast (además, las sombras
  son caras en render por software). Mantener hover solo cuando el origen es mouse
  (`Stylus.IsPressAndHoldEnabled=False`, y desactivar el resaltado tras toque).
- Desactivar el "presionar y mantener = clic derecho" de Windows en controles del POS.

### 18.4 Tipografía y colores solo por tokens
- Problema: 214 `FontSize` y 34 colores fijos en XAML (contra la regla de CLAUDE.md).
- Tareas:
  - Ampliar la escala tipográfica: `Font.Caption` (12), `Font.Small` (13), `Font.Body` (15),
    `Font.Subtitle` (17), `Font.Title` (22), `Font.Display` (30), `Font.Hero` (64). Tamaño base
    de cuerpo 15 para lectura a distancia de brazo en pantalla táctil.
  - Reemplazar todos los valores fijos (`12.5`, `13`, `11`, `White`, `#000000`, etc.) por tokens.
    Hacerlo por feature para revisar visualmente cada pantalla.
  - Agregar una prueba/script de verificación (p. ej. `grep` en CI, Lote 24) que falle si aparece
    un `FontSize="número"` o color hex en `Features/`.

### 18.5 Escala de UI por terminal
- Ajuste "Tamaño de la interfaz" (100 % / 115 % / 125 %) aplicado con `LayoutTransform` en el
  contenido de `MainWindow`. Se guarda **por equipo** en un archivo de usuario
  (`%ProgramData%/USASHOPP POS/terminal.json`), no en la BD (sin migración).

### 18.6 Tablas legibles al tacto
- Estilo global de `DataGrid`: `RowHeight` 52, encabezados 48, sin selección de celda (fila
  completa), sin edición en línea salvo donde se necesite.
- **Reemplazar el doble clic** por botón/ícono "Editar" visible en cada fila (o tocar la fila
  para abrir el detalle) en: Categorías, Clientes, Inventario, Selector de producto, Proveedores,
  Roles y Usuarios.

**Verificación táctil (equipo real):** desplazar productos, carrito, historial y reportes con
el dedo; tocar 20 veces seguidas +/– sin fallar; ninguna fila de tabla requiere doble toque.

---

## Lote 19 — Teclados en pantalla y selectores táctiles (sin migración)

**Objetivo:** no depender del teclado táctil de Windows (aparece de forma poco confiable en WPF).

### 19.1 Teclado numérico reutilizable
- Extraer el teclado de `CobroWindow` a un control `TecladoNumerico` (Common/Controles).
- Comportamiento adjunto `InputHelpers.TecladoNumerico="True"`: al tocar un campo numérico
  (cantidad, precio, descuento, fondo de caja, conteo de denominaciones, ajuste de stock, abonos)
  se abre un popup con el teclado junto al campo, con "Aceptar" y "Borrar".
- Solo con entrada táctil/stylus; con mouse+teclado físico no se abre (configurable).

### 19.2 Teclado alfabético propio
- Control `TecladoAlfabetico` QWERTY español (ñ, acentos con pulsación larga o tecla ´, @, .,
  espacio, borrar, mayúsculas, "123").
- Se usa en: búsqueda del POS, notas, login (usuario/contraseña), buscador de clientes, editores
  rápidos (cliente al vuelo). Panel acoplado abajo que **no tapa** el campo activo.
- Opción en configuración del terminal: "Usar teclado en pantalla: Siempre / Solo táctil / Nunca".

### 19.3 Buscador de clientes táctil
- Problema: `ComboBox` con todos los clientes en el POS.
- Tareas: botón "Cliente: (ninguno)" → panel con buscador (nombre/teléfono/RFC) por servicio
  (`ClienteService.BuscarAsync(texto, limite)`), resultados grandes, "+ Nuevo cliente" y
  "Quitar cliente". Mostrar crédito disponible / saldo a favor / puntos del elegido.
- Dejar de cargar todos los clientes al iniciar el POS.

### 19.4 Otros `ComboBox`
- Hay ~20 en la app. Si tienen ≤ 5 opciones (tipo de descuento, tipo de movimiento de caja,
  método de reembolso) → **botones segmentados**. Si tienen muchas → estilo táctil global para
  `ComboBox` (items de 48 px, popup más grande) o buscador como 19.3.
- `DatePicker` (9 usos): estilo táctil con calendario grande + accesos rápidos "Hoy, Ayer,
  Esta semana, Este mes, Mes pasado" en Reportes, Ventas, Kardex y Auditoría.

**Verificación:** completar una venta con cliente, nota, cantidad editada y pago mixto **sin
tocar el teclado físico**.

---

## Lote 20 — POS para tienda de ropa (sin migración)

**Objetivo:** encontrar y vender una prenda en talla/color en 2 toques.

### 20.1 Tarjeta por producto + matriz talla × color
- Problema: el grid muestra una tarjeta por **variante** con tope de 200
  (`BuscarProductosService.ParaGridAsync`).
- Tareas:
  - Nuevo `ParaGridProductosAsync(categoriaId, pagina)` que devuelva **productos** (nombre,
    imagen, rango de precio, stock total, nº de variantes).
  - Al tocar un producto con varias variantes → panel **matriz talla × color**: filas = colores
    (con muestra de color si se puede derivar), columnas = tallas en orden lógico
    (XS, S, M, L, XL / numéricas ascendentes), cada celda muestra stock; agotado en gris (tocable
    solo si se permite stock negativo), bajo stock en ámbar. Un toque agrega y cierra; botón
    "Agregar y seguir" para varias.
  - Producto con una sola variante → se agrega directo.
  - Orden configurable de tallas (lista en Configuración o convención fija documentada).
  - Grid con paginación / "Cargar más" y virtualización (no hay `VirtualizingWrapPanel` nativo:
    usar un panel virtualizado de terceros o filas de `UniformGrid` dentro de un `ListBox`
    virtualizado).

### 20.2 Fotos en el POS
- Las imágenes existen (Lote 6, `IAlmacenImagenes`) pero el POS muestra iniciales.
- Mostrar miniatura en tarjetas, resultados de búsqueda, carrito y matriz. Cargar con
  `DecodePixelWidth` (≈ 200 px) y caché en memoria para no saturar RAM; iniciales como respaldo.
- Opcional: generar miniaturas al guardar la imagen (archivo `_thumb`).

### 20.3 Búsqueda rápida
- Problema: `VarianteRepository.BuscarAsync` carga todas las variantes activas en cada búsqueda.
- Tareas: `CatalogoCache` (singleton en Application/Infrastructure) con el índice de búsqueda
  normalizado (sin acentos, minúsculas, tokens) en memoria; se invalida al guardar
  producto/variante/stock (mensaje o versión). Búsqueda por código de barras sigue yendo directo
  a la BD (índice).
- Meta: < 50 ms por búsqueda con 10 000 variantes en el equipo objetivo.

### 20.4 Accesos fijos en el POS
- Botones grandes siempre visibles: **Última venta** (ver/reimprimir), **Cambio de talla**
  (Lote 21), **Devolución** (Lote 21), **Movimiento de caja**.
- Atajos de teclado actuales (F2–F9, F11) se mantienen y se muestran en una ayuda (F1).

### 20.5 Modo cajero
- Opción por usuario/rol: al iniciar sesión entra directo al POS a pantalla completa, sin la
  barra lateral (220 px más de espacio).
- **Cambio rápido de usuario con PIN** (Lote 17.3) sin cerrar la app ni perder el carrito.
- Botón "Más" para acceder a lo que el rol permita (ventas del día, corte).
- Bloqueo manual de pantalla con PIN (no automático, respetando la decisión del Lote 4).

**Verificación:** vender "Playera básica, negra, M" en ≤ 2 toques desde el grid; búsqueda
instantánea; fotos visibles sin consumo excesivo de memoria.

---

## Lote 21 — Cambios de talla y devoluciones desde el POS `[BD]` (migración `AddCambios`)

**Objetivo:** el cambio de talla, la operación más común después de vender, en un solo flujo.

### Tareas
- **Folio como código de barras en el ticket** (Code128 ya existe en `Features/Etiquetas`):
  impreso en ESC/POS (Lote 14) y en la vista previa.
- Botón **Cambio de talla** en el POS: escanear/teclear el folio → ver líneas de la venta → elegir
  qué prenda regresa → elegir la nueva (matriz talla×color del Lote 20) → calcular diferencia:
  - Diferencia a favor de la tienda → cobrar (flujo de cobro normal).
  - Diferencia a favor del cliente → efectivo de caja o nota de crédito (reutilizar 3b).
  - Sin diferencia → solo movimientos de inventario.
- `CambioService` en Application: **una sola transacción** = devolución de líneas (reintegra
  stock) + nueva venta (descuenta stock) ligadas.
- Migración: `Venta.VentaOrigenId` (nullable, FK a la venta original) y tipo/marca de "cambio"
  para que reportes no lo cuenten como venta nueva completa ni como devolución pura.
- Botón **Devolución** en el POS que abre el flujo existente (`DevolucionService`) por escaneo.
- Reglas configurables: días máximos para cambio, permiso requerido, ¿se permite cambio de
  artículo con descuento/promoción?
- Reportes: columna/filtro de cambios; el corte refleja solo la diferencia en efectivo.
- Pruebas: cambio sin diferencia, con cobro, con reembolso; stock correcto; corte correcto.

---

## Lote 22 — Promociones `[BD]` (migración `AddPromociones`)

**Objetivo:** las promociones típicas de ropa sin descuentos manuales.

### Tareas
- Entidad `Promocion`: nombre, tipo, valor(es), vigencia (inicio/fin), días de la semana
  opcionales, alcance (todo / categoría / producto / variante), activa, prioridad, ¿acumulable?
- Tipos iniciales:
  - **% o monto de descuento** por alcance (rebaja de temporada, liquidación).
  - **N×M** (2×1, 3×2) — el más barato gratis.
  - **N por $X** (3 × $299).
  - **Precio especial** fijo por variante en vigencia.
- **Motor de promociones en Domain** (puro, sin IO): recibe líneas del carrito + promociones
  vigentes y devuelve descuentos por línea con la promoción aplicada. Lo usa la
  `CalculadoraTotales` (Lote 16.4).
- Reglas de precedencia: promoción automática vs. descuento manual (el manual reemplaza o se
  suma según configuración; nunca dos promociones no acumulables en la misma línea).
- `DetalleVenta.PromocionId` + descripción congelada para ticket y reportes.
- UI: sección "Promociones" (permiso nuevo `promociones.gestionar` con el patrón
  `SincronizarPermisosAsync`), etiqueta visible en el carrito ("2×1 aplicado") y en el ticket.
- Reporte: ventas y descuento por promoción.
- Pruebas: cada tipo, vigencias, combinaciones, redondeos, devolución de una prenda de un 2×1
  (prorrateo del descuento).

---

## Lote 23 — Respaldos robustos sin internet (sin migración)

**Objetivo:** que un fallo de disco o una mala migración no cueste datos.

### Tareas
- Reemplazar `File.Copy` en `SqliteBackupService` por la **API de respaldo en línea de SQLite**
  (`SqliteConnection.BackupDatabase`) → copia consistente aunque la app esté escribiendo.
- **Verificar cada respaldo**: abrirlo y correr `PRAGMA integrity_check`; registrar resultado;
  marcar como inválido si falla.
- **Respaldo automático antes de migrar** en `DatabaseInitializer` cuando haya migraciones
  pendientes (nombre `pos_premigracion_<migración>_<fecha>.db`), y abortar el arranque con
  mensaje claro si la migración falla, ofreciendo restaurar.
- **Destino externo (USB/disco)**: configurable en la UI (también la carpeta de nube, pendiente
  del Lote 13). Si el destino no está conectado: aviso no bloqueante en la barra superior
  ("Respaldo externo pendiente desde hace N días").
- Política de retención: diarios 14, semanales 8, mensuales 12 (en lugar de "últimos N").
- Pantalla de respaldos: lista con fecha, tamaño, verificado ✓/✗, origen (manual/corte/auto/
  pre-migración) y "Restaurar" con confirmación (ya existe la restauración diferida).
- `PRAGMA busy_timeout` y `synchronous=NORMAL` con WAL documentados en `DatabaseInitializer`.
- Pruebas: respaldo mientras se escribe; respaldo corrupto detectado; pre-migración creado.

---

## Lote 24 — Entrega y calidad (sin migración)

### 24.1 Actualizador que funciona sin internet
- Integrar **Velopack** (o equivalente): paquetes delta, actualización desde una **carpeta local
  o USB** además de una URL (GitHub Releases) cuando haya internet.
- Al actualizar: respaldo previo automático (Lote 23) y nota de "Qué hay de nuevo".
- Mantener el instalador de Inno Setup para la primera instalación o migrar a Velopack por
  completo (decidir al implementar).

### 24.2 CI en GitHub Actions
- Workflow en `windows-latest`: restore, build, test, verificación de tokens (18.4), publicación
  self-contained y artefacto del instalador en cada tag `v*`.
- Versión tomada del tag (fin de los commits manuales "subir versión a 1.1.x").

### 24.3 Pruebas de ViewModels
- Pruebas de `PosViewModel` y `CobroViewModel` con servicios falsos: agregar, quitar,
  suspender/recuperar, descuentos con/sin permiso, cobro mixto, pantalla de resultado.
- Requiere desacoplar `IDialogService` y `IServiceScopeFactory` en las pruebas (ya son
  interfaces; agregar fakes en el proyecto de pruebas).
- Opcional: humo de UI con FlaUI (abrir app con BD temporal, vender, cerrar caja).

### 24.4 Telemetría local
- Registro de tiempos (búsqueda, cobro, arranque) en el log para detectar lentitud en el equipo
  real; visor simple en Configuración → Diagnóstico.

---

## Lote 25 — Pantallas de gestión táctiles (sin migración)

**Objetivo:** que inventario, compras, clientes, etc. también sean cómodos con el dedo.

- Las 32 ventanas modales → **paneles laterales/pantallas** dentro del shell con "Atrás" y
  "Guardar" grandes y fijos abajo (las ventanas de confirmación pequeñas pueden quedarse).
  Hacerlo por módulo, empezando por los más usados: Producto (editor), Ajuste de stock,
  Cliente, Movimiento de caja, Corte.
- Formularios en una columna con etiquetas arriba, campos de 48 px, teclados de 19.x.
- Barra lateral: íconos + texto de 56 px, colapsable a íconos.
- Mensajes (`MessageBox` y `IDialogService.Mensaje`) con diseño propio, botones grandes y
  sin cierre accidental.
- Revisión final con el checklist táctil completo en el equipo real.

---

## Si en el futuro se decide cambiar de tecnología de UI

No es parte de este plan, pero para no cerrar la puerta:
- **Ruta recomendada:** nueva capa de UI (WinUI 3, compatible con Windows 10 1809+; o Avalonia)
  que **reutilice `Domain`, `Application` e `Infrastructure` sin cambios**. La BD se conserva
  automáticamente porque sigue el mismo `AppDbContext` y la misma cadena de migraciones.
- Programa totalmente nuevo en otra tecnología: debe respetar las convenciones de
  almacenamiento actuales (Guid como TEXT, `Dinero`/decimal como TEXT, fechas en UTC, enums como
  int, converters de `Sku`/`CodigoBarras`/`Descuento`) y conservar `__EFMigrationsHistory`.
  Es viable pero con más riesgo para los datos reales.
- Hacer los Lotes 16–18 primero en cualquier caso: lo aprendido (componentes táctiles, matriz de
  tallas, calculadora de totales) se traslada a cualquier UI.

---

## Checklist maestro (todo lo detectado en la revisión)

| # | Detalle | Lote |
|---|---|---|
| 1 | Reportes/historial truncados a 1000 ventas | 16.1 |
| 2 | Otros topes silenciosos (500 en compras/apartados/cliente) | 16.1 |
| 3 | Ventas en espera solo en memoria | 17.1 |
| 4 | Errores de impresora/cajón silenciados (`catch {}`) | 16.2 |
| 5 | Cambio visible solo 3 s en un toast | 16.3 |
| 6 | Totales duplicados en ViewModel y dominio | 16.4 |
| 7 | Lector solo funciona con foco en la búsqueda | 16.5 |
| 8 | Sin aviso de stock al agregar | 16.6 |
| 9 | Utilidad con costo actual, no histórico | 17.2 |
| 10 | Autorización de supervisor solo con contraseña completa | 17.3 |
| 11 | Puntos de lealtad "1 por $10" fijos en código | 17.4 |
| 12 | Sin `PanningMode`: el dedo no desplaza listas | 18.1 |
| 13 | Botones pequeños en carrito (✕, –/+, %, Abrir caja) | 18.2 |
| 14 | Sin estado presionado; hover pegado tras toque; sombras/animaciones de hover | 18.3 |
| 15 | 214 tamaños de letra y 34 colores fijos | 18.4 |
| 16 | Sin escala de UI para el equipo táctil | 18.5 |
| 17 | Filas de tabla de 40–42 px; doble clic para editar en 7 vistas | 18.6 |
| 18 | Dependencia del teclado táctil de Windows | 19.1, 19.2 |
| 19 | `ComboBox` de todos los clientes en el POS | 19.3 |
| 20 | ~20 `ComboBox` y 9 `DatePicker` no táctiles | 19.4 |
| 21 | Grid por variante con tope de 200; sin selector talla×color | 20.1 |
| 22 | Fotos de producto no usadas en el POS | 20.2 |
| 23 | Búsqueda carga todas las variantes en cada consulta | 20.3 |
| 24 | Sin acceso rápido a última venta/reimpresión | 20.4 |
| 25 | Barra lateral ocupa espacio del cajero; sin cambio rápido de usuario | 20.5 |
| 26 | Cambio de talla requiere ir a Ventas + devolución parcial | 21 |
| 27 | Sin promociones (2×1, N×$, temporada) | 22 |
| 28 | Respaldo por copia de archivo, sin verificación | 23 |
| 29 | Sin respaldo antes de migrar | 23 |
| 30 | Respaldo externo/nube sin UI; nube inútil sin internet | 23 |
| 31 | Actualizaciones por reinstalación manual | 24.1 |
| 32 | Sin CI; versión subida a mano | 24.2 |
| 33 | Sin pruebas de ViewModels del POS/cobro | 24.3 |
| 34 | 32 ventanas modales en gestión | 25 |
