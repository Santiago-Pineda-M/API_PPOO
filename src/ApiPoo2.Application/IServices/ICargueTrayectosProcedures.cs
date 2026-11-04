namespace ApiPoo2.Application.IServices;

public sealed record ResumenCargue(int Total, int Validados, int Procesados, int Errores);

/// <summary>
///     Invoca los tres procedimientos almacenados del cargue (cargue, validación y procesado) en
///     una sola transacción, como exige el enunciado de la entrega 4.
/// </summary>
public interface ICargueTrayectosProcedures
{
    Task<ResumenCargue> EjecutarAsync(
        long idCargue,
        IReadOnlyList<FilaTrayectoExcel> filas,
        CancellationToken cancellationToken = default);
}
