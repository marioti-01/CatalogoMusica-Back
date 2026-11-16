using CatalogoMusica.WebAPI.Models;

namespace CatalogoMusica.WebAPI.Interfaces;

public interface IMusicaRepository
{
    /// <summary>Lista paginada. "busca" procura no título ou no nome do artista (RF05); "idGenero" filtra (RF04).</summary>
    Task<(List<Musica> musicas, int total)> ListarPaginado(int pagina = 1, int tamanhoPagina = 10, string? busca = null, int? idGenero = null);
    Task<Musica?> BuscarPorId(int id);
    Task<bool> Existe(int id);
    Task Cadastrar(Musica musica);
    Task Atualizar(int id, Musica musica, List<Genero> generos);

    /// <summary>Exclui a música e reorganiza as posições das playlists e filas afetadas (RN22 / RN17).</summary>
    Task Excluir(int id);
}
