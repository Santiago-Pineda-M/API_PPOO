namespace ApiPoo2.Application.IServices;

/// <summary>
///     Notificación opcional (CEREZA) al conductor restringido. La implementación por defecto solo
///     registra el evento en el log; no se acopla un servidor SMTP.
/// </summary>
public interface IEmailNotifier
{
    Task NotificarRestriccionAsync(string correo, string login, CancellationToken cancellationToken = default);
}
