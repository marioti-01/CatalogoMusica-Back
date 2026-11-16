using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CatalogoMusica.WebAPI.DTO;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Services;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CatalogoMusica.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ApiControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenRevogadoService _tokensRevogados;
    private readonly JwtSettings _jwtSettings;

    public LoginController(IUsuarioRepository usuarioRepository, ITokenRevogadoService tokensRevogados, JwtSettings jwtSettings)
    {
        _usuarioRepository = usuarioRepository;
        _tokensRevogados = tokensRevogados;
        _jwtSettings = jwtSettings;
    }

    /// <summary>Autentica por e-mail e senha e devolve o token JWT (RF01).</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Entrar([FromBody] LoginDTO dto)
    {
        var usuario = await _usuarioRepository.BuscarPorEmailESenha(dto.Email.Trim(), dto.Senha);
        if (usuario is null) return Erro(401, "E-mail ou senha inválidos.");
        if (!usuario.Ativo) return Erro(403, "Usuário desativado. Procure um administrador."); // RN04

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.TipoUsuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiracao = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiracao,
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiracaoUtc = expiracao,
            usuario = UsuarioRespostaDTO.Converter(usuario)
        });
    }

    /// <summary>Encerra a sessão: o token atual deixa de ser aceito (RF02).</summary>
    [HttpPost("sair")]
    public IActionResult Sair()
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var exp = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (!string.IsNullOrEmpty(jti) && long.TryParse(exp, out var segundos))
            _tokensRevogados.Revogar(jti, DateTimeOffset.FromUnixTimeSeconds(segundos));

        return NoContent();
    }
}
