using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Consultas;

// Consultas públicas del enunciado: no requieren token.

public sealed record GetConductoresOperablesInputDto(int Cantidad);

public sealed record ConductorOperableOutputDto(Guid PersonaId, string Nombres, string Apellidos, Guid VehiculoId);

public sealed class GetConductoresOperablesUseCase
    : BaseUseCase<GetConductoresOperablesInputDto, IReadOnlyList<ConductorOperableOutputDto>>
{
    private readonly IConductorVehiculoRepository _conductorRepository;

    public GetConductoresOperablesUseCase(
        IEnumerable<IValidator<GetConductoresOperablesInputDto>> validators,
        ILoggerFactory loggerFactory,
        IConductorVehiculoRepository conductorRepository)
        : base(validators, loggerFactory)
    {
        _conductorRepository = conductorRepository;
    }

    protected override async Task<IReadOnlyList<ConductorOperableOutputDto>> ExecuteCoreAsync(
        GetConductoresOperablesInputDto request,
        CancellationToken cancellationToken)
    {
        var operables = await _conductorRepository.GetOperablesAsync(cancellationToken);

        return operables
            .Select(c => new ConductorOperableOutputDto(
                c.PersonaId,
                c.Persona?.Nombres.Value ?? string.Empty,
                c.Persona?.Apellidos.Value ?? string.Empty,
                c.VehiculoId))
            .Take(request.Cantidad)
            .ToList();
    }
}

public sealed record GetDocumentosPorVencerInputDto(int Dias);

public sealed record DocumentoVehiculoVencimientoOutputDto(
    Guid VehiculoId,
    Guid TipoDocumentoId,
    DateTime FechaVencimiento,
    EstadoDocumento Estado);

public sealed class GetDocumentosPorVencerUseCase
    : BaseUseCase<GetDocumentosPorVencerInputDto, IReadOnlyList<DocumentoVehiculoVencimientoOutputDto>>
{
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDocumentosPorVencerUseCase(
        IEnumerable<IValidator<GetDocumentosPorVencerInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<IReadOnlyList<DocumentoVehiculoVencimientoOutputDto>> ExecuteCoreAsync(
        GetDocumentosPorVencerInputDto request,
        CancellationToken cancellationToken)
    {
        if (request.Dias <= 0)
        {
            throw new RequestValidationException(["El número de días debe ser mayor que cero."]);
        }

        var ahora = _dateTimeProvider.UtcNow;
        var documentos = await _documentoRepository.GetPorVencerAsync(ahora, request.Dias, cancellationToken);

        return documentos
            .Select(d => new DocumentoVehiculoVencimientoOutputDto(
                d.VehiculoId,
                d.TipoDocumentoId,
                d.FechaVencimiento,
                d.EstadoActual(ahora)))
            .OrderBy(d => d.FechaVencimiento)
            .ToList();
    }
}

public sealed record GetVehiculosDocumentosVencidosInputDto;

public sealed class GetVehiculosDocumentosVencidosUseCase
    : BaseUseCase<GetVehiculosDocumentosVencidosInputDto, IReadOnlyList<DocumentoVehiculoVencimientoOutputDto>>
{
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehiculosDocumentosVencidosUseCase(
        IEnumerable<IValidator<GetVehiculosDocumentosVencidosInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<IReadOnlyList<DocumentoVehiculoVencimientoOutputDto>> ExecuteCoreAsync(
        GetVehiculosDocumentosVencidosInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var documentos = await _documentoRepository.GetVencidosAsync(ahora, cancellationToken);

        return documentos
            .Select(d => new DocumentoVehiculoVencimientoOutputDto(
                d.VehiculoId,
                d.TipoDocumentoId,
                d.FechaVencimiento,
                EstadoDocumento.Vencido))
            .ToList();
    }
}

public sealed record CountPersonasByTipoInputDto;

public sealed record PersonaCountOutputDto(TipoPersona Tipo, int Cantidad);

public sealed class CountPersonasByTipoUseCase
    : BaseUseCase<CountPersonasByTipoInputDto, IReadOnlyList<PersonaCountOutputDto>>
{
    private readonly IPersonaRepository _personaRepository;

    public CountPersonasByTipoUseCase(
        IEnumerable<IValidator<CountPersonasByTipoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
    }

    protected override async Task<IReadOnlyList<PersonaCountOutputDto>> ExecuteCoreAsync(
        CountPersonasByTipoInputDto request,
        CancellationToken cancellationToken)
    {
        var conteo = await _personaRepository.CountByTipoAsync(cancellationToken);

        return Enum.GetValues<TipoPersona>()
            .Select(tipo => new PersonaCountOutputDto(tipo, conteo.GetValueOrDefault(tipo)))
            .ToList();
    }
}
