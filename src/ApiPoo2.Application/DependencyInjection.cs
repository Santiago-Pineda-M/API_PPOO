using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Conductores;
using ApiPoo2.Application.UseCases.Consultas;
using ApiPoo2.Application.UseCases.Documentos;
using ApiPoo2.Application.UseCases.Personas;
using ApiPoo2.Application.UseCases.Personas.Create;
using ApiPoo2.Application.UseCases.Personas.Get;
using ApiPoo2.Application.UseCases.Personas.Update;
using ApiPoo2.Application.UseCases.Usuarios;
using ApiPoo2.Application.UseCases.Vehiculos;
using ApiPoo2.Application.UseCases.Vehiculos.Consultas;
using ApiPoo2.Application.UseCases.Vehiculos.Delete;
using ApiPoo2.Application.UseCases.Vehiculos.Update;
using ApiPoo2.Application.UseCases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPoo2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<ChangePasswordUseCase>();
        services.AddScoped<RevokeRefreshTokenUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();

        // Personas
        services.AddScoped<CreatePersonaUseCase>();
        services.AddScoped<GetPersonaUseCase>();
        services.AddScoped<UpdatePersonaUseCase>();

        // Usuarios
        services.AddScoped<ChangeUserPasswordUseCase>();
        services.AddScoped<RegenerateApiKeyUseCase>();

        // Vehiculos
        services.AddScoped<CreateVehiculoUseCase>();
        services.AddScoped<GetVehiculoByPlacaUseCase>();
        services.AddScoped<GetVehiculoByIdUseCase>();
        services.AddScoped<UpdateVehiculoUseCase>();
        services.AddScoped<DeleteVehiculoUseCase>();
        services.AddScoped<GetVehiculosByTipoUseCase>();
        services.AddScoped<GetVehiculosByDocumentoUseCase>();
        services.AddScoped<GetVehiculosByEstadoDocumentoUseCase>();

        // Documentos
        services.AddScoped<RegisterDocumentoUseCase>();
        services.AddScoped<GetDocumentoUseCase>();
        services.AddScoped<GetDocumentosUseCase>();
        services.AddScoped<UpdateDocumentoUseCase>();
        services.AddScoped<DeleteDocumentoUseCase>();
        services.AddScoped<ChangeDocumentoEstadoUseCase>();
        services.AddScoped<UploadDocumentosUseCase>();

        // Conductores
        services.AddScoped<AssociateVehiculosUseCase>();
        services.AddScoped<ChangeConductorEstadoUseCase>();

        // Consultas públicas
        services.AddScoped<GetConductoresOperablesUseCase>();
        services.AddScoped<GetDocumentosPorVencerUseCase>();
        services.AddScoped<GetVehiculosDocumentosVencidosUseCase>();
        services.AddScoped<CountPersonasByTipoUseCase>();

        // Validators
        services.AddScoped<IValidator<LoginInputDto>, LoginValidator>();
        services.AddScoped<IValidator<RefreshTokenInputDto>, RefreshTokenValidator>();
        services.AddScoped<IValidator<ChangePasswordInputDto>, ChangePasswordValidator>();
        services.AddScoped<IValidator<CreatePersonaInputDto>, CreatePersonaValidator>();
        services.AddScoped<IValidator<UpdatePersonaInputDto>, UpdatePersonaValidator>();
        services.AddScoped<IValidator<ChangeUserPasswordInputDto>, ChangeUserPasswordValidator>();
        services.AddScoped<IValidator<CreateVehiculoInputDto>, CreateVehiculoValidator>();
        services.AddScoped<IValidator<UpdateVehiculoInputDto>, UpdateVehiculoValidator>();
        services.AddScoped<IValidator<UpdateDocumentoInputDto>, UpdateDocumentoValidator>();

        return services;
    }
}
