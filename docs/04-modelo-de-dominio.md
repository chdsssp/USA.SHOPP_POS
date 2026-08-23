# 04 — Modelo de dominio

Este documento describe las **entidades**, **value objects** y **reglas de negocio** que viven en la capa `Domain`. Es independiente de la base de datos (ver el mapeo en [Base de datos](05-base-de-datos.md)).

## Diagrama de relaciones (alto nivel)

```
Categoria 1───* Producto 1───* VarianteProducto *───1 (Talla, Color)
                                     │
Proveedor 1──* Compra 1──* DetalleCompra *──1 VarianteProducto
                                     │
Cliente 1──* Venta 1──* DetalleVenta *──1 VarianteProducto
              │  │
              │  *── Pago
              │
Cliente 1──* Apartado 1──* AbonoApartado
                    *──* DetalleApartado *──1 VarianteProducto

VarianteProducto 1──* MovimientoInventario

Usuario *──1 Rol *──* Permiso
SesionCaja 1──* Venta        (una venta pertenece a una sesión de caja)
```

## Value Objects

Objetos inmutables que representan conceptos, no cosas con identidad.

### `Dinero`
- Encapsula `decimal Monto` + `string Moneda` (MXN).
- Evita errores de redondeo y mezcla de monedas. Operaciones `Sumar`, `Multiplicar(cantidad)`, `AplicarDescuento`.
- **Regla:** los importes se manejan con `decimal` (nunca `double`) y se redondean a 2 decimales al presentar.

### `Sku`
- Cadena normalizada (mayúsculas, sin espacios) que identifica una variante.

### `CodigoBarras`
- Cadena validada (EAN-13/Code128/UPC según se use). Único por variante.

### `Descuento`
- Tipo (`Porcentaje` | `MontoFijo`) + valor. Sabe calcular el importe descontado sobre un `Dinero`.

## Entidades

> Convención: toda entidad tiene `Id` (GUID o int), `CreadoEn`, `ActualizadoEn`. Se usa **borrado lógico** (`Activo`/`EliminadoEn`) para catálogos, para no romper el histórico de ventas.

### Catálogo

**`Categoria`**
- `Nombre`, `Descripcion?`, `Activo`.

**`Producto`**
- `Nombre`, `Descripcion?`, `CategoriaId`, `Marca?`, `Activo`.
- Un producto **agrupa variantes**. El precio y el stock viven en la variante.
- Regla: un producto debe tener al menos una variante.

**`VarianteProducto`**  *(el ítem que realmente se vende)*
- `ProductoId`, `Sku`, `CodigoBarras?`, `Talla?`, `Color?`.
- `PrecioVenta : Dinero`, `Costo : Dinero`.
- `StockActual : int`, `StockMinimo : int`.
- Reglas:
  - `PrecioVenta >= 0`, `Costo >= 0`.
  - `StockActual` **solo** cambia mediante `MovimientoInventario` (nunca se edita a mano directamente en ventas).
  - `EstaBajoMinimo => StockActual <= StockMinimo`.

### Inventario

**`MovimientoInventario`**
- `VarianteId`, `Tipo` (enum), `Cantidad` (+/-), `Motivo?`, `ReferenciaId?` (venta/compra/ajuste), `UsuarioId`, `Fecha`.
- Es la **fuente de la verdad** del stock: `StockActual` es la suma de movimientos (se mantiene desnormalizado por rendimiento y se reconstruye si es necesario).

`enum TipoMovimientoInventario`: `Venta`, `Compra`, `AjustePositivo`, `AjusteNegativo`, `Devolucion`, `Merma`, `InventarioInicial`.

### Ventas

**`Venta`**
- `Folio` (consecutivo), `SesionCajaId`, `UsuarioId`, `ClienteId?`, `Fecha`.
- `Estado` (enum), `DescuentoGlobal? : Descuento`, `Notas?`.
- Colecciones: `Detalles`, `Pagos`.
- Propiedades calculadas: `Subtotal`, `TotalDescuentos`, `Impuestos`, `Total`, `TotalPagado`, `Cambio`.
- Reglas:
  - No se registra sin una **sesión de caja abierta** → `CajaNoAbiertaException`.
  - `TotalPagado >= Total` para cerrarse como `Pagada` (salvo apartado).
  - Al confirmarse, genera un `MovimientoInventario` de tipo `Venta` por cada línea; si alguna variante no tiene stock → `StockInsuficienteException` (configurable si se permite venta en negativo).

`enum EstadoVenta`: `EnProceso`, `Pagada`, `Cancelada`, `Devuelta`, `ParcialmenteDevuelta`.

**`DetalleVenta`**
- `VentaId`, `VarianteId`, `Descripcion` (copiada al momento de la venta), `Cantidad`, `PrecioUnitario : Dinero`, `Descuento? : Descuento`.
- `Importe => PrecioUnitario * Cantidad - descuento`.
- Guarda `Descripcion` y `PrecioUnitario` **congelados** (si luego cambia el precio del producto, el histórico no se altera).

**`Pago`**
- `VentaId`, `Metodo` (enum), `Monto : Dinero`, `Referencia?` (autorización de tarjeta, etc.).
- Permite **pago mixto** (varios pagos en una venta).

`enum MetodoPago`: `Efectivo`, `Tarjeta`, `Transferencia`, `Vales`, `Otro`.

### Apartados (layaway)

**`Apartado`**
- `Folio`, `ClienteId` (obligatorio), `Fecha`, `FechaLimite?`, `Estado`.
- `Detalles` (variantes reservadas), `Abonos`.
- `Total`, `TotalAbonado`, `Saldo`.
- Reglas:
  - Al crear, **reserva stock** (movimiento o marca de reservado, según configuración).
  - No se liquida si `Saldo > 0`.
  - Al liquidar, se convierte en `Venta` y libera/consume el stock reservado.

`enum EstadoApartado`: `Activo`, `Liquidado`, `Cancelado`, `Vencido`.

**`AbonoApartado`**: `ApartadoId`, `Monto : Dinero`, `Metodo`, `Fecha`, `UsuarioId`.

### Compras

**`Compra`**
- `Folio`, `ProveedorId`, `Fecha`, `Estado`, `Detalles`, `Total`.
- Al recibirse, genera `MovimientoInventario` tipo `Compra` (+stock) y puede actualizar el `Costo` de la variante.

**`DetalleCompra`**: `CompraId`, `VarianteId`, `Cantidad`, `CostoUnitario : Dinero`.

### Terceros

**`Cliente`**: `Nombre`, `Telefono?`, `Email?`, `Notas?`, `Activo`.
**`Proveedor`**: `Nombre`, `Contacto?`, `Telefono?`, `Email?`, `Activo`.

### Caja

**`SesionCaja`**
- `UsuarioId`, `FechaApertura`, `FondoInicial : Dinero`, `FechaCierre?`, `MontoContado? : Dinero`.
- Calculado: `TotalVentasEfectivo`, `EfectivoEsperado`, `Diferencia`.
- Reglas: solo puede haber **una sesión abierta** a la vez por caja.

`enum EstadoSesionCaja`: `Abierta`, `Cerrada`.

### Usuarios y seguridad

**`Usuario`**: `Nombre`, `UsuarioLogin`, `HashContrasena` (o PIN hasheado), `RolId`, `Activo`.
**`Rol`**: `Nombre`, `Permisos` (colección).
**`Permiso`**: clave (`ventas.crear`, `ventas.cancelar`, `descuentos.aplicar`, `inventario.editar`, `reportes.ver`, `usuarios.gestionar`, `caja.corte`, `config.editar`, …).

Reglas:
- Contraseñas/PIN **hasheados** (nunca en texto plano) con un algoritmo fuerte (p. ej. PBKDF2/BCrypt).
- Acciones sensibles verifican permiso en la capa Application, no solo ocultando botones en la UI.

## Configuración de tienda

**`ConfiguracionTienda`** (una fila):
- Datos del ticket (nombre, dirección, RFC opcional, teléfono, mensaje pie).
- `TasaImpuesto` (p. ej. IVA 16% — o 0 si los precios ya lo incluyen), `ImpuestoIncluidoEnPrecio` (bool).
- Prefijos/consecutivos de folios (venta, apartado, compra).
- Nombre/puerto de la impresora, permitir venta con stock negativo, etc.

## Servicios de dominio (lógica que no cabe en una sola entidad)

- **`CalculadoraTotalesVenta`**: aplica descuentos de línea, descuento global e impuestos y produce el desglose (subtotal, descuento, impuesto, total).
- **`GeneradorFolios`**: genera consecutivos por tipo de documento de forma segura.

## Invariantes clave (resumen)

1. El stock cambia **solo** por movimientos de inventario.
2. Una venta requiere **caja abierta**.
3. Los importes de líneas y descripciones se **congelan** al momento de la venta.
4. Un apartado no se liquida con saldo pendiente.
5. Solo una sesión de caja abierta a la vez.
6. Los permisos se validan del lado del negocio, no solo en la UI.
