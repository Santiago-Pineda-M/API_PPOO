using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.DTOs;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Entities;
using ApiPoo2.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Auth.Register;

public sealed class RegisterUseCase : BaseUseCase<RegisterInputDto, RegisterUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUseCase(
        IEnumerable<IValidator<RegisterInputDto>> validators,
        ILoggerFactory loggerFactory,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<RegisterUserDto> ExecuteCoreAsync(RegisterInputDto request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException("email.conflict", "Ya existe una cuenta con ese correo electrónico.");
        }

        PasswordPolicy.EnsureValid(request.Password);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Register(
            email,
            passwordHash,
            UserRole.Member,
            _dateTimeProvider.UtcNow);

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RegisterUserDto.From(user);
    }
}