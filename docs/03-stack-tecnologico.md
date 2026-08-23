# 03 — Stack tecnológico

## Resumen

| Área | Elección | Por qué |
|------|----------|---------|
| Plataforma | **Windows 10 (x64)** nativo | Requisito del negocio |
| Runtime | **.NET 8 LTS** | Soporte a largo plazo, rápido, moderno |
| UI | **WPF** | Nativa, madura, excelente soporte táctil y de estilos |
| Patrón UI | **MVVM** (CommunityToolkit.Mvvm) | Testeable, idiomático en WPF, ligero |
| Composición | **Microsoft.Extensions.Hosting** (Generic Host) | DI, configuración y logging unificados |
| ORM | **EF Core 8** + provider **SQLite** | Productivo, migraciones, LINQ |
| Base de datos | **SQLite** (archivo local) | Cero administración, ideal para una caja |
| Validación | **FluentValidation** | Reglas claras y testeables |
| Logging | **Serilog** (archivo rotativo) | Diagnóstico en campo |
| Impresión | **ESC/POS** (`ESCPOS_NET` o RAW spooler) | Tickets térmicos + cajón |
| Reportes/impresión de documentos | **QuestPDF** (opcional) | Reportes en PDF si se requieren |
| Pruebas | **xUnit** + **FluentAssertions** + **NSubstitute** | Estándar y legible |
| Instalador | **Inno Setup** o **Velopack** | Instalación y actualizaciones sencillas |

## Detalle y justificación

### .NET 8 + WPF
- **.NET 8 es LTS** (soporte hasta nov-2026 y más con parches), rápido y con arranque optimizado — importante en el i7-6700T.
- **WPF** es la opción nativa más madura para apps de negocio en Windows: estilos/temas potentes (clave para el look Shopify), binding de datos de primera y **soporte táctil integrado** (eventos touch, `manipulation`, scroll inercial).
- Publicación **self-contained** o dependiente del framework; recomendamos framework-dependent con .NET 8 Desktop Runtime instalado en el equipo (menor tamaño), o self-contained si se prefiere que no dependa de instalar el runtime.

### MVVM con CommunityToolkit.Mvvm
- Ligero y con **source generators**: `[ObservableProperty]` y `[RelayCommand]` eliminan código repetitivo.
- No arrastra un framework pesado; encaja perfecto con Clean Architecture.

### Generic Host + DI
- `Host.CreateApplicationBuilder()` centraliza:
  - **Inyección de dependencias** (registro de repos, servicios, ViewModels).
  - **Configuración** (`appsettings.json`: cadena de conexión, impresora, tienda).
  - **Logging** (Serilog).
- La `App.xaml.cs` arranca el host y resuelve la `MainWindow`.

### EF Core 8 + SQLite
- **SQLite** es perfecto para un POS de una sola caja: un archivo `.db`, sin servidor, muy rápido para este volumen.
- **EF Core** da migraciones versionadas, LINQ y mapeo limpio. El `DbContext` vive **solo** en Infrastructure.
- Índices en código de barras y SKU para búsquedas instantáneas.
- Modo **WAL** activado para mejor concurrencia lectura/escritura y robustez.

### Impresión ESC/POS y cajón de dinero
- La mayoría de impresoras térmicas hablan **ESC/POS**. Se envían bytes de comandos (texto, alineación, corte, y el "kick" que **abre el cajón** conectado a la impresora).
- Estrategia: interfaz `ITicketPrinter`/`ICashDrawer` en Application; implementación en Infrastructure vía librería `ESCPOS_NET` (o escritura RAW al spooler de Windows por nombre de impresora). Ver [Hardware](08-hardware-perifericos.md).

### Lector de código de barras
- Los lectores USB actúan como **teclado** (keyboard wedge): "teclean" el código y un Enter. La captura se maneja en el `PosViewModel` detectando entrada rápida terminada en Enter. No requiere driver especial.

### Respaldos
- `IBackupService` copia el archivo SQLite (con checkpoint WAL) a una carpeta de respaldos con fecha, y opcionalmente a una carpeta sincronizada en la nube (p. ej. una carpeta de Google Drive/OneDrive local). Programado al cierre de caja y por temporizador.

### Pruebas
- **xUnit** para unit/integration, **FluentAssertions** para aserciones legibles, **NSubstitute** para dobles de prueba de los puertos.

### Empaquetado y actualizaciones
- **Inno Setup**: instalador clásico .exe, simple y confiable en Windows 10.
- **Velopack** (alternativa moderna): instalador + **auto-actualización** con poco esfuerzo. Recomendado si se prevén actualizaciones frecuentes.

## Paquetes NuGet previstos

```
# Presentation (WPF)
CommunityToolkit.Mvvm
Microsoft.Extensions.Hosting
Microsoft.Extensions.Configuration.Json

# Application
FluentValidation

# Infrastructure
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.EntityFrameworkCore.Design      # migraciones
Serilog.Extensions.Hosting
Serilog.Sinks.File
ESCPOS_NET                                 # impresión térmica (evaluar)
QuestPDF                                   # reportes PDF (opcional)

# Tests
xunit
xunit.runner.visualstudio
FluentAssertions
NSubstitute
Microsoft.EntityFrameworkCore.Sqlite       # integración
```

## Requisitos del entorno de desarrollo

> ⚠️ **Importante:** WPF **solo compila y se ejecuta en Windows**. No es posible construir/depurar esta app en macOS o Linux.

- **Windows 10/11** con **Visual Studio 2022** (carga de trabajo ".NET Desktop Development") o **VS Code + C# Dev Kit** + **.NET 8 SDK**.
- Se puede desarrollar en:
  1. El propio equipo All-in-One (si tiene recursos y comodidad), o
  2. Otra PC con Windows, o
  3. Una **máquina virtual Windows** (Parallels/UTM/VMware) si el desarrollo se hace desde la Mac actual.
- Git para control de versiones (el repositorio aún **no** está inicializado).

> Si en algún momento no fuera viable tener Windows para desarrollar, la única alternativa que compila desde macOS con look nativo sería **Avalonia**; implicaría reescribir la capa de Presentation, pero **Domain/Application/Infrastructure se reutilizarían casi intactos** gracias a Clean Architecture.
