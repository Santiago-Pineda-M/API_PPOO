namespace ApiPoo2.Application.IServices;

public sealed class CargueOptions
{
    public const string SectionName = "Trayectos:Cargue";

    public int TamanioMaximoMb { get; set; } = 5;

    public int MaxFilas { get; set; } = 5000;
}
