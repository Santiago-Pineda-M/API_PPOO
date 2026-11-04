using ApiPoo2.Application.UseCases.Rutas;
using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Paradas.List;

public sealed record ListParadasInputDto(string CodigoRuta);

public sealed class ListParadasUseCase
    : BaseUseCase<ListParadasInputDto, IReadOnlyList<ParadaOutputDto>>
{
    private readonly IRutaRepository _rutaRepository;

    public ListParadasUseCase(
        IEnumerable<FluentValidation.IValidator<ListParadasInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
    }

    protected override async Task<IReadOnlyList<ParadaOutputDto>> ExecuteCoreAsync(
        ListParadasInputDto request,
        CancellationToken cancellationToken)
    {
        var ruta = await _rutaRepository.GetByCodigoAsync(
            CodigoRuta.From(request.CodigoRuta),
            cancellationToken)
            ?? throw new NotFoundException("ruta.not_found", "La ruta no existe.");

        return ruta.Paradas
            .OrderBy(p => p.OrdenParada)
            .Select(ParadaOutputDto.From)
            .ToList();
    }
}
