using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed record RegisterTipoDocumentoInputDto(
    string Codigo,
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);

public sealed record TipoDocumentoOutputDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);

/// <summary>CRUD de la entidad paramétrica de documentos (catálogo de tipos).</summary>
public sealed class RegisterTipoDocumentoUseCase : BaseUseCase<RegisterTipoDocumentoInputDto, TipoDocumentoOutputDto>
{
    private readonly ITipoDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterTipoDocumentoUseCase(
        IEnumerable<IValidator<RegisterTipoDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITipoDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<TipoDocumentoOutputDto> ExecuteCoreAsync(
        RegisterTipoDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var codigo = TipoDocumentoCodigo.From(request.Codigo);

        if (await _documentoRepository.GetByCodigoAsync(codigo, cancellationToken) is not null)
        {
            throw new ConflictException("document.code.conflict", "Ya existe un documento con ese código.");
        }

        var documento = TipoDocumento.Register(
            codigo.Value,
            request.Nombre,
            request.TiposVehiculoAplicables,
            request.CodigoObligatoriedad,
            request.Descripcion,
            _dateTimeProvider.UtcNow);

        _documentoRepository.Add(documento);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TipoDocumentoOutputDto(
            documento.Id,
            documento.Codigo.Value,
            documento.Nombre.Value,
            documento.TiposVehiculoAplicables.Value,
            documento.CodigoObligatoriedad.Value,
            documento.Descripcion.Value);
    }
}
