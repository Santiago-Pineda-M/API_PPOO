using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Documentos;

public sealed record RegisterDocumentoInputDto(
    string Codigo,
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);

public sealed record DocumentoParametricoOutputDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);

/// <summary>CRUD de la entidad paramétrica de documentos (catálogo de tipos).</summary>
public sealed class RegisterDocumentoUseCase : BaseUseCase<RegisterDocumentoInputDto, DocumentoParametricoOutputDto>
{
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDocumentoUseCase(
        IEnumerable<IValidator<RegisterDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IDocumentoRepository documentoRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<DocumentoParametricoOutputDto> ExecuteCoreAsync(
        RegisterDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var codigo = DocumentoCodigo.From(request.Codigo);

        if (await _documentoRepository.GetByCodigoAsync(codigo, cancellationToken) is not null)
        {
            throw new ConflictException("document.code.conflict", "Ya existe un documento con ese código.");
        }

        var documento = Documento.Register(
            codigo.Value,
            request.Nombre,
            request.TiposVehiculoAplicables,
            request.CodigoObligatoriedad,
            request.Descripcion,
            _dateTimeProvider.UtcNow);

        _documentoRepository.Add(documento);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DocumentoParametricoOutputDto(
            documento.Id,
            documento.Codigo.Value,
            documento.Nombre.Value,
            documento.TiposVehiculoAplicables.Value,
            documento.CodigoObligatoriedad.Value,
            documento.Descripcion.Value);
    }
}
