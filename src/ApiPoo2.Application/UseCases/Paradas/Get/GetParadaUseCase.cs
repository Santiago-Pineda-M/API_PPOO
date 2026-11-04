using ApiPoo2.Application.UseCases.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Paradas.Get;

public sealed record GetParadaInputDto(Guid ParadaId);

public sealed class GetParadaUseCase : BaseUseCase<GetParadaInputDto, ParadaOutputDto>
{
    private readonly IRutaRepository _rutaRepository;

    public GetParadaUseCase(
        IEnumerable<FluentValidation.IValidator<GetParadaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
    }

    protected override async Task<ParadaOutputDto> ExecuteCoreAsync(
        GetParadaInputDto request,
        CancellationToken cancellationToken)
    {
        var ruta = await _rutaRepository.GetByParadaAsync(request.ParadaId, cancellationToken)
            ?? throw new NotFoundException("parada.not_found", "La parada no existe.");

        var parada = ruta.BuscarParada(request.ParadaId)
            ?? throw new NotFoundException("parada.not_found", "La parada no existe.");

        return ParadaOutputDto.From(parada);
    }
}
