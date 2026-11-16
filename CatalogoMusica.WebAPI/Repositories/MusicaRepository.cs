using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Repositories;

public class MusicaRepository : IMusicaRepository
{
    // Depois que o banco remove a música em cascata das playlists e filas, sobram "buracos"
    // nas posições (ex.: 1, 2, 4). Estes comandos renumeram para 1, 2, 3... (RN17).
    // Cada UPDATE é um único comando, então as constraints UNIQUE de posição não são violadas.
    private const string SqlRenumerarPlaylists = """
        WITH Ordenada AS (
            SELECT Posicao, ROW_NUMBER() OVER (PARTITION BY IdPlaylist ORDER BY Posicao) AS NovaPosicao
            FROM PlaylistMusica
        )
        UPDATE Ordenada SET Posicao = NovaPosicao WHERE Posicao <> NovaPosicao;
        """;

    private const string SqlRenumerarFilas = """
        WITH Ordenada AS (
            SELECT Posicao, ROW_NUMBER() OVER (PARTITION BY IdUsuario ORDER BY Posicao) AS NovaPosicao
            FROM FilaReproducao
        )
        UPDATE Ordenada SET Posicao = NovaPosicao WHERE Posicao <> NovaPosicao;
        """;

    private readonly CatalogoMusicaContext _context;

    public MusicaRepository(CatalogoMusicaContext context)
    {
        _context = context;
    }

    public async Task<(List<Musica> musicas, int total)> ListarPaginado(int pagina = 1, int tamanhoPagina = 10, string? busca = null, int? idGenero = null)
    {
        var query = _context.Musica.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(m => m.Titulo.Contains(termo) || m.Artista.Nome.Contains(termo));
        }

        if (idGenero is not null)
            query = query.Where(m => m.Generos.Any(g => g.IdGenero == idGenero));

        var total = await query.CountAsync();

        var musicas = await query
            .Include(m => m.Artista)
            .Include(m => m.Generos)
            .OrderBy(m => m.Titulo).ThenBy(m => m.IdMusica)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .AsSplitQuery()
            .ToListAsync();

        return (musicas, total);
    }

    public async Task<Musica?> BuscarPorId(int id)
    {
        return await _context.Musica.AsNoTracking()
            .Include(m => m.Artista)
            .Include(m => m.Generos)
            .FirstOrDefaultAsync(m => m.IdMusica == id);
    }

    public async Task<bool> Existe(int id)
    {
        return await _context.Musica.AnyAsync(m => m.IdMusica == id);
    }

    public async Task Cadastrar(Musica musica)
    {
        await _context.Musica.AddAsync(musica);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int id, Musica musica, List<Genero> generos)
    {
        var musicaExistente = await _context.Musica
            .Include(m => m.Generos)
            .FirstOrDefaultAsync(m => m.IdMusica == id);
        if (musicaExistente is null) return;

        musicaExistente.Titulo = musica.Titulo;
        musicaExistente.IdArtista = musica.IdArtista;
        musicaExistente.Album = musica.Album;
        musicaExistente.DataLancamento = musica.DataLancamento;
        musicaExistente.Duracao = musica.Duracao;
        musicaExistente.ImagemCapa = musica.ImagemCapa;

        // Atualiza a tabela MusicaGenero: remove os que saíram e adiciona os novos.
        var idsNovos = generos.Select(g => g.IdGenero).ToHashSet();
        foreach (var genero in musicaExistente.Generos.Where(g => !idsNovos.Contains(g.IdGenero)).ToList())
            musicaExistente.Generos.Remove(genero);

        var idsAtuais = musicaExistente.Generos.Select(g => g.IdGenero).ToHashSet();
        foreach (var genero in generos.Where(g => !idsAtuais.Contains(g.IdGenero)))
            musicaExistente.Generos.Add(genero);

        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();

        // O banco remove em cascata: MusicaGenero, PlaylistMusica e FilaReproducao (RN22).
        var removidas = await _context.Musica.Where(m => m.IdMusica == id).ExecuteDeleteAsync();

        if (removidas > 0)
        {
            await _context.Database.ExecuteSqlRawAsync(SqlRenumerarPlaylists);
            await _context.Database.ExecuteSqlRawAsync(SqlRenumerarFilas);
        }

        await transacao.CommitAsync();
    }
}
