using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Application.UseCases.Bootstrap;
using ApiPoo2.Application.UseCases.Cargues;
using ApiPoo2.Application.UseCases.Conductores;
using ApiPoo2.Application.UseCases.Consultas;
using ApiPoo2.Application.UseCases.Paradas.Create;
using ApiPoo2.Application.UseCases.Paradas.Delete;
using ApiPoo2.Application.UseCases.Paradas.Get;
using ApiPoo2.Application.UseCases.Paradas.List;
using ApiPoo2.Application.UseCases.Paradas.Update;
using ApiPoo2.Application.UseCases.Personas.Licencia;
using ApiPoo2.Application.UseCases.Rutas.Create;
using ApiPoo2.Application.UseCases.Rutas.Delete;
using ApiPoo2.Application.UseCases.Rutas.Get;
using ApiPoo2.Application.UseCases.Rutas.List;
using ApiPoo2.Application.UseCases.Rutas.PorConductor;
using ApiPoo2.Application.UseCases.Rutas.PorPlaca;
using ApiPoo2.Application.UseCases.Rutas.Update;
using ApiPoo2.Application.UseCases.TiposDocumento;
using ApiPoo2.Application.UseCases.Trayectos.Jobs;
using ApiPoo2.Application.UseCases.Personas;
using ApiPoo2.Application.UseCases.Personas.Create;
using ApiPoo2.Application.UseCases.Personas.Get;
using ApiPoo2.Application.UseCases.Personas.Update;
using ApiPoo2.Application.UseCases.Usuarios;
using ApiPoo2.Application.UseCases.Usuarios.AsignarRol;
using ApiPoo2.Application.UseCases.Usuarios.Crear;
using ApiPoo2.Application.UseCases.Usuarios.Delete;
using ApiPoo2.Application.UseCases.Usuarios.Get;
using ApiPoo2.Application.UseCases.Usuarios.Listar;
using ApiPoo2.Application.UseCases.Usuarios.Update;
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
        services.AddScoped<ListarUsuariosUseCase>();
        services.AddScoped<GetUsuarioUseCase>();
        services.AddScoped<CrearUsuarioUseCase>();
        services.AddScoped<UpdateUsuarioUseCase>();
        services.AddScoped<DeleteUsuarioUseCase>();
        services.AddScoped<AsignarRolUseCase>();

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

        // Rutas (E3)
        services.AddScoped<CreateRutaUseCase>();
        services.AddScoped<ListRutasUseCase>();
        services.AddScoped<ListRutasPorConductorUseCase>();
        services.AddScoped<ListRutasPorPlacaUseCase>();
        services.AddScoped<IValidator<ListRutasPorConductorInputDto>, ListRutasPorConductorValidator>();
        services.AddScoped<IValidator<ListRutasPorPlacaInputDto>, ListRutasPorPlacaValidator>();
        services.AddScoped<GetRutaUseCase>();
        services.AddScoped<UpdateRutaUseCase>();
        services.AddScoped<DeleteRutaUseCase>();

        // Paradas (subrecurso de ruta)
        services.AddScoped<ListParadasUseCase>();
        services.AddScoped<CreateParadaUseCase>();
        services.AddScoped<GetParadaUseCase>();
        services.AddScoped<UpdateParadaUseCase>();
        services.AddScoped<DeleteParadaUseCase>();

        // Licencia de conducción (subrecurso de persona)
        services.AddScoped<GetLicenciaConductorUseCase>();
        services.AddScoped<RegistrarLicenciaConductorUseCase>();
        services.AddScoped<EliminarLicenciaConductorUseCase>();

        // Cargue masivo E4
        services.AddScoped<CargarTrayectosExcelUseCase>();
        services.AddScoped<ListarCarguesUseCase>();
        services.AddScoped<GetCargueUseCase>();
        services.AddScoped<EliminarCargueUseCase>();

        // Tareas programadas
        services.AddScoped<ExpirarLicenciasConductoresUseCase>();
        services.AddScoped<MarcarDocumentosVencidosUseCase>();
        services.AddScoped<GeocodificarParadasUseCase>();

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
        services.AddScoped<IValidator<ListarUsuariosInputDto>, ListarUsuariosValidator>();
        services.AddScoped<IValidator<GetUsuarioInputDto>, GetUsuarioValidator>();
        services.AddScoped<IValidator<CrearUsuarioInputDto>, CrearUsuarioValidator>();
        services.AddScoped<IValidator<UpdateUsuarioInputDto>, UpdateUsuarioValidator>();
        services.AddScoped<IValidator<DeleteUsuarioInputDto>, DeleteUsuarioValidator>();
        services.AddScoped<IValidator<AsignarRolInputDto>, AsignarRolValidator>();
        services.AddScoped<IValidator<CreateVehiculoInputDto>, CreateVehiculoValidator>();
        services.AddScoped<IValidator<UpdateVehiculoInputDto>, UpdateVehiculoValidator>();
        services.AddScoped<IValidator<UpdateTipoDocumentoInputDto>, UpdateTipoDocumentoValidator>();
        services.AddScoped<IValidator<CreateRutaInputDto>, CreateRutaValidator>();
        services.AddScoped<IValidator<UpdateRutaInputDto>, UpdateRutaValidator>();
        services.AddScoped<IValidator<CreateParadaInputDto>, CreateParadaValidator>();
        services.AddScoped<IValidator<UpdateParadaInputDto>, UpdateParadaValidator>();
        services.AddScoped<IValidator<RegistrarLicenciaConductorInputDto>, RegistrarLicenciaConductorValidator>();

        return services;
    }
}
