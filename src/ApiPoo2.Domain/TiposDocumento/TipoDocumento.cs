using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.TiposDocumento;

/// <summary>
///     Entidad paramétrica: catálogo de documentos que pueden asociarse a vehículos. No guarda el
///     archivo; el contenido vive en <see cref="DocumentoVehiculo" />.
/// </summary>
public sealed class TipoDocumento : BaseEntity
{
    public TipoDocumentoCodigo Codigo { get; private set; } = null!;

    public TipoDocumentoNombre Nombre { get; private set; } = null!;

    public TiposVehiculoAplicables TiposVehiculoAplicables { get; private set; } = null!;

    public CodigoObligatoriedad CodigoObligatoriedad { get; private set; } = null!;

    public TipoDocumentoDescripcion Descripcion { get; private set; } = null!;

    private TipoDocumento()
    {
    }

    public static TipoDocumento Register(
        string codigo,
        string nombre,
        string tiposVehiculoAplicables,
        string codigoObligatoriedad,
        string descripcion,
        DateTime utcNow)
    {
        var documento = new TipoDocumento
        {
            Codigo = TipoDocumentoCodigo.From(codigo),
            Nombre = TipoDocumentoNombre.From(nombre),
            TiposVehiculoAplicables = TiposVehiculoAplicables.From(tiposVehiculoAplicables),
            CodigoObligatoriedad = CodigoObligatoriedad.From(codigoObligatoriedad),
            Descripcion = TipoDocumentoDescripcion.From(descripcion),
        };

        documento.Initialize(Guid.NewGuid(), utcNow);
        return documento;
    }

    public bool AplicaA(TipoVehiculo tipoVehiculo) => TiposVehiculoAplicables.AplicaA(tipoVehiculo);

    public bool EsObligatorioPara(TipoVehiculo tipoVehiculo)
        => CodigoObligatoriedad.EsObligatorioPara(tipoVehiculo);

    /// <summary>Actualiza un documento paramétrico sin cambiar su código, que es su identidad funcional.</summary>
    public void Actualizar(
        string nombre,
        string tiposVehiculoAplicables,
        string codigoObligatoriedad,
        string descripcion,
        DateTime utcNow)
    {
        Nombre = TipoDocumentoNombre.From(nombre);
        TiposVehiculoAplicables = TiposVehiculoAplicables.From(tiposVehiculoAplicables);
        CodigoObligatoriedad = CodigoObligatoriedad.From(codigoObligatoriedad);
        Descripcion = TipoDocumentoDescripcion.From(descripcion);
        MarkUpdated(utcNow);
    }
}