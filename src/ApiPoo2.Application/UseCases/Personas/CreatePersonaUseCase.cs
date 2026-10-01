using ApiPoo2.Application.UseCases.Personas.Create;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Personas;

public sealed class CreatePersonaUseCase : BaseUseCase<CreatePersonaInputDto, CreatePersonaOutputDto>
{
    private const int MaxIntentosLogin = 100;

    private readonly IPersonaRepository _personaRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePersonaUseCase(
        IEnumerable<IValidator<CreatePersonaInputDto>> validators,
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

    protected override async Task<CreatePersonaOutputDto> ExecuteCoreAsync(
        CreatePersonaInputDto request,
        CancellationToken cancellationToken)
    {
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
            request.TipoPersona,
            now);

        _personaRepository.Add(persona);

        string? passwordGenerada = null;

        if (persona.PuedeTenerUsuario())
        {
            passwordGenerada = GenerarPasswordTemporal();

            var sufiix = await ResolverSufixLoginLibre(persona, cancellationToken);

            persona.CrearUsuario(_passwordHasher.Hash(passwordGenerada), now, sufiix);

            if (persona.Usuario is { } usuario)
            {
                _userRepository.Add(usuario);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatePersonaOutputDto.From(persona, passwordGenerada);
    }

    /// <summary>
    ///     La regla mnemotécnica puede colisionar entre homónimos. Se resuelve con un sufijo
    ///     incremental, que es la convención que quedó documentada.
    /// </summary>
    private async Task<int> ResolverSufixLoginLibre(Persona persona, CancellationToken cancellationToken)
    {
        for (var sufiix = 0; sufiix < MaxIntentosLogin; sufiix++)
        {
            var candidato = Login.FromMnemonic(
                persona.Nombres.Value,
                persona.Apellidos.Value,
                persona.NumeroIdentificacion,
                sufiix);

            if (!await _personaRepository.ExistsByLoginAsync(candidato, cancellationToken))
            {
                return sufiix;
            }
        }

        throw new ConflictException(
            "user.login.conflict",
            "No se pudo generar un login único para la persona.");
    }

    private static string GenerarPasswordTemporal() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
}
