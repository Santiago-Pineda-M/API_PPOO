namespace ApiPoo2.Application.UseCases.Documentos;

public sealed record UpdateDocumentoInputDto(
    Guid DocumentoId,
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);
