using CatalogoMusica.WebAPI.DTO;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GeneroController : ApiControllerBase
{
    private readonly IGeneroRepository _generoRepository;

    public GeneroController(IGeneroRepository generoRepository)
    {
        _generoRepository = generoRepository;
    }

    /// <summary>Lista todos os gêneros em ordem de nome (usado no filtro do RF04).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var generos = await _generoRepository.Listar();
        return Ok(generos.Select(GeneroRespostaDTO.Converter));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var genero = await _generoRepository.BuscarPorId(id);
        return genero is null
            ? Erro(404, "Gênero não encontrado.")
            : Ok(GeneroRespostaDTO.Converter(genero));
    }

    /// <summary>Cadastra um gênero (RF11 / RN11).</summary>
    [HttpPost]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Cadastrar([FromBody] GeneroDTO dto)
    {
        var nome = dto.Nome.Trim();
        if (await _generoRepository.NomeEmUso(nome))
            return Erro(409, "Já existe um gênero com esse nome.");

        var genero = new Genero { Nome = nome };

        try
        {
            await _generoRepository.Cadastrar(genero);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um gênero com esse nome.");
        }

        return CreatedAtAction(nameof(BuscarPorId), new { id = genero.IdGenero }, GeneroRespostaDTO.Converter(genero));
    }

    /// <summary>Edita um gênero (RF11 / RN11).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] GeneroDTO dto)
    {
        if (await _generoRepository.BuscarPorId(id) is null) return Erro(404, "Gênero não encontrado.");

        var nome = dto.Nome.Trim();
        if (await _generoRepository.NomeEmUso(nome, id))
            return Erro(409, "Já existe um gênero com esse nome.");

        try
        {
            await _generoRepository.Atualizar(id, nome);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um gênero com esse nome.");
        }

        return NoContent();
    }
}
