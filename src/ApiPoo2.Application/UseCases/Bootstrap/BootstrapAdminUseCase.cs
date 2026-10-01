using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Bootstrap;

/// <summary>
///     Crea el primer administrador del sistema. Solo funciona cuando no existe ninguna persona:
///     una vez creado el primero, el endpoint queda cerrado para siempre.
/// </summary>
public sealed class BootstrapAdminUseCase : BaseUseCase<BootstrapAdminInputDto, BootstrapAdminOutputDto>
{
    private const int MaxIntentosLogin = 100;

    private readonly IPersonaRepository _personaRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public BootstrapAdminUseCase(
        IEnumerable<IValidator<BootstrapAdminInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<BootstrapAdminOutputDto> ExecuteCoreAsync(
        BootstrapAdminInputDto request,
        CancellationToken cancellationToken)
    {
        if (await _personaRepository.AnyAsync(_ => true, cancellationToken))
        {
            throw new ForbiddenException(
                "bootstrap.disabled",
                "El sistema ya tiene un administrador inicializado.");
        }

        var now = _dateTimeProvider.UtcNow;
        var numero = NumeroIdentificacion.From(request.NumeroIdentificacion);

        if (await _personaRepository.ExistsByNumeroIdentificacionAsync(numero, cancellationToken))
        {
            throw new ConflictException(
                "persona.identificacion.conflict",
                "Ya existe una persona registrada con ese número de identificación.");
        }

        var persona = Persona.Register(
            request.TipoIdentificacion,
            numero.Value,
            request.Nombres,
            request.Apellidos,
            request.CorreoElectronico,
            TipoPersona.Administrativo,
            now);

        var passwordGenerada = GenerarPasswordTemporal();
        var sufijo = await ResolverSufijoLoginLibre(persona, cancellationToken);

        persona.CrearUsuario(_passwordHasher.Hash(passwordGenerada), now, sufijo);

        _personaRepository.Add(persona);

        if (persona.Usuario is { } usuario)
        {
            _userRepository.Add(usuario);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BootstrapAdminOutputDto(
            persona.Id,
            persona.Usuario!.Login.Value,
            passwordGenerada,
            persona.Usuario.ApiKey.Value);
    }

    private async Task<int> ResolverSufijoLoginLibre(Persona persona, CancellationToken cancellationToken)
    {
        for (var sufijo = 0; sufijo < MaxIntentosLogin; sufijo++)
        {
            var candidato = Login.FromMnemonic(
                persona.Nombres.Value,
                persona.Apellidos.Value,
                persona.NumeroIdentificacion,
                sufijo);

            if (!await _personaRepository.ExistsByLoginAsync(candidato, cancellationToken))
            {
                return sufijo;
            }
        }

        throw new ConflictException(
            "user.login.conflict",
            "No se pudo generar un login único para la persona.");
    }

    private static string GenerarPasswordTemporal() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
}
