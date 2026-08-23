# USASHOPP POS

Sistema de **Punto de Venta (POS) y gestión de tienda** para ropa y artículos varios, desarrollado como **aplicación nativa de Windows 10** con **WPF (.NET 8)** y **Clean Architecture**.

Diseño minimalista, limpio y moderno en **tema claro**, inspirado en la tipografía y los botones del panel de administración de Shopify. Optimizado para uso con **mouse + teclado** (búsqueda rápida) y con **pantalla táctil** (grid visual de productos).

---

## ¿Qué resuelve?

Una caja registradora moderna para una tienda física de ropa y artículos varios que además controla inventario, clientes, proveedores, compras, apartados y reportes — todo funcionando **100% local y sin depender de internet**, con respaldos automáticos.

## Características clave (MVP)

- **Venta rápida** con dos modos intercambiables:
  - **Modo búsqueda** (teclado/mouse): por nombre, código de barras/SKU o características.
  - **Modo grid** (táctil): tarjetas grandes de productos, filtrado por categoría.
- **Catálogo con variantes**: producto con talla y color (ideal para ropa).
- **Inventario**: stock por variante, movimientos, alertas de bajo stock.
- **Cobro**: efectivo, tarjeta y pago mixto; cálculo de cambio.
- **Ticket** en impresora térmica (ESC/POS) y apertura de **cajón de dinero**.
- **Apartados** (layaway) con abonos.
- **Clientes y proveedores**.
- **Compras** que reingresan stock.
- **Usuarios y roles** con permisos.
- **Corte de caja** y **reportes** básicos.
- **Respaldos automáticos** de la base de datos.

## Hardware objetivo

All-in-One táctil — Intel Core i7-6700T, 12 GB RAM, SSD 240 GB, Windows 10. El sistema está pensado para ser fluido en ese equipo y **cómodo de usar con el dedo** (objetivos táctiles grandes).

---

## Documentación

| # | Documento | Contenido |
|---|-----------|-----------|
| 01 | [Visión y alcance](docs/01-vision-y-alcance.md) | Objetivos, usuarios, alcance MVP y fuera de alcance |
| 02 | [Arquitectura](docs/02-arquitectura.md) | Clean Architecture, capas, dependencias, MVVM |
| 03 | [Stack tecnológico](docs/03-stack-tecnologico.md) | .NET 8, WPF, EF Core, librerías, herramientas |
| 04 | [Modelo de dominio](docs/04-modelo-de-dominio.md) | Entidades, value objects, reglas de negocio |
| 05 | [Base de datos](docs/05-base-de-datos.md) | Esquema SQLite, tablas, relaciones, respaldos |
| 06 | [Diseño UI/UX](docs/06-diseno-ui-ux.md) | Sistema de diseño estilo Shopify, tokens, pantallas |
| 07 | [Estructura de la solución](docs/07-estructura-solucion.md) | Proyectos, carpetas, convenciones de código |
| 08 | [Hardware y periféricos](docs/08-hardware-perifericos.md) | Lector, impresora ESC/POS, cajón de dinero |
| 09 | [Roadmap](docs/09-roadmap.md) | Fases de desarrollo e hitos |
| 10 | [Puesta en marcha (dev)](docs/10-desarrollo.md) | Compilar, migraciones EF, ejecutar y probar |
| 11 | [Ejecutar en VM de Windows (desde Mac)](docs/11-ejecutar-en-windows-vm.md) | Correr el WPF real desde una Mac vía Windows 11 ARM |

---

## Estructura del código

```
Usashopp.Pos.sln
├── src/
│   ├── Usashopp.Pos.Domain/          Núcleo: entidades, value objects, reglas
│   ├── Usashopp.Pos.Application/     Casos de uso, puertos (interfaces), DTOs
│   ├── Usashopp.Pos.Infrastructure/  EF Core/SQLite, repos, hardware, respaldos
│   └── Usashopp.Pos.Wpf/             UI WPF (MVVM), temas, shell (arranque)
└── tests/                            Pruebas de Domain, Application e Infrastructure
```

Detalle en [docs/07 — Estructura de la solución](docs/07-estructura-solucion.md).

## Cómo ejecutar (en Windows)

```bash
dotnet restore
dotnet ef migrations add Inicial --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf --output-dir Persistence/Migrations
dotnet run --project src/Usashopp.Pos.Wpf
```

Guía completa: [docs/10 — Puesta en marcha](docs/10-desarrollo.md). **Nota:** WPF solo compila/ejecuta en Windows.

---

## Estado

🚧 **Fases 1–3 en progreso.**

- **Fase 1 (Cimientos):** solución con las 4 capas + tests, dominio completo, puertos, infraestructura de datos (SQLite + repos + seed + respaldos) y UI WPF con temas y shell. ✅
- **Fase 2 (Inventario):** alta de productos con variantes, listado con búsqueda y filtro de bajo stock, y ajuste de existencias. 🚧
- **Fase 3 (POS):** venta de punta a punta — búsqueda en vivo / lector, grid táctil por categoría, carrito, apertura de caja y cobro (efectivo/tarjeta, teclado numérico, cambio) que descuenta stock. 🚧

> Durante el desarrollo se inicia sesión automáticamente como `admin` (temporal, hasta la pantalla de login de la Fase 6).

Siguiente paso: completar detalles del POS y **Fase 5 — Operación de tienda** (ver [Roadmap](docs/09-roadmap.md)).
