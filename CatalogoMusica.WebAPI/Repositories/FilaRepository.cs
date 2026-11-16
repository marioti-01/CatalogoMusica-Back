using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoMusica.WebAPI.Repositories;

public class FilaRepository : IFilaRepository
{
    // Valor somado às posições durante a reordenação, para que nenhuma posição
    // temporária colida com a constraint UNIQUE (IdUsuario, Posicao).
    private const int DeslocamentoTemporario = 1_000_000;

    private readonly CatalogoMusicaContext _context;

    public FilaRepository(CatalogoMusicaContext context)
    {
        _context = context;
    }

    public async Task<List<FilaReproducao>> Listar(int idUsuario)
    {
        return await _context.FilaReproducao.AsNoTracking()
            .Where(f => f.IdUsuario == idUsuario)
            .Include(f => f.Musica).ThenInclude(m => m.Artista)
            .Include(f => f.Musica).ThenInclude(m => m.Generos)
            .OrderBy(f => f.Posicao)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task Adicionar(int idUsuario, IReadOnlyList<int> idsMusicas)
    {
        var ultimaPosicao = await _context.FilaReproducao
            .Where(f => f.IdUsuario == idUsuario)
            .MaxAsync(f => (int?)f.Posicao) ?? 0;

        for (var i = 0; i < idsMusicas.Count; i++)
        {
            await _context.FilaReproducao.AddAsync(new FilaReproducao
            {
                IdUsuario = idUsuario,
                IdMusica = idsMusicas[i],
                Posicao = ultimaPosicao + i + 1
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> Remover(int idUsuario, int idFila)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();

        var item = await _context.FilaReproducao.AsNoTracking()
            .FirstOrDefaultAsync(f => f.IdFila == idFila && f.IdUsuario == idUsuario);
        if (item is null) return false;

        await _context.FilaReproducao.Where(f => f.IdFila == idFila).ExecuteDeleteAsync();

        await _context.FilaReproducao
            .Where(f => f.IdUsuario == idUsuario && f.Posicao > item.Posicao)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Posicao, f => f.Posicao - 1));

        await transacao.CommitAsync();
        return true;
    }

    public async Task Limpar(int idUsuario)
    {
        await _context.FilaReproducao.Where(f => f.IdUsuario == idUsuario).ExecuteDeleteAsync();
    }

    public async Task<bool> Reordenar(int idUsuario, IReadOnlyList<int> idsFila)
    {
        var atuais = await _context.FilaReproducao
            .Where(f => f.IdUsuario == idUsuario)
            .Select(f => f.IdFila)
            .ToListAsync();

        var novos = idsFila.ToHashSet();
        if (novos.Count != idsFila.Count || novos.Count != atuais.Count || !novos.SetEquals(atuais))
            return false;

        await using var transacao = await _context.Database.BeginTransactionAsync();

        await _context.FilaReproducao
            .Where(f => f.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Posicao, f => f.Posicao + DeslocamentoTemporario));

        for (var i = 0; i < idsFila.Count; i++)
        {
            var idFila = idsFila[i];
            var posicao = i + 1;

            await _context.FilaReproducao
                .Where(f => f.IdFila == idFila)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.Posicao, posicao));
        }

        await transacao.CommitAsync();
        return true;
    }

    public async Task<bool> Avancar(int idUsuario)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();

        var removidas = await _context.FilaReproducao
            .Where(f => f.IdUsuario == idUsuario && f.Posicao == 1)
            .ExecuteDeleteAsync();
        if (removidas == 0) return false;

        await _context.FilaReproducao
            .Where(f => f.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Posicao, f => f.Posicao - 1));

        await transacao.CommitAsync();
        return true;
    }
}
