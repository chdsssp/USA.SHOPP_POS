namespace Usashopp.Pos.Wpf.Common;

/// <summary>Se emite cuando cambia el estado de la caja (apertura/cierre), para que
/// otras vistas (como la barra superior del shell) se actualicen.</summary>
public sealed record CajaEstadoCambiadoMessage;
