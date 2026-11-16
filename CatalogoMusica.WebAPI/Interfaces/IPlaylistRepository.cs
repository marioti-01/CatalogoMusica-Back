using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.Interfaces;

/// <summary>
/// Todas as consultas recebem o IdUsuario dono da playlist (RN14):
/// playlist de outro usuário se comporta como inexistente.
/// </summary>
public interface IPlaylistRepository
{
    Task<List<Playlist>> ListarDoUsuario(int idUsuario);
    Task<Playlist?> BuscarDoUsuario(int idPlaylist, int idUsuario);
    Task<bool> PertenceAoUsuario(int idPlaylist, int idUsuario);
    Task<bool> NomeEmUso(int idUsuario, string nome, int? ignorarId = null);
    Task Cadastrar(Playlist playlist);
    Task Atualizar(int idPlaylist, string nome, string? descricao);
    Task Excluir(int idPlaylist);

    Task<bool> ContemMusica(int idPlaylist, int idMusica);
    Task AdicionarMusica(int idPlaylist, int idMusica);
    Task<bool> RemoverMusica(int idPlaylist, int idMusica);

    /// <summary>Retorna false se a lista não tiver exatamente as músicas atuais da playlist.</summary>
    Task<bool> Reordenar(int idPlaylist, IReadOnlyList<int> idsMusicas);
}
