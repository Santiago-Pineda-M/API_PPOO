using ApiPoo2.Application.DTOs;
using ApiPoo2.Application.UseCases;
using ApiPoo2.Application.UseCases.Auth.ChangePassword;
using ApiPoo2.Application.UseCases.Auth.GetCurrentUser;
using ApiPoo2.Application.UseCases.Auth.Login;
using ApiPoo2.Application.UseCases.Auth.Logout;
using ApiPoo2.Application.UseCases.Auth.RefreshToken;
using ApiPoo2.Application.UseCases.Auth.Register;
using ApiPoo2.Application.UseCases.Auth.RevokeRefreshToken;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPoo2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<ChangePasswordUseCase>();
        services.AddScoped<RevokeRefreshTokenUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();

        services.AddScoped<IValidator<RegisterInputDto>, RegisterValidator>();
        services.AddScoped<IValidator<LoginInputDto>, LoginValidator>();
        services.AddScoped<IValidator<RefreshTokenInputDto>, RefreshTokenValidator>();
        services.AddScoped<IValidator<ChangePasswordInputDto>, ChangePasswordValidator>();

        return services;
    }
}