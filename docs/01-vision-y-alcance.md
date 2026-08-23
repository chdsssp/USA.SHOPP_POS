# 01 — Visión y alcance

## Visión

Ofrecer a la tienda un punto de venta moderno, rápido y confiable que funcione de forma **autónoma en un solo equipo** (sin depender de internet), que sea **fácil de operar con la pantalla táctil** y a la vez **veloz con teclado y lector de código de barras**, y que además centralice la operación del negocio: inventario, clientes, proveedores, compras, apartados y reportes.

## Principios de producto

1. **Rapidez de cobro por encima de todo.** Una venta común (buscar → agregar → cobrar → ticket) debe tomar segundos. El flujo se optimiza para el 80% de los casos.
2. **Dos formas de trabajar, misma pantalla.** El cajero puede escribir/escanear o tocar. Ambos modos conviven sin cambiar de ventana.
3. **Táctil de primera clase.** Objetivos grandes (mínimo 44×44 px, meta 48 px), sin gestos ocultos, teclado en pantalla cuando aplica.
4. **Nunca perder una venta ni un dato.** Funciona offline; los respaldos son automáticos.
5. **Minimalismo funcional.** Estética limpia estilo Shopify: mucho espacio en blanco, jerarquía tipográfica clara, color usado con intención.

## Usuarios y roles

| Rol | Descripción | Acciones típicas |
|-----|-------------|------------------|
| **Cajero/Vendedor** | Opera la caja en el día a día | Vender, cobrar, imprimir ticket, consultar producto, apartados, abrir caja |
| **Encargado/Supervisor** | Gestiona la tienda | Todo lo del cajero + descuentos especiales, devoluciones, cortes de caja, ver reportes |
| **Administrador** | Dueño / responsable | Todo + alta de productos, precios, compras, proveedores, usuarios, configuración |

Los permisos son granulares y configurables por rol (ver [Modelo de dominio](04-modelo-de-dominio.md)).

## Alcance del MVP (v1.0)

### Ventas (POS)
- Modo búsqueda (nombre, código de barras, SKU, características/atributos).
- Modo grid táctil con filtro por categoría.
- Carrito/ticket: cantidades, descuentos por línea y por venta, notas.
- Selección de cliente (opcional).
- Cobro: efectivo, tarjeta, mixto; cálculo de cambio.
- Impresión de ticket (ESC/POS) y apertura de cajón.
- Cancelación de línea y de venta (con permiso).

### Catálogo e inventario
- Productos con **variantes** (talla, color) y sus propios SKU/código de barras, precio y costo.
- Categorías y marcas.
- Stock por variante; movimientos de inventario (venta, compra, ajuste, devolución).
- Alertas de bajo stock.

### Compras
- Registro de compras a proveedores que **reingresan stock** y actualizan costo.

### Clientes y proveedores
- Alta/edición, historial básico de compras del cliente.

### Apartados (layaway)
- Crear apartado con anticipo, registrar abonos, liquidar y convertir en venta.

### Caja
- Sesión de caja (apertura con fondo, cierre/corte con conteo y diferencia).

### Usuarios
- Autenticación con PIN/contraseña, roles y permisos.

### Reportes
- Ventas por día/rango, productos más vendidos, existencias, corte de caja.

### Sistema
- Respaldo automático y restauración de la base de datos.
- Configuración de tienda (datos del ticket, impuestos, folios).

## Fuera de alcance (por ahora)

- Integración con terminal bancaria (se deja **preparado** el registro del pago con tarjeta, pero el cobro se hace en la terminal física por separado).
- Tienda en línea / sincronización con Shopify u otro e-commerce.
- Multi-sucursal / multi-terminal con servidor central (la arquitectura no lo impide a futuro).
- App móvil.
- Facturación electrónica (CFDI/SAT). *Se recomienda evaluarlo en una fase posterior si el negocio lo requiere.*

## Métricas de éxito

- Tiempo de una venta simple ≤ 10 s.
- La app arranca en ≤ 5 s en el equipo objetivo.
- 0 pérdidas de datos gracias a respaldos.
- El cajero puede operar el 100% del flujo de venta con solo la pantalla táctil.

## Idioma y localización

- Interfaz y datos en **español (México)**.
- Moneda **MXN**, formato de fecha/hora local (GMT-07:00 Mazatlán).
