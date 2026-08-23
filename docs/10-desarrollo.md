# 10 — Puesta en marcha para desarrollo

Guía para compilar y ejecutar el esqueleto en **Windows** (WPF solo compila en Windows).

## Requisitos

- **Windows 10/11**
- **.NET 8 SDK** (`dotnet --version` ≥ 8.0)
- **Visual Studio 2022** (carga ".NET Desktop Development") o **VS Code + C# Dev Kit**
- Herramienta EF Core: `dotnet tool install --global dotnet-ef`

## 1) Restaurar y compilar

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet build
```

## 2) Crear la migración inicial (una sola vez)

El proyecto de arranque aplica migraciones automáticamente al iniciar, pero **primero hay
que generar la migración inicial**. El `DbContext` vive en Infrastructure y el proyecto de
arranque es el de WPF:

```bash
dotnet ef migrations add Inicial --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf --output-dir Persistence/Migrations
```

> Si prefieres no usar migraciones al inicio, puedes probar el modelo con la base creada
> por convención, pero **para el desarrollo real usa migraciones** (versionan el esquema).

## 3) Ejecutar

```bash
dotnet run --project src/Usashopp.Pos.Wpf
```

Al arrancar, la app:
1. Configura el host, la inyección de dependencias y el logging (Serilog → archivo).
2. Aplica migraciones y **siembra** permisos, roles, usuario admin y configuración.
3. Muestra la ventana principal (shell) en la sección **Punto de venta**.

**Usuario inicial:** `admin` / contraseña `admin` (cámbiala; la autenticación se conecta
en la Fase 6).

## 4) Ejecutar pruebas

```bash
dotnet test
```

## Ubicaciones en el equipo

| Qué | Ruta |
|---|---|
| Base de datos | `%ProgramData%\USASHOPP POS\data\pos.db` |
| Respaldos | `%ProgramData%\USASHOPP POS\backups\` |
| Logs | `%ProgramData%\USASHOPP POS\logs\` |

Estas rutas se configuran en [`src/Usashopp.Pos.Wpf/appsettings.json`](../src/Usashopp.Pos.Wpf/appsettings.json).

## Comandos EF útiles

```bash
# Nueva migración tras cambiar el modelo
dotnet ef migrations add <Nombre> --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf --output-dir Persistence/Migrations

# Aplicar migraciones manualmente (normalmente lo hace la app al iniciar)
dotnet ef database update --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf
```

## Estado del esqueleto (Fase 1)

Ya está montado:
- Solución con las 4 capas + 3 proyectos de test y sus referencias.
- **Domain** completo (entidades, value objects, enums, reglas, servicios, excepciones).
- **Application** con puertos (repos, hardware, sistema), `Result<T>`, DTOs, validación y
  dos casos de uso: `BuscarProductosService` y `RegistrarVentaService`.
- **Infrastructure** con `AppDbContext` (SQLite), convertidores de value objects,
  configuraciones, repositorios, Unit of Work, respaldos, hashing y `DatabaseInitializer`.
- **Wpf** con host + DI, sistema de temas (tokens estilo Shopify), shell con navegación y
  la pantalla POS (andamiaje de dos columnas con el conmutador Búsqueda/Grid).

Pendiente por fase (ver [Roadmap](09-roadmap.md)): CRUD de inventario, búsqueda y carrito
reales en el POS, cobro, impresión ESC/POS y cajón, apartados, compras, reportes y login.
