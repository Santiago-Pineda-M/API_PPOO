using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.Documentos;

/// <summary>
///     Entidad paramétrica: catálogo de documentos que pueden asociarse a vehículos. No guarda el
///     archivo; el contenido vive en <see cref="VehiculoDocumento" />.
/// </summary>
public sealed class Documento : BaseEntity
{
    public DocumentoCodigo Codigo { get; private set; } = null!;

    public DocumentoNombre Nombre { get; private set; } = null!;

    public TiposVehiculoAplicables TiposVehiculoAplicables { get; private set; } = null!;

    public CodigoObligatoriedad CodigoObligatoriedad { get; private set; } = null!;

    public DocumentoDescripcion Descripcion { get; private set; } = null!;

    private Documento()
    {
    }

    public static Documento Register(
        string codigo,
        string nombre,
        string tiposVehiculoAplicables,
        string codigoObligatoriedad,
        string descripcion,
        DateTime utcNow)
    {
        var documento = new Documento
        {
            Codigo = DocumentoCodigo.From(codigo),
            Nombre = DocumentoNombre.From(nombre),
            TiposVehiculoAplicables = TiposVehiculoAplicables.From(tiposVehiculoAplicables),
            CodigoObligatoriedad = CodigoObligatoriedad.From(codigoObligatoriedad),
            Descripcion = DocumentoDescripcion.From(descripcion),
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
        Nombre = DocumentoNombre.From(nombre);
        TiposVehiculoAplicables = TiposVehiculoAplicables.From(tiposVehiculoAplicables);
        CodigoObligatoriedad = CodigoObligatoriedad.From(codigoObligatoriedad);
        Descripcion = DocumentoDescripcion.From(descripcion);
        MarkUpdated(utcNow);
    }
}