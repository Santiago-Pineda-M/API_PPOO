using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Bootstrap;
using ApiPoo2.Application.UseCases.Conductores;
using ApiPoo2.Application.UseCases.Consultas;
using ApiPoo2.Application.UseCases.TiposDocumento;
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

        // Bootstrap del primer administrador (solo con base vacía).
        services.AddScoped<BootstrapAdminUseCase>();

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
        services.AddScoped<GetVehiculosByTipoDocumentoUseCase>();
        services.AddScoped<GetVehiculosByEstadoDocumentoUseCase>();

        // Documentos
        services.AddScoped<RegisterTipoDocumentoUseCase>();
        services.AddScoped<GetTipoDocumentoUseCase>();
        services.AddScoped<GetTiposDocumentoUseCase>();
        services.AddScoped<UpdateTipoDocumentoUseCase>();
        services.AddScoped<DeleteTipoDocumentoUseCase>();
        services.AddScoped<ChangeTipoDocumentoEstadoUseCase>();
        services.AddScoped<UploadDocumentoVehiculoUseCase>();

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
        services.AddScoped<IValidator<BootstrapAdminInputDto>, BootstrapAdminValidator>();
        services.AddScoped<IValidator<UpdatePersonaInputDto>, UpdatePersonaValidator>();
        services.AddScoped<IValidator<ChangeUserPasswordInputDto>, ChangeUserPasswordValidator>();
        services.AddScoped<IValidator<CreateVehiculoInputDto>, CreateVehiculoValidator>();
        services.AddScoped<IValidator<UpdateVehiculoInputDto>, UpdateVehiculoValidator>();
        services.AddScoped<IValidator<UpdateTipoDocumentoInputDto>, UpdateTipoDocumentoValidator>();

        return services;
    }
}
