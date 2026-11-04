using System.Data;
using System.Globalization;
using System.Text.Json;
using ApiPoo2.Application.IServices;
using ApiPoo2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace ApiPoo2.Infrastructure.Persistence.Procedures;

/// <summary>
///     Ejecuta los tres procedimientos almacenados del cargue (cargue, validación y procesado) en una
///     sola transacción. La transformación y la validación estructural ya las hizo el caso de uso; acá
///     solo se invoca el trabajo masivo que exige la entrega 4.
/// </summary>
public sealed class CargueTrayectosProcedures : ICargueTrayectosProcedures
{
    private readonly AppDbContext _dbContext;

    public CargueTrayectosProcedures(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<ResumenCargue> EjecutarAsync(
        long idCargue,
        IReadOnlyList<FilaTrayectoExcel> filas,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(filas.Select(f => new
        {
            codigo_ruta = f.CodigoRuta,
            identificacion_conductor = f.IdentificacionConductor,
            placa_vehiculo = f.PlacaVehiculo,
            ubicacion = f.Ubicacion,
            orden_parada = f.OrdenParada,
            latitud = f.Latitud,
            longitud = f.Longitud,
            login_usuario = f.LoginUsuario,
        }));

        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var dbTransaction = (NpgsqlTransaction)transaction.GetDbTransaction();

        try
        {
            var total = await EjecutarCargueAsync(
                connection,
                dbTransaction,
                idCargue,
                json,
                cancellationToken);

            var (validados, errores) = await EjecutarValidacionAsync(
                connection,
                dbTransaction,
                idCargue,
                cancellationToken);

            var (procesados, rechazados) = await EjecutarProcesadoAsync(
                connection,
                dbTransaction,
                idCargue,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new ResumenCargue(total, validados, procesados, errores + rechazados);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<int> EjecutarCargueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long idCargue,
        string json,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT sp_trayecto_cargue(@id_cargue, @filas::jsonb)";
        command.Parameters.Add(new NpgsqlParameter("id_cargue", NpgsqlDbType.Bigint) { Value = idCargue });
        command.Parameters.Add(new NpgsqlParameter("filas", NpgsqlDbType.Jsonb) { Value = json });

        var resultado = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(resultado, CultureInfo.InvariantCulture);
    }

    private static async Task<(int Validados, int Errores)> EjecutarValidacionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long idCargue,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT validados, errores FROM sp_trayecto_validar(@id_cargue)";
        command.Parameters.Add(new NpgsqlParameter("id_cargue", NpgsqlDbType.Bigint) { Value = idCargue });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 0);
        }

        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    private static async Task<(int Procesados, int Rechazados)> EjecutarProcesadoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long idCargue,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT procesados, rechazados FROM sp_trayecto_procesar(@id_cargue)";
        command.Parameters.Add(new NpgsqlParameter("id_cargue", NpgsqlDbType.Bigint) { Value = idCargue });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 0);
        }

        return (reader.GetInt32(0), reader.GetInt32(1));
    }
}
