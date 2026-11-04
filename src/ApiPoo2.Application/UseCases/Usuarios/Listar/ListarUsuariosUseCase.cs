using ApiPoo2.Application.UseCases.Usuarios.Listar;

using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Usuarios.Listar;

public sealed class ListarUsuariosUseCase : BaseUseCase<ListarUsuariosInputDto, PagedResult<UsuarioOutputDto>>
{
    private readonly IPersonaRepository _personaRepository;

    public ListarUsuariosUseCase(
        IEnumerable<IValidator<ListarUsuariosInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
    }

    protected override async Task<PagedResult<UsuarioOutputDto>> ExecuteCoreAsync(
        ListarUsuariosInputDto request,
        CancellationToken cancellationToken)
    {
        var filtro = new PagedFilter(request.Page, request.PageSize);
        var (items, total) = await _personaRepository.GetPersonasConUsuarioPaginadoAsync(filtro, cancellationToken);

        var dtos = items.Select(UsuarioOutputDto.From).ToList();

        return new PagedResult<UsuarioOutputDto>(dtos, total, request.Page, request.PageSize);
    }
}
