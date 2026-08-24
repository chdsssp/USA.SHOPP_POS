# Publica la app WPF lista para empaquetar en el instalador.
# Ejecutar en Windows (x64), desde la raíz del repositorio:  ./build/publish.ps1
#
# Genera una build self-contained (no requiere instalar .NET en el equipo destino)
# para el All-in-One con CPU Intel (x64).

$ErrorActionPreference = "Stop"

$proyecto = "src/Usashopp.Pos.Wpf/Usashopp.Pos.Wpf.csproj"
$salida = "publish/win-x64"

Write-Host "Limpiando salida anterior..." -ForegroundColor Cyan
if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }

Write-Host "Publicando (Release, win-x64, self-contained)..." -ForegroundColor Cyan
dotnet publish $proyecto `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -o $salida

Write-Host ""
Write-Host "Listo. Archivos en: $salida" -ForegroundColor Green
Write-Host "Siguiente paso: compilar el instalador con Inno Setup (ver installer/USASHOPP-POS.iss)." -ForegroundColor Yellow
