using ApiPoo2.Application.UseCases.Usuarios.Crear;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios.Crear;

public sealed class CrearUsuarioUseCase : BaseUseCase<CrearUsuarioInputDto, UsuarioOutputDto>
{
    private const int MaxIntentosLogin = 100;

    private readonly IPersonaRepository _personaRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CrearUsuarioUseCase(
        IEnumerable<IValidator<CrearUsuarioInputDto>> validators,
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

    protected override async Task<UsuarioOutputDto> ExecuteCoreAsync(
        CrearUsuarioInputDto request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("persona.not_found", "La persona no existe.");

        if (!persona.PuedeTenerUsuario())
        {
            throw new DomainValidationException(
                "persona.usuario.no_admitido",
                [$"Una persona de tipo {persona.TipoPersona} no puede tener un usuario asociado."]);
        }

        if (persona.Usuario is not null)
        {
            throw new DomainValidationException(
                "persona.usuario.duplicado",
                ["La persona ya tiene un usuario asociado."]);
        }

        PasswordPolicy.EnsureValid(request.Password);

        var sufix = await ResolverSufixLoginLibre(persona, cancellationToken);
        var hash = _passwordHasher.Hash(request.Password);

        persona.CrearUsuario(hash, now, sufix);

        if (persona.Usuario is { } usuario)
        {
            _userRepository.Add(usuario);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UsuarioOutputDto.From(persona);
    }

    /// <summary>
    ///     La regla mnemotécnica puede colisionar entre homónimos. Se resuelve con un sufijo
    ///     incremental, igual que en <see cref="CreatePersonaUseCase.ResolverSufixLoginLibre" />.
    /// </summary>
    private async Task<int> ResolverSufixLoginLibre(Persona persona, CancellationToken cancellationToken)
    {
        for (var sufix = 0; sufix < MaxIntentosLogin; sufix++)
        {
            var candidato = Login.FromMnemonic(
                persona.Nombres.Value,
                persona.Apellidos.Value,
                persona.NumeroIdentificacion,
                sufix);

            if (!await _personaRepository.ExistsByLoginAsync(candidato, cancellationToken))
            {
                return sufix;
            }
        }

        throw new ConflictException(
            "user.login.conflict",
            "No se pudo generar un login único para la persona.");
    }
}
