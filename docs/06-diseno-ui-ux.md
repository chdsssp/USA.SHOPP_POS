# 06 — Diseño UI/UX

Sistema de diseño **minimalista, limpio y moderno** en **tema claro**, inspirado en el panel de administración de Shopify (tipografía, botones, uso del espacio). Pensado para **mouse + teclado** y **pantalla táctil** por igual.

## Principios visuales

1. **Claridad sobre densidad.** Mucho espacio en blanco, agrupación en tarjetas, jerarquía tipográfica marcada.
2. **Color con intención.** Base neutra (grises/blancos); el color se reserva para acciones y estados (éxito, alerta, error).
3. **Un botón primario por vista.** La acción principal (Cobrar, Guardar) es evidente; el resto son secundarias/sutiles.
4. **Táctil primero en el POS.** Objetivos ≥ 44 px (meta **48 px**), separación generosa, sin hover como única pista, sin gestos ocultos.
5. **Consistencia.** Todo se construye con los mismos tokens y componentes reutilizables.

## Tipografía

Shopify usa **Inter** (base de su sistema Polaris). La adoptamos como fuente de la app.

- **Fuente:** **Inter** (open source, gratuita). Se **empaqueta con la app** (no depender de que esté instalada). Fallback: `Segoe UI` (nativa de Windows 10) → `sans-serif`.
- **Escala tipográfica** (tamaños en px @96dpi; en el POS se suben un escalón por legibilidad táctil):

| Rol | Tamaño | Peso | Uso |
|---|---|---|---|
| Display | 28 | 600 | Totales grandes, importe a cobrar |
| Título | 20 | 600 | Encabezados de pantalla / diálogos |
| Subtítulo | 16 | 600 | Encabezados de sección / tarjetas |
| Cuerpo | 14 | 400 | Texto general |
| Cuerpo fuerte | 14 | 600 | Etiquetas, nombres de producto |
| Small | 12 | 400/500 | Metadatos, ayudas, badges |

- Interlineado cómodo (1.4–1.5), altura de línea consistente. Números tabulares para columnas de precios.

## Paleta (tema claro)

Neutros cálidos y sobrios, al estilo del admin de Shopify. Se definirán como **tokens de tema** en un `ResourceDictionary` de WPF.

| Token | Hex | Uso |
|---|---|---|
| `Bg/App` | `#F1F1F1` | Fondo general de la app |
| `Bg/Surface` | `#FFFFFF` | Tarjetas, paneles, listas |
| `Bg/SurfaceSubdued` | `#FAFAFA` | Filas alternas, zonas secundarias |
| `Bg/Hover` | `#F1F1F1` | Estado hover de filas/botones sutiles |
| `Bg/Selected` | `#EBF5FF` | Fila/tarjeta seleccionada |
| `Border` | `#E3E3E3` | Bordes de tarjetas e inputs |
| `Border/Strong` | `#C9CCCF` | Bordes de inputs enfocados |
| `Text/Primary` | `#1A1A1A` | Texto principal |
| `Text/Secondary` | `#616161` | Texto secundario / ayudas |
| `Text/Disabled` | `#A6A6A6` | Deshabilitado |
| `Action/Primary` | `#303030` | Botón primario (fondo, estilo Shopify "oscuro") |
| `Action/PrimaryText` | `#FFFFFF` | Texto de botón primario |
| `Action/PrimaryHover` | `#1A1A1A` | Hover primario |
| `Accent` | `#2C6ECB` | Enlaces, foco, selección |
| `Success` | `#007F5F` | Confirmaciones, stock ok, pago |
| `Success/Bg` | `#E7F5EF` | Fondos de badge éxito |
| `Warning` | `#B98900` | Bajo stock |
| `Warning/Bg` | `#FFF3D6` | |
| `Critical` | `#D72C0D` | Errores, cancelar/eliminar |
| `Critical/Bg` | `#FDECEA` | |

> Aunque el requisito es **tema claro**, todos los colores viven como tokens; agregar un tema oscuro después sería solo un segundo diccionario.

## Espaciado, radios y sombras

- **Rejilla base de 4 px.** Espaciados: 4, 8, 12, 16, 20, 24, 32.
- **Radio de esquinas:** 8 px (tarjetas, botones, inputs); 12 px para tarjetas de producto grandes.
- **Sombras:** muy sutiles. Tarjetas con sombra ligera (`0 1px 2px rgba(0,0,0,.05)`); elevaciones mayores solo en diálogos/popovers.
- **Bordes** de 1 px preferidos sobre sombras fuertes (look plano y limpio).

## Componentes base (estilos WPF reutilizables)

Se definen como `Style`/`ControlTemplate` en `Themes/`:

- **Botones:** `PrimaryButton` (oscuro), `SecondaryButton` (borde gris, fondo blanco), `SubtleButton` (sin borde), `CriticalButton` (rojo). Altura mínima **44 px** (POS: 48–56 px).
- **Campos de texto** (`TextBox` estilizado): borde `Border`, foco `Accent`, placeholder, ícono opcional (lupa en búsqueda).
- **Tarjeta** (`Card`): superficie blanca, radio 8, padding 16, borde sutil.
- **Badge/Etiqueta de estado:** píldoras para stock, estado de venta, método de pago.
- **Lista/DataGrid** estilizado: filas altas (mín. 44 px), hover, selección, encabezados sobrios.
- **Cantidad (stepper):** botones grandes `–`/`+` táctiles con campo numérico.
- **Teclado numérico en pantalla:** para cobro y cantidades en modo táctil.
- **Diálogo/Modal:** encabezado, contenido, acciones (primaria a la derecha).
- **Toast/Notificación:** confirmaciones no intrusivas (venta registrada, respaldo hecho).

## Estructura general de la app (shell)

```
┌───────────────────────────────────────────────────────────────────┐
│  Barra superior:  [test_tienda]   Caja: Abierta ($1,000)   Usuario ▾    │  56 px
├───────┬───────────────────────────────────────────────────────────┤
│       │                                                            │
│  Nav  │                Área de contenido (vista activa)            │
│ later │                                                            │
│ (íconos)                                                           │
│ POS   │                                                            │
│ Inv.  │                                                            │
│ Vent. │                                                            │
│ Clien.│                                                            │
│ Repor.│                                                            │
│ Config│                                                            │
└───────┴───────────────────────────────────────────────────────────┘
```

- **Barra lateral** de navegación con íconos + etiqueta (colapsable). Ítems: **Punto de venta**, Inventario, Ventas, Apartados, Compras, Clientes, Proveedores, Reportes, Configuración. Los ítems se muestran según permisos.
- **Barra superior:** nombre de tienda, estado de la caja (abierta/cerrada + fondo), usuario y menú (bloquear, cerrar sesión).

## Pantalla estrella — Punto de venta (POS)

Diseño de **dos columnas**: a la izquierda la selección de productos (con dos modos), a la derecha el **ticket/carrito**.

```
┌──────────────────────────────────────────────┬──────────────────────┐
│  🔍 [ Buscar por nombre, código o SKU…      ] │   TICKET             │
│  [ Búsqueda ]  [ Grid ]      ← conmutador     │                      │
│                                               │  Playera negra  M    │
│  MODO BÚSQUEDA:                               │   $199 × 2   $398  🗑 │
│  ┌─────────────────────────────────────────┐ │  Gorra roja          │
│  │ Playera negra — M   SKU PLN-NEG-M  $199 │ │   $149 × 1   $149  🗑 │
│  │ Playera negra — L   SKU PLN-NEG-L  $199 │ │                      │
│  │ Gorra roja          SKU GOR-ROJ    $149 │ │  ────────────────    │
│  └─────────────────────────────────────────┘ │  Subtotal    $547    │
│                                               │  Desc.        $0     │
│  MODO GRID (táctil):                          │  IVA         incl.   │
│  [Todas][Playeras][Gorras][Pantalones]…       │  ────────────────    │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐  │  TOTAL      $547     │
│  │ [foto] │ │ [foto] │ │ [foto] │ │ [foto] │  │                      │
│  │Playera │ │ Gorra  │ │Pantalón│ │Sudadera│  │  Cliente: General ▾  │
│  │ $199   │ │ $149   │ │ $399   │ │ $549   │  │  [   COBRAR  $547  ]  │  ← primario, grande
│  └────────┘ └────────┘ └────────┘ └────────┘  │                      │
└──────────────────────────────────────────────┴──────────────────────┘
```

### Modo búsqueda (mouse/teclado/lector)
- Campo de búsqueda **siempre enfocado** al abrir; el cursor vuelve ahí tras cada acción.
- Al **escanear** un código de barras: se agrega el producto al ticket automáticamente (el lector "teclea" el código + Enter).
- Al escribir texto: lista de resultados en vivo (nombre, atributos, SKU, precio, stock). Enter agrega el primero; flechas para navegar.
- Ideal para catálogos grandes y cajeros rápidos.

### Modo grid (táctil)
- **Tarjetas grandes** de producto (foto, nombre, precio) en cuadrícula que fluye según ancho.
- **Filtros por categoría** como chips grandes arriba.
- Un toque agrega al ticket; si el producto tiene variantes (talla/color), abre un **selector grande** de variante antes de agregar.
- Scroll táctil con inercia.

### Conmutador
- Botón segmentado **[Búsqueda | Grid]** siempre visible. El cajero elige según el momento (una venta con lector vs. mostrar catálogo al cliente).

### Panel de ticket (derecha)
- Líneas con nombre + variante, precio, **stepper de cantidad grande**, importe y botón eliminar.
- Descuentos (por línea y global) con permiso.
- Totales claros; **TOTAL** en tamaño Display.
- Selector de **cliente** (opcional).
- **Botón COBRAR** primario, ancho completo, alto (≥ 56 px), muestra el total.

### Flujo de cobro (diálogo)
- Total grande, **botonera de método** (Efectivo / Tarjeta / Mixto) con objetivos grandes.
- **Teclado numérico en pantalla** + montos rápidos ($50, $100, $200, $500).
- Cálculo de **cambio** en vivo.
- Confirmar → registra venta, **imprime ticket**, **abre cajón**, muestra toast y limpia el carrito.

## Accesibilidad y ergonomía táctil

- Objetivos táctiles ≥ 44 px (meta 48), separación ≥ 8 px.
- Texto mínimo 14 px; contraste AA como mínimo.
- Foco visible para navegación por teclado.
- Estados claros (cargando, vacío, error) en cada vista.
- Escalado: la UI se ve bien a 100% y 125% de escala de Windows (común en All-in-One táctiles).

## Otras pantallas (resumen)

- **Inventario:** lista/DataGrid con búsqueda y filtros; edición de producto y variantes; alertas de bajo stock; ajustes de stock.
- **Ventas:** historial con filtros por fecha/usuario; detalle de venta; reimpresión de ticket; devoluciones (con permiso).
- **Apartados:** lista por estado; crear apartado; registrar abono; liquidar.
- **Compras:** registrar compra a proveedor; recepción que suma stock.
- **Clientes/Proveedores:** ABC (alta/baja/cambio) con búsqueda.
- **Reportes:** tarjetas con KPIs (venta del día, ticket promedio, top productos) y tablas/gráficas simples; corte de caja.
- **Configuración:** datos de tienda/ticket, impresora, impuestos, folios, usuarios y roles, respaldos.

## Assets

- **Íconos:** set open source consistente (p. ej. **Lucide** / **Fluent System Icons**) como recursos vectoriales.
- **Fuente Inter:** incluida en `Presentation/Fonts/`.
- Placeholder para productos sin foto (inicial del nombre sobre color suave).
