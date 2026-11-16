using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Repositories;

public class PlaylistRepository : IPlaylistRepository
{
    // Valor somado às posições durante a reordenação, para que nenhuma posição
    // temporária colida com a constraint UNIQUE (IdPlaylist, Posicao).
    private const int DeslocamentoTemporario = 1_000_000;

    private readonly CatalogoMusicaContext _context;

    public PlaylistRepository(CatalogoMusicaContext context)
    {
        _context = context;
    }

    public async Task<List<Playlist>> ListarDoUsuario(int idUsuario)
    {
        return await _context.Playlist.AsNoTracking()
            .Where(p => p.IdUsuario == idUsuario)
            .Include(p => p.Musicas).ThenInclude(pm => pm.Musica)
            .OrderBy(p => p.Nome)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<Playlist?> BuscarDoUsuario(int idPlaylist, int idUsuario)
    {
        return await _context.Playlist.AsNoTracking()
            .Include(p => p.Musicas).ThenInclude(pm => pm.Musica).ThenInclude(m => m.Artista)
            .Include(p => p.Musicas).ThenInclude(pm => pm.Musica).ThenInclude(m => m.Generos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.IdPlaylist == idPlaylist && p.IdUsuario == idUsuario);
    }

    public async Task<bool> PertenceAoUsuario(int idPlaylist, int idUsuario)
    {
        return await _context.Playlist.AnyAsync(p => p.IdPlaylist == idPlaylist && p.IdUsuario == idUsuario);
    }

    public async Task<bool> NomeEmUso(int idUsuario, string nome, int? ignorarId = null)
    {
        return await _context.Playlist.AnyAsync(p =>
            p.IdUsuario == idUsuario
            && p.Nome == nome
            && (ignorarId == null || p.IdPlaylist != ignorarId));
    }

    public async Task Cadastrar(Playlist playlist)
    {
        await _context.Playlist.AddAsync(playlist);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int idPlaylist, string nome, string? descricao)
    {
        await _context.Playlist
            .Where(p => p.IdPlaylist == idPlaylist)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Nome, nome)
                .SetProperty(p => p.Descricao, descricao));
    }

    public async Task Excluir(int idPlaylist)
    {
        // O banco remove em cascata os vínculos em PlaylistMusica; as músicas continuam no catálogo (RN18).
        await _context.Playlist.Where(p => p.IdPlaylist == idPlaylist).ExecuteDeleteAsync();
    }

    public async Task<bool> ContemMusica(int idPlaylist, int idMusica)
    {
        return await _context.PlaylistMusica.AnyAsync(pm => pm.IdPlaylist == idPlaylist && pm.IdMusica == idMusica);
    }

    public async Task AdicionarMusica(int idPlaylist, int idMusica)
    {
        var ultimaPosicao = await _context.PlaylistMusica
            .Where(pm => pm.IdPlaylist == idPlaylist)
            .MaxAsync(pm => (int?)pm.Posicao) ?? 0;

        await _context.PlaylistMusica.AddAsync(new PlaylistMusica
        {
            IdPlaylist = idPlaylist,
            IdMusica = idMusica,
            Posicao = ultimaPosicao + 1
        });
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoverMusica(int idPlaylist, int idMusica)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();

        var item = await _context.PlaylistMusica.AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.IdPlaylist == idPlaylist && pm.IdMusica == idMusica);
        if (item is null) return false;

        await _context.PlaylistMusica
            .Where(pm => pm.IdPlaylist == idPlaylist && pm.IdMusica == idMusica)
            .ExecuteDeleteAsync();

        // Mantém as posições sequenciais a partir de 1 (RN17).
        await _context.PlaylistMusica
            .Where(pm => pm.IdPlaylist == idPlaylist && pm.Posicao > item.Posicao)
            .ExecuteUpdateAsync(s => s.SetProperty(pm => pm.Posicao, pm => pm.Posicao - 1));

        await transacao.CommitAsync();
        return true;
    }

    public async Task<bool> Reordenar(int idPlaylist, IReadOnlyList<int> idsMusicas)
    {
        var atuais = await _context.PlaylistMusica
            .Where(pm => pm.IdPlaylist == idPlaylist)
            .Select(pm => pm.IdMusica)
            .ToListAsync();

        var novos = idsMusicas.ToHashSet();
        if (novos.Count != idsMusicas.Count || novos.Count != atuais.Count || !novos.SetEquals(atuais))
            return false;

        await using var transacao = await _context.Database.BeginTransactionAsync();

        await _context.PlaylistMusica
            .Where(pm => pm.IdPlaylist == idPlaylist)
            .ExecuteUpdateAsync(s => s.SetProperty(pm => pm.Posicao, pm => pm.Posicao + DeslocamentoTemporario));

        for (var i = 0; i < idsMusicas.Count; i++)
        {
            var idMusica = idsMusicas[i];
            var posicao = i + 1;

            await _context.PlaylistMusica
                .Where(pm => pm.IdPlaylist == idPlaylist && pm.IdMusica == idMusica)
                .ExecuteUpdateAsync(s => s.SetProperty(pm => pm.Posicao, posicao));
        }

        await transacao.CommitAsync();
        return true;
    }
}
