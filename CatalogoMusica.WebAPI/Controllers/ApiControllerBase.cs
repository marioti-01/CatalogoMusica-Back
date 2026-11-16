using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CatalogoMusica.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogoMusica.WebAPI.Controllers;

/// <summary>
/// Base de todos os controllers. O [Authorize] aqui faz com que toda rota exija login (RN12),
/// exceto as marcadas com [AllowAnonymous] (apenas o login).
/// </summary>
[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id do usuário logado, lido do token JWT.</summary>
    protected int IdUsuarioLogado => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    protected bool EhAdmin => User.IsInRole(TiposUsuario.Admin);

    /// <summary>Resposta de erro padronizada: { "mensagem": "..." } (RNF07).</summary>
    protected ObjectResult Erro(int statusCode, string mensagem) =>
        StatusCode(statusCode, new { mensagem });
}
