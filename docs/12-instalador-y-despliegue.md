# 12 — Instalador y puesta en marcha

Cómo empaquetar USASHOPP POS en un instalador `.exe` e instalarlo en el All-in-One.

## Equipo destino

- All-in-One táctil, **Intel Core i7-6700T (x64)**, Windows 10 → se publica para **win-x64**.
- La build es **self-contained**: incluye el runtime de .NET, así que **no hay que instalar
  nada extra** en el equipo destino.

## 1) Publicar la aplicación (en Windows)

Desde la raíz del repositorio, en PowerShell:

```powershell
./build/publish.ps1
```

Esto genera la carpeta `publish/win-x64/` con `USASHOPP POS.exe` y todas sus dependencias.

> Equivale a:
> ```powershell
> dotnet publish src/Usashopp.Pos.Wpf/Usashopp.Pos.Wpf.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
> ```

## 2) Compilar el instalador (Inno Setup)

Instala Inno Setup una vez:

```powershell
winget install JRSoftware.InnoSetup
```

Compila el instalador:

```powershell
ISCC installer/USASHOPP-POS.iss
```

(o abre `installer/USASHOPP-POS.iss` en Inno Setup y presiona **Compilar**).

El instalador queda en **`dist/USASHOPP-POS-Setup-1.0.0.exe`**.

## 3) Instalar en el All-in-One

1. Copia `USASHOPP-POS-Setup-1.0.0.exe` al equipo (USB o red).
2. Ejecútalo (pide permisos de administrador) y sigue el asistente.
3. Se instala en `C:\Program Files\USASHOPP POS\` y crea accesos directos.
4. La base de datos, respaldos y logs viven en `C:\ProgramData\USASHOPP POS\`
   (compartidos entre todos los usuarios de Windows del equipo).

## 4) Primer arranque en sitio

1. Abre **USASHOPP POS**. La primera vez crea la base y siembra datos.
2. Inicia sesión con **`admin` / `admin`** y **cambia la contraseña** (Usuarios → Editar).
3. En **Configuración**: datos de la tienda, impuestos y respaldos.
4. Da de alta el **catálogo** (Inventario → Nuevo producto) y usuarios (cajeros).
5. Conecta y configura los **periféricos** (ver [docs/08](08-hardware-perifericos.md)):
   impresora de tickets, cajón y lector de código de barras.

> **Impresión y cajón:** en esta versión son *stubs* (registran en el log). La integración
> ESC/POS real (Fase 4) se hace con la impresora física ya conectada.

## Actualizaciones

Para publicar una nueva versión:
1. Sube el número en `installer/USASHOPP-POS.iss` (`AppVersion`).
2. Repite pasos 1 y 2. El instalador actualiza sobre la instalación existente
   (mismo `AppId`) **sin tocar** los datos de `C:\ProgramData\USASHOPP POS\`.

## Respaldos

- **Manual:** Configuración → *Crear respaldo ahora*.
- **Automático:** al **cerrar caja** (corte) y **por temporizador** cada
  `Infrastructure:CadaHoras` horas (configurable en `appsettings.json`; 0 = desactivado).
- Copias en `C:\ProgramData\USASHOPP POS\backups\`; opcionalmente a una carpeta en la nube
  (`Infrastructure:CarpetaNube`). Se conservan las últimas `RetenerUltimos`.
