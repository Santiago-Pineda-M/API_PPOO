using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Rutas;

/// <summary>
///     Reglas del orden de parada: 0 es la inicial, el mayor es la final y solo caben cinco
///     intermedias, es decir, siete paradas por ruta.
/// </summary>
public static class ReglasParada
{
    public const int OrdenInicial = 0;

    public const int OrdenMaximo = 6;

    public const int MaximoParadasPorRuta = 7;

    public static bool EsValido(int orden) => orden is >= OrdenInicial and <= OrdenMaximo;

    public static void EnsureValid(int orden)
    {
        if (!EsValido(orden))
        {
            throw new DomainValidationException(
                "parada.orden.rango",
                [$"El orden de parada debe estar entre {OrdenInicial} y {OrdenMaximo}."]);
        }
    }
}
