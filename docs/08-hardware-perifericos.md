# 08 — Hardware y periféricos

Equipo objetivo y periféricos requeridos: **lector de código de barras**, **impresora de tickets (ESC/POS)** y **cajón de dinero**.

## Equipo objetivo (All-in-One táctil)

| Componente | Especificación | Implicación de diseño |
|---|---|---|
| CPU | Intel Core i7-6700T (4C/8T, 2.8 GHz) | Suficiente; evitar trabajo pesado en el hilo de UI (usar async). |
| RAM | 12 GB | Holgado para WPF + SQLite. |
| Almacenamiento | SSD 240 GB | Rápido; vigilar tamaño de respaldos y logs (rotación). |
| Pantalla | Táctil | UI táctil de primera clase (doc 06). |
| SO | Windows 10 x64 | .NET 8 Desktop Runtime requerido (o publicar self-contained). |

**Recomendaciones de rendimiento:**
- Todas las operaciones de datos/impresión en **async**; nunca bloquear el hilo de UI.
- **Virtualización** de listas (`VirtualizingStackPanel`) en catálogos e historiales largos.
- Cargar imágenes de producto en tamaño reducido y con caché.
- Arranque rápido: diferir trabajo no esencial; mostrar la ventana cuanto antes.

## Lector de código de barras (USB)

- **Cómo funciona:** la mayoría son **"keyboard wedge"** — se comportan como un teclado: envían los dígitos del código y un **Enter** al final. **No requieren driver especial** ni SDK.
- **Manejo en la app:**
  - En el POS, el campo de búsqueda está enfocado por defecto. Al escanear, el código "se teclea" y el Enter dispara la búsqueda por **código de barras exacto** → agrega la variante al ticket.
  - Para distinguir escaneo de tecleo manual, se puede detectar la **velocidad de entrada** (los lectores teclean muy rápido) y el Enter final. Interfaz `IBarcodeScanner`/manejador de entrada global opcional para capturar el escaneo aun sin foco en el campo.
- **Configuración física:** configurar el lector (con su hoja de códigos) para que agregue **sufijo Enter (CR/LF)**. Simbologías típicas para retail: **EAN-13, UPC-A, Code128**.

## Impresora de tickets (térmica ESC/POS)

- **Estándar:** casi todas las impresoras térmicas de tickets entienden **ESC/POS** (Epson TM-T20/T88, y muchas genéricas compatibles).
- **Conexión:** USB (lo más común), red o serial. Se instala como impresora de Windows.
- **Estrategia de impresión (dos opciones):**
  1. **Librería ESC/POS** (`ESCPOS_NET`): se construye el ticket con comandos (texto, negritas, alineación, tamaño doble para el total, código QR/de barras del folio, **corte de papel**) y se envía a la impresora. Máximo control del formato.
  2. **RAW al spooler de Windows**: enviar bytes ESC/POS directamente a la impresora por nombre (Win32 `WritePrinter`). Útil como respaldo.
- **Contenido del ticket:**
  - Encabezado: nombre de tienda (`test_tienda`), dirección, teléfono, RFC (opcional).
  - Folio, fecha/hora, cajero.
  - Líneas: cantidad × descripción … importe.
  - Subtotal, descuentos, impuesto (o "IVA incluido"), **TOTAL** (tamaño doble).
  - Método(s) de pago, efectivo recibido, **cambio**.
  - Pie: mensaje de agradecimiento / políticas.
  - **Corte automático** al final.
- **Interfaz:** `ITicketPrinter` en Application; `EscPosTicketPrinter` en Infrastructure. El ancho de columnas (típ. **42** a 80 mm / **32** a 58 mm) es configurable.

## Cajón de dinero

- **Cómo funciona:** el cajón normalmente se conecta a la **impresora de tickets** mediante un puerto **RJ11/RJ12 ("DK")**. Se abre enviando a la impresora el comando ESC/POS de **"drawer kick"** (`ESC p m t1 t2`).
- **Cuándo abrir:** al confirmar un cobro (configurable: siempre, solo efectivo, o manual con botón "Abrir cajón" bajo permiso).
- **Interfaz:** `ICashDrawer.AbrirAsync()` en Application; implementación envía el pulso vía la impresora en Infrastructure.
- **Nota:** si el cajón fuera de conexión directa (USB/serial propio) en lugar de vía impresora, se ajusta la implementación sin tocar el resto del sistema.

## Terminal de pago con tarjeta (fuera de alcance del MVP)

- El cobro con tarjeta se realiza en la **terminal bancaria física** por separado. En la app se **registra** el pago con método `Tarjeta` (y referencia/autorización opcional) para que el corte cuadre.
- La arquitectura deja lista una interfaz (`IPaymentTerminal`) por si en el futuro se integra una terminal con SDK.

## Configuración de periféricos en la app

En **Configuración → Hardware** (solo Administrador):
- Seleccionar impresora (lista de impresoras de Windows) y ancho de columnas.
- Probar impresión (ticket de prueba).
- Activar/desactivar apertura de cajón y su modo.
- Probar apertura de cajón.
- Nota informativa sobre configurar el lector con sufijo Enter.

## Checklist de puesta en marcha (en sitio)

1. Instalar **.NET 8 Desktop Runtime** (si la app es framework-dependent) o usar build self-contained.
2. Instalar la app (Inno Setup/Velopack).
3. Conectar e instalar la **impresora** en Windows; imprimir página de prueba.
4. Conectar el **cajón** a la impresora (cable DK).
5. Configurar el **lector** (sufijo Enter, simbologías).
6. En la app: seleccionar impresora, probar ticket, probar cajón, escanear un producto de prueba.
7. Configurar **respaldos** (carpeta local y, si aplica, carpeta en la nube).
8. Cargar catálogo inicial y stock; crear usuarios.
