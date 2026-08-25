namespace Usashopp.Pos.Wpf.Common;

/// <summary>Se emite cuando cambia el estado de la caja (apertura/cierre), para que
/// otras vistas (como la barra superior del shell) se actualicen.</summary>
public sealed record CajaEstadoCambiadoMessage;

/// <summary>Se emite al cerrar sesión para que la app vuelva a la pantalla de login
/// sin reiniciar el proceso.</summary>
public sealed record CerrarSesionMessage;
