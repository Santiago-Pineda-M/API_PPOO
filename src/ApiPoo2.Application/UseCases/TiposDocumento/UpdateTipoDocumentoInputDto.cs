namespace ApiPoo2.Application.UseCases.TiposDocumento;

public sealed record UpdateTipoDocumentoInputDto(
    Guid TipoDocumentoId,
    string Nombre,
    string TiposVehiculoAplicables,
    string CodigoObligatoriedad,
    string Descripcion);
