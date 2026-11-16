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
public class ArtistaController : ApiControllerBase
{
    private readonly IArtistaRepository _artistaRepository;

    public ArtistaController(IArtistaRepository artistaRepository)
    {
        _artistaRepository = artistaRepository;
    }

    /// <summary>Lista os artistas em ordem de nome. "busca" filtra pelo nome.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca = null)
    {
        var artistas = await _artistaRepository.Listar(busca);
        return Ok(artistas.Select(ArtistaRespostaDTO.Converter));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var artista = await _artistaRepository.BuscarPorId(id);
        return artista is null
            ? Erro(404, "Artista não encontrado.")
            : Ok(ArtistaRespostaDTO.Converter(artista));
    }

    /// <summary>Cadastra um artista (RF16 / RN13).</summary>
    [HttpPost]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Cadastrar([FromBody] ArtistaDTO dto)
    {
        var nome = dto.Nome.Trim();
        if (await _artistaRepository.NomeEmUso(nome))
            return Erro(409, "Já existe um artista com esse nome.");

        var artista = new Artista { Nome = nome };

        try
        {
            await _artistaRepository.Cadastrar(artista);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um artista com esse nome.");
        }

        return CreatedAtAction(nameof(BuscarPorId), new { id = artista.IdArtista }, ArtistaRespostaDTO.Converter(artista));
    }

    /// <summary>Edita um artista (RF16 / RN13).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ArtistaDTO dto)
    {
        if (await _artistaRepository.BuscarPorId(id) is null) return Erro(404, "Artista não encontrado.");

        var nome = dto.Nome.Trim();
        if (await _artistaRepository.NomeEmUso(nome, id))
            return Erro(409, "Já existe um artista com esse nome.");

        try
        {
            await _artistaRepository.Atualizar(id, nome);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "Já existe um artista com esse nome.");
        }

        return NoContent();
    }
}
