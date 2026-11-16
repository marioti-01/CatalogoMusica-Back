using CatalogoMusica.WebAPI.DTO;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Controllers;

/// <summary>
/// Gestão de usuários. Não existe cadastro público: só o administrador cadastra (RN03).
/// Usuários não são excluídos, são desativados (RF15).
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ApiControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioController(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    /// <summary>Lista os usuários cadastrados (RF12).</summary>
    [HttpGet]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await _usuarioRepository.Listar();
        return Ok(usuarios.Select(UsuarioRespostaDTO.Converter));
    }

    /// <summary>Dados do usuário logado.</summary>
    [HttpGet("perfil")]
    public async Task<IActionResult> MeuPerfil()
    {
        var usuario = await _usuarioRepository.BuscarPorId(IdUsuarioLogado);
        return usuario is null
            ? Erro(404, "Usuário não encontrado.")
            : Ok(UsuarioRespostaDTO.Converter(usuario));
    }

    /// <summary>Busca um usuário. Admin vê qualquer um; usuário comum, apenas a si mesmo.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        if (!EhAdmin && id != IdUsuarioLogado) return Forbid();

        var usuario = await _usuarioRepository.BuscarPorId(id);
        return usuario is null
            ? Erro(404, "Usuário não encontrado.")
            : Ok(UsuarioRespostaDTO.Converter(usuario));
    }

    /// <summary>Cadastra um novo usuário (RF13).</summary>
    [HttpPost]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Cadastrar([FromBody] UsuarioCadastroDTO dto)
    {
        var email = dto.Email.Trim();
        if (await _usuarioRepository.EmailEmUso(email))
            return Erro(409, "Já existe um usuário com esse e-mail."); // RN01

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Senha = dto.Senha,
            TipoUsuario = dto.TipoUsuario,
            Ativo = true
        };

        try
        {
            await _usuarioRepository.Cadastrar(usuario);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um usuário com esse e-mail.");
        }

        return CreatedAtAction(nameof(BuscarPorId), new { id = usuario.IdUsuario }, UsuarioRespostaDTO.Converter(usuario));
    }

    /// <summary>
    /// Cadastro público: qualquer pessoa cria a própria conta, sem estar logada.
    /// A conta é sempre do tipo Comum; só um admin pode promover alguém a Admin.
    /// </summary>
    [HttpPost("cadastro")]
    [AllowAnonymous]
    public async Task<IActionResult> CadastroPublico([FromBody] CadastroPublicoDTO dto)
    {
        var email = dto.Email.Trim();
        if (await _usuarioRepository.EmailEmUso(email))
            return Erro(409, "Já existe uma conta com esse e-mail."); // RN01

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Senha = dto.Senha,
            TipoUsuario = TiposUsuario.Comum,
            Ativo = true
        };

        try
        {
            await _usuarioRepository.Cadastrar(usuario);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe uma conta com esse e-mail.");
        }

        return StatusCode(201, UsuarioRespostaDTO.Converter(usuario));
    }

    /// <summary>Edita um usuário (RF14). Senha em branco mantém a atual.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioAtualizacaoDTO dto)
    {
        var atual = await _usuarioRepository.BuscarPorId(id);
        if (atual is null) return Erro(404, "Usuário não encontrado.");

        if (id == IdUsuarioLogado && dto.TipoUsuario != TiposUsuario.Admin)
            return Erro(400, "Você não pode remover o seu próprio perfil de administrador.");

        var email = dto.Email.Trim();
        if (await _usuarioRepository.EmailEmUso(email, id))
            return Erro(409, "Já existe um usuário com esse e-mail.");

        try
        {
            await _usuarioRepository.Atualizar(id, new Usuario
            {
                Nome = dto.Nome.Trim(),
                Email = email,
                Senha = dto.Senha ?? string.Empty,
                TipoUsuario = dto.TipoUsuario,
                Ativo = atual.Ativo
            });
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um usuário com esse e-mail.");
        }

        return NoContent();
    }

    /// <summary>Edita o próprio perfil (RF07 / RN09).</summary>
    [HttpPut("perfil")]
    public async Task<IActionResult> AtualizarPerfil([FromBody] PerfilAtualizacaoDTO dto)
    {
        var id = IdUsuarioLogado;
        var atual = await _usuarioRepository.BuscarPorId(id);
        if (atual is null) return Erro(404, "Usuário não encontrado.");

        if (!string.IsNullOrEmpty(dto.NovaSenha) && !Criptografia.CompararHash(dto.SenhaAtual ?? string.Empty, atual.Senha))
            return Erro(400, "A senha atual está incorreta.");

        var email = dto.Email.Trim();
        if (await _usuarioRepository.EmailEmUso(email, id))
            return Erro(409, "Já existe um usuário com esse e-mail.");

        try
        {
            await _usuarioRepository.Atualizar(id, new Usuario
            {
                Nome = dto.Nome.Trim(),
                Email = email,
                Senha = dto.NovaSenha ?? string.Empty,
                TipoUsuario = atual.TipoUsuario,
                Ativo = atual.Ativo
            });
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um usuário com esse e-mail.");
        }

        return NoContent();
    }

    /// <summary>Desativa um usuário: ele não consegue mais acessar o sistema (RF15 / RN04).</summary>
    [HttpPatch("{id:int}/desativar")]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Desativar(int id)
    {
        if (id == IdUsuarioLogado) return Erro(400, "Você não pode desativar a sua própria conta.");
        if (await _usuarioRepository.BuscarPorId(id) is null) return Erro(404, "Usuário não encontrado.");

        await _usuarioRepository.AlterarStatus(id, false);
        return NoContent();
    }

    /// <summary>Reativa um usuário desativado.</summary>
    [HttpPatch("{id:int}/ativar")]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Ativar(int id)
    {
        if (await _usuarioRepository.BuscarPorId(id) is null) return Erro(404, "Usuário não encontrado.");

        await _usuarioRepository.AlterarStatus(id, true);
        return NoContent();
    }
}
