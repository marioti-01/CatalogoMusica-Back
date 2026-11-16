using CatalogoMusica.WebAPI.DTO;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Controllers;

/// <summary>
/// Playlists do usuário logado. Só o dono vê, edita ou exclui a playlist (RN14):
/// a playlist de outra pessoa responde 404, como se não existisse.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PlaylistController : ApiControllerBase
{
    private const string NaoEncontrada = "Playlist não encontrada.";
    private const string NomeDuplicado = "Você já possui uma playlist com esse nome.";

    private readonly IPlaylistRepository _playlistRepository;
    private readonly IMusicaRepository _musicaRepository;

    public PlaylistController(IPlaylistRepository playlistRepository, IMusicaRepository musicaRepository)
    {
        _playlistRepository = playlistRepository;
        _musicaRepository = musicaRepository;
    }

    /// <summary>Lista as playlists do usuário logado (RF18).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var playlists = await _playlistRepository.ListarDoUsuario(IdUsuarioLogado);
        return Ok(playlists.Select(PlaylistResumoDTO.Converter));
    }

    /// <summary>Detalhes da playlist com as músicas em ordem (RF19).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var playlist = await _playlistRepository.BuscarDoUsuario(id, IdUsuarioLogado);
        return playlist is null
            ? Erro(404, NaoEncontrada)
            : Ok(PlaylistDetalheDTO.Converter(playlist));
    }

    /// <summary>Cria uma playlist (RF17 / RN15).</summary>
    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] PlaylistDTO dto)
    {
        var idUsuario = IdUsuarioLogado;
        var nome = dto.Nome.Trim();

        if (await _playlistRepository.NomeEmUso(idUsuario, nome))
            return Erro(409, NomeDuplicado);

        var playlist = new Playlist
        {
            IdUsuario = idUsuario,
            Nome = nome,
            Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim()
        };

        try
        {
            await _playlistRepository.Cadastrar(playlist);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, NomeDuplicado);
        }

        var criada = await _playlistRepository.BuscarDoUsuario(playlist.IdPlaylist, idUsuario);
        return CreatedAtAction(nameof(BuscarPorId), new { id = playlist.IdPlaylist }, PlaylistDetalheDTO.Converter(criada!));
    }

    /// <summary>Edita nome e descrição da playlist (RF20 / RN15).</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] PlaylistDTO dto)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _playlistRepository.PertenceAoUsuario(id, idUsuario)) return Erro(404, NaoEncontrada);

        var nome = dto.Nome.Trim();
        if (await _playlistRepository.NomeEmUso(idUsuario, nome, id))
            return Erro(409, NomeDuplicado);

        var descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim();

        try
        {
            await _playlistRepository.Atualizar(id, nome, descricao);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, NomeDuplicado);
        }

        return NoContent();
    }

    /// <summary>Exclui a playlist. As músicas continuam no catálogo (RF21 / RN18).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (!await _playlistRepository.PertenceAoUsuario(id, IdUsuarioLogado)) return Erro(404, NaoEncontrada);

        await _playlistRepository.Excluir(id);
        return NoContent();
    }

    /// <summary>Adiciona uma música ao final da playlist (RF22 / RN16). Retorna a playlist atualizada.</summary>
    [HttpPost("{id:int}/musicas")]
    public async Task<IActionResult> AdicionarMusica(int id, [FromBody] AdicionarMusicaDTO dto)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _playlistRepository.PertenceAoUsuario(id, idUsuario)) return Erro(404, NaoEncontrada);
        if (!await _musicaRepository.Existe(dto.IdMusica)) return Erro(404, "Música não encontrada.");
        if (await _playlistRepository.ContemMusica(id, dto.IdMusica))
            return Erro(409, "Essa música já está na playlist.");

        try
        {
            await _playlistRepository.AdicionarMusica(id, dto.IdMusica);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, "A playlist foi alterada ao mesmo tempo por outra ação. Tente novamente.");
        }

        return await PlaylistAtualizada(id, idUsuario);
    }

    /// <summary>Remove uma música da playlist (RF23). Retorna a playlist atualizada.</summary>
    [HttpDelete("{id:int}/musicas/{idMusica:int}")]
    public async Task<IActionResult> RemoverMusica(int id, int idMusica)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _playlistRepository.PertenceAoUsuario(id, idUsuario)) return Erro(404, NaoEncontrada);

        if (!await _playlistRepository.RemoverMusica(id, idMusica))
            return Erro(404, "Essa música não está na playlist.");

        return await PlaylistAtualizada(id, idUsuario);
    }

    /// <summary>
    /// Reordena as músicas da playlist (RF24). Envie todos os IdMusica da playlist na nova ordem.
    /// Exemplo: { "ids": [5, 2, 9] }. Retorna a playlist atualizada.
    /// </summary>
    [HttpPut("{id:int}/musicas/ordem")]
    public async Task<IActionResult> Reordenar(int id, [FromBody] OrdemDTO dto)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _playlistRepository.PertenceAoUsuario(id, idUsuario)) return Erro(404, NaoEncontrada);

        if (!await _playlistRepository.Reordenar(id, dto.Ids))
            return Erro(400, "Envie todas as músicas da playlist, sem repetir, na nova ordem.");

        return await PlaylistAtualizada(id, idUsuario);
    }

    private async Task<IActionResult> PlaylistAtualizada(int id, int idUsuario)
    {
        var playlist = await _playlistRepository.BuscarDoUsuario(id, idUsuario);
        return playlist is null
            ? Erro(404, NaoEncontrada)
            : Ok(PlaylistDetalheDTO.Converter(playlist));
    }
}
