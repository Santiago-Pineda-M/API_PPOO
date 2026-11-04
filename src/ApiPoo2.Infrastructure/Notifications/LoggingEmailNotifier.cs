using ApiPoo2.Application.IServices;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Infrastructure.Notifications;

/// <summary>
///     Notificador por log. El enunciado menciona CEREZA como opcional; este adaptador permite
///     sustituirlo por un cliente SMTP/HTTP sin tocar los casos de uso.
/// </summary>
public sealed class LoggingEmailNotifier : IEmailNotifier
{
    private readonly ILogger<LoggingEmailNotifier> _logger;

    public LoggingEmailNotifier(ILogger<LoggingEmailNotifier> logger) => _logger = logger;

    public Task NotificarRestriccionAsync(
        string correo,
        string login,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Notificación de restricción para el conductor {Login} ({Correo}): su licencia venció y sus vehículos quedaron en estado RO.",
            login,
            correo);

        return Task.CompletedTask;
    }
}
