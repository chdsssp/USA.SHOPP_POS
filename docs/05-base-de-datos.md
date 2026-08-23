# 05 — Base de datos (SQLite)

## Estrategia

- **Motor:** SQLite (un archivo `pos.db`).
- **Acceso:** EF Core 8 con migraciones versionadas (Code-First). El esquema se genera y evoluciona desde las entidades de dominio + configuraciones de mapeo en Infrastructure.
- **Ubicación del archivo:** `%ProgramData%\USASHOPP POS\data\pos.db` (accesible por todos los usuarios de Windows del equipo, fuera de "Archivos de programa" para evitar problemas de permisos).
- **Modo WAL** activado (`PRAGMA journal_mode=WAL;`) para robustez y mejor lectura/escritura concurrente.
- **Claves foráneas** activadas (`PRAGMA foreign_keys=ON;`).

## Tablas principales

> Tipos indicativos. `TEXT` para GUID/fechas ISO-8601, `INTEGER` para enteros/enums, `NUMERIC` (decimal) para dinero. Toda tabla de catálogo lleva `Activo`, `CreadoEn`, `ActualizadoEn`.

### Catálogo e inventario

**Categorias**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| Nombre | TEXT | único |
| Descripcion | TEXT NULL | |
| Activo | INTEGER | |

**Productos**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| Nombre | TEXT | índice |
| Descripcion | TEXT NULL | |
| CategoriaId | TEXT FK→Categorias | |
| Marca | TEXT NULL | |
| Activo | INTEGER | |

**VariantesProducto**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| ProductoId | TEXT FK→Productos | |
| Sku | TEXT | **único**, índice |
| CodigoBarras | TEXT NULL | **único**, índice |
| Talla | TEXT NULL | |
| Color | TEXT NULL | |
| PrecioVenta | NUMERIC | |
| Costo | NUMERIC | |
| StockActual | INTEGER | desnormalizado |
| StockMinimo | INTEGER | |
| Activo | INTEGER | |

Índices: `IX_Variantes_CodigoBarras`, `IX_Variantes_Sku`, `IX_Variantes_ProductoId`.

**MovimientosInventario**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| VarianteId | TEXT FK→VariantesProducto | índice |
| Tipo | INTEGER | enum |
| Cantidad | INTEGER | +/- |
| Motivo | TEXT NULL | |
| ReferenciaId | TEXT NULL | id de venta/compra/ajuste |
| UsuarioId | TEXT FK→Usuarios | |
| Fecha | TEXT | índice |

### Ventas

**SesionesCaja**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| UsuarioId | TEXT FK→Usuarios | |
| FechaApertura | TEXT | |
| FondoInicial | NUMERIC | |
| FechaCierre | TEXT NULL | |
| MontoContado | NUMERIC NULL | |
| Estado | INTEGER | enum |

**Ventas**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| Folio | TEXT | **único** |
| SesionCajaId | TEXT FK→SesionesCaja | |
| UsuarioId | TEXT FK→Usuarios | |
| ClienteId | TEXT FK→Clientes NULL | |
| Fecha | TEXT | índice |
| Estado | INTEGER | enum |
| DescuentoGlobalTipo | INTEGER NULL | |
| DescuentoGlobalValor | NUMERIC NULL | |
| Subtotal | NUMERIC | congelado |
| TotalDescuentos | NUMERIC | |
| Impuestos | NUMERIC | |
| Total | NUMERIC | |
| Notas | TEXT NULL | |

**DetallesVenta**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| VentaId | TEXT FK→Ventas | índice |
| VarianteId | TEXT FK→VariantesProducto | |
| Descripcion | TEXT | congelada |
| Cantidad | INTEGER | |
| PrecioUnitario | NUMERIC | congelado |
| DescuentoTipo | INTEGER NULL | |
| DescuentoValor | NUMERIC NULL | |
| Importe | NUMERIC | |

**Pagos**
| Columna | Tipo | Notas |
|---|---|---|
| Id | TEXT PK | |
| VentaId | TEXT FK→Ventas | índice |
| Metodo | INTEGER | enum |
| Monto | NUMERIC | |
| Referencia | TEXT NULL | |
| Fecha | TEXT | |

### Apartados

**Apartados**: `Id, Folio(único), ClienteId FK, Fecha, FechaLimite NULL, Estado, Total, TotalAbonado, Saldo`.
**DetallesApartado**: `Id, ApartadoId FK, VarianteId FK, Descripcion, Cantidad, PrecioUnitario`.
**AbonosApartado**: `Id, ApartadoId FK, Monto, Metodo, Fecha, UsuarioId`.

### Compras

**Compras**: `Id, Folio(único), ProveedorId FK, Fecha, Estado, Total`.
**DetallesCompra**: `Id, CompraId FK, VarianteId FK, Cantidad, CostoUnitario`.

### Terceros

**Clientes**: `Id, Nombre(índice), Telefono NULL, Email NULL, Notas NULL, Activo`.
**Proveedores**: `Id, Nombre, Contacto NULL, Telefono NULL, Email NULL, Activo`.

### Seguridad y configuración

**Usuarios**: `Id, Nombre, UsuarioLogin(único), HashContrasena, RolId FK, Activo`.
**Roles**: `Id, Nombre(único)`.
**Permisos**: `Id, Clave(único)`.
**RolesPermisos** (N:N): `RolId FK, PermisoId FK`.
**ConfiguracionTienda**: fila única con datos del ticket, impuestos, folios, impresora, banderas.

## Búsqueda de productos (rendimiento)

La búsqueda del POS debe sentirse instantánea. Estrategia:

1. **Código de barras / SKU exacto** → consulta indexada directa (uso con lector).
2. **Texto libre** (nombre, marca, talla, color) → `LIKE`/`CONTAINS` sobre columnas indexadas; para el catálogo esperado (miles de ítems) es suficiente.
3. Si el catálogo creciera mucho, se puede activar **FTS5** (búsqueda de texto completo de SQLite) sobre nombre/descripcion/atributos como optimización futura.

## Respaldos y restauración

- **`IBackupService`** (implementado en Infrastructure):
  - Ejecuta `PRAGMA wal_checkpoint(TRUNCATE)` y copia el `.db` a `%ProgramData%\USASHOPP POS\backups\pos_YYYYMMDD_HHMM.db`.
  - Retención: conserva los últimos N respaldos (configurable).
  - Copia adicional opcional a una **carpeta en la nube** sincronizada localmente (OneDrive/Google Drive), si está configurada.
- **Disparadores:** al **cerrar caja**, y por temporizador (p. ej. cada X horas mientras la app esté abierta).
- **Restauración:** utilidad en Configuración (solo Administrador) que reemplaza el archivo activo por un respaldo elegido, con la app en modo mantenimiento.

## Datos semilla (seed)

En la primera ejecución se crean:
- Rol **Administrador** con todos los permisos y un usuario admin inicial (obliga a cambiar contraseña).
- Roles **Cajero** y **Encargado** con permisos base.
- Catálogo de permisos.
- `ConfiguracionTienda` con datos de ejemplo (`test_tienda`, editable).

## Migraciones

- Se generan con `dotnet ef migrations add <Nombre>` desde el proyecto Infrastructure.
- Se aplican **automáticamente al iniciar** la app (`Database.Migrate()`), de modo que actualizar el ejecutable actualiza el esquema sin intervención.
