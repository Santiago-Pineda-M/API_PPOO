using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.Get;

public sealed record GetRutaInputDto(string CodigoRuta);

public sealed class GetRutaUseCase : BaseUseCase<GetRutaInputDto, RutaConsultaOutputDto>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetRutaUseCase(
        IEnumerable<FluentValidation.IValidator<GetRutaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<RutaConsultaOutputDto> ExecuteCoreAsync(
        GetRutaInputDto request,
        CancellationToken cancellationToken)
    {
        var codigo = CodigoRuta.From(request.CodigoRuta);
        var ruta = await _rutaRepository.GetByCodigoAsync(codigo, cancellationToken)
            ?? throw new NotFoundException("ruta.not_found", "La ruta no existe.");

        return RutaConsultaOutputDto.From(ruta, _dateTimeProvider.UtcNow);
    }
}
