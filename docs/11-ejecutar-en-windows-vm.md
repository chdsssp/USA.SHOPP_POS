# 11 — Ejecutar en una VM de Windows (desde Mac Apple Silicon)

WPF solo corre en Windows. Para ver/probar la app desde tu Mac (Apple Silicon) montamos
**Windows 11 ARM** en una máquina virtual y compilamos/ejecutamos ahí. El código no cambia.

## 1) Elegir el hipervisor

| Opción | Costo | Notas |
|---|---|---|
| **VMware Fusion** (recomendado) | Gratis (uso personal) | Buen rendimiento, carpetas compartidas fáciles |
| **Parallels Desktop** | De pago | El más sencillo: descarga e instala Windows 11 ARM por ti |
| **UTM** | Gratis (open source) | Basado en QEMU; funciona, algo más lento y manual |

## 2) Instalar Windows 11 ARM

- **Parallels:** al crear la VM, elige "Instalar Windows" y lo descarga solo. Listo.
- **VMware Fusion / UTM:** descarga el **ISO oficial de Windows 11 (Arm64)** desde
  Microsoft (`microsoft.com/software-download/windows11arm64`) y crea la VM apuntando a ese ISO.
- Asigna a la VM al menos **4 CPU y 6–8 GB de RAM** para que compile con soltura.

## 3) Instalar herramientas dentro de Windows

Dentro de la VM (todo Arm64):

1. **.NET 8 SDK (Arm64)** — desde `dotnet.microsoft.com/download/dotnet/8.0`.
   Verifica en una terminal (PowerShell):
   ```powershell
   dotnet --version
   ```
2. **Herramienta EF Core:**
   ```powershell
   dotnet tool install --global dotnet-ef
   ```
3. **Editor (opcional):** Visual Studio 2022 o **VS Code + C# Dev Kit**. No es obligatorio
   para compilar: el **SDK de .NET ya trae WPF**, así que basta el CLI `dotnet`.

> No necesitas Visual Studio completo para compilar WPF: `dotnet build` funciona con solo el SDK.

## 4) Llevar el código a la VM (elige una)

- **A. Carpeta compartida (más simple para empezar):** en Parallels/VMware, comparte la
  carpeta del proyecto de tu Mac. Aparece en Windows como una unidad de red. Editas en la Mac
  y ejecutas en la VM. *Contra:* compilar sobre la red puede ir lento.
- **B. Copiar a disco de la VM (mejor rendimiento):** copia la carpeta `USASHOPP` al disco de
  Windows (p. ej. `C:\dev\USASHOPP`) y compila ahí.
- **C. Git (recomendado a mediano plazo):** inicializa Git en la Mac, súbelo a un repo privado
  y clónalo en la VM. Sincronizas cambios con `git pull` / `git push`.

## 5) Compilar y ejecutar

En la terminal de Windows, dentro de la carpeta del proyecto:

```powershell
dotnet restore
dotnet ef migrations add Inicial --project src/Usashopp.Pos.Infrastructure --startup-project src/Usashopp.Pos.Wpf --output-dir Persistence/Migrations
dotnet run --project src/Usashopp.Pos.Wpf
```

La primera vez crea la base SQLite, siembra datos e inicia sesión como `admin` (temporal).
Deberías ver el shell con el **Punto de venta**.

## 6) Ciclo de trabajo sugerido

1. Editas el código en la **Mac** (con tu editor y conmigo).
2. En la **VM**: `git pull` (opción C) o simplemente vuelves a ejecutar si usas carpeta
   compartida (opción A).
3. `dotnet run --project src/Usashopp.Pos.Wpf` para ver el cambio.
4. Para pruebas de lógica, en cualquiera de los dos lados: `dotnet test`.

## Notas

- **Táctil:** la VM no simula multitáctil real, pero puedes validar el layout y el flujo con
  mouse. La prueba táctil final se hace en el All-in-One.
- **Impresora/cajón:** son stubs hasta la Fase 4; no necesitas hardware para probar el flujo.
- **Primera compilación:** como el desarrollo se hizo en Mac (sin poder compilar WPF), es
  posible que aparezca algún error al compilar por primera vez. Si pasa, **copia el mensaje
  y lo corrijo** — son ajustes menores, no de arquitectura.
