using CatalogoMusica.WebAPI.DTO;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Controllers;

/// <summary>
/// Fila de reprodução do usuário logado (RN19). A música na posição 1 é a que está tocando.
/// As ações que alteram a fila retornam a fila atualizada.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class FilaController : ApiControllerBase
{
    private const string FilaAlteradaAoMesmoTempo = "A fila foi alterada ao mesmo tempo por outra ação. Tente novamente.";

    private readonly IFilaRepository _filaRepository;
    private readonly IMusicaRepository _musicaRepository;
    private readonly IPlaylistRepository _playlistRepository;

    public FilaController(IFilaRepository filaRepository, IMusicaRepository musicaRepository, IPlaylistRepository playlistRepository)
    {
        _filaRepository = filaRepository;
        _musicaRepository = musicaRepository;
        _playlistRepository = playlistRepository;
    }

    /// <summary>Mostra a fila em ordem (RF27).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return await FilaAtual(IdUsuarioLogado);
    }

    /// <summary>Adiciona uma música ao final da fila (RF25). A mesma música pode entrar mais de uma vez (RN20).</summary>
    [HttpPost]
    public async Task<IActionResult> AdicionarMusica([FromBody] AdicionarMusicaDTO dto)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _musicaRepository.Existe(dto.IdMusica)) return Erro(404, "Música não encontrada.");

        try
        {
            await _filaRepository.Adicionar(idUsuario, new[] { dto.IdMusica });
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, FilaAlteradaAoMesmoTempo);
        }

        return await FilaAtual(idUsuario);
    }

    /// <summary>Adiciona todas as músicas de uma playlist ao final da fila, na ordem da playlist (RF26).</summary>
    [HttpPost("playlist/{idPlaylist:int}")]
    public async Task<IActionResult> AdicionarPlaylist(int idPlaylist)
    {
        var idUsuario = IdUsuarioLogado;
        var playlist = await _playlistRepository.BuscarDoUsuario(idPlaylist, idUsuario);
        if (playlist is null) return Erro(404, "Playlist não encontrada.");

        var idsMusicas = playlist.Musicas.OrderBy(pm => pm.Posicao).Select(pm => pm.IdMusica).ToList();
        if (idsMusicas.Count == 0) return Erro(400, "A playlist não tem músicas.");

        try
        {
            await _filaRepository.Adicionar(idUsuario, idsMusicas);
        }
        catch (DbUpdateException erro) when (SqlErros.ViolouUnicidade(erro))
        {
            return Erro(409, FilaAlteradaAoMesmoTempo);
        }

        return await FilaAtual(idUsuario);
    }

    /// <summary>
    /// Reordena a fila (RF28). Envie todos os IdFila na nova ordem.
    /// Exemplo: { "ids": [12, 10, 11] }.
    /// </summary>
    [HttpPut("ordem")]
    public async Task<IActionResult> Reordenar([FromBody] OrdemDTO dto)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _filaRepository.Reordenar(idUsuario, dto.Ids))
            return Erro(400, "Envie todos os itens da fila (IdFila), sem repetir, na nova ordem.");

        return await FilaAtual(idUsuario);
    }

    /// <summary>Remove um item da fila pelo IdFila (RF29).</summary>
    [HttpDelete("{idFila:int}")]
    public async Task<IActionResult> Remover(int idFila)
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _filaRepository.Remover(idUsuario, idFila))
            return Erro(404, "Item não encontrado na sua fila.");

        return await FilaAtual(idUsuario);
    }

    /// <summary>Limpa a fila inteira (RF30).</summary>
    [HttpDelete]
    public async Task<IActionResult> Limpar()
    {
        await _filaRepository.Limpar(IdUsuarioLogado);
        return NoContent();
    }

    /// <summary>
    /// Avança para a próxima música (RF31 / RN21): a música atual sai da fila
    /// e a seguinte passa a ser a primeira.
    /// </summary>
    [HttpPost("avancar")]
    public async Task<IActionResult> Avancar()
    {
        var idUsuario = IdUsuarioLogado;
        if (!await _filaRepository.Avancar(idUsuario))
            return Erro(404, "A fila está vazia.");

        return await FilaAtual(idUsuario);
    }

    private async Task<IActionResult> FilaAtual(int idUsuario)
    {
        var fila = await _filaRepository.Listar(idUsuario);
        return Ok(fila.Select(FilaItemDTO.Converter));
    }
}
